using Amicitia.IO;
using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace AshDumpLib.HedgehogEngine.BINA;

public class BINAWriter : ExtendedBinaryWriter
{
    public const string BINASignature = "BINA";
    public const string BINAVersion = "210";
    public const string BlockSignature = "DATA";

    public int RelativeDataOffset = 24;

    long fileSizePos = 8;
    long blockSizePos = 20;
    long stringTableOffsetPos = 24;
    long stringTableSizePos = 28;
    long offsetTableSizePos = 32;

    Dictionary<string, List<object>> arrays = new();

    public BINAWriter(string filePath, Endianness endianness, Encoding encoding) : base(filePath, endianness, encoding)
    {
        GenericOffset = 64;
    }

    public BINAWriter(string filePath, FileStreamingMode fileStreamingMode, Endianness endianness, Encoding encoding, int bufferSize = 1048576) : base(filePath, fileStreamingMode, endianness, encoding, bufferSize)
    {
        GenericOffset = 64;
    }

    public BINAWriter(Stream stream, StreamOwnership streamOwnership, Endianness endianness, Encoding encoding = null, string fileName = null, int blockSize = 1048576) : base(stream, streamOwnership, endianness, encoding, fileName, blockSize)
    {
        GenericOffset = 64;
    }

    public override void SetOffset(string id)
    {
        long offset = Position;
        Seek(Offsets[id] + GenericOffset, SeekOrigin.Begin);
        Write(offset - GenericOffset);
        OffsetValues[id] = offset;
        Seek(offset, SeekOrigin.Begin);
    }

    public override void WriteHeader()
    {
        //Writes BINA signature
        WriteString(StringBinaryFormat.FixedLength, BINASignature, 4);

        //Writes BINA version
        WriteString(StringBinaryFormat.FixedLength, BINAVersion, 3);

        //Writes endianess
        if(Endianness == Endianness.Little)
            Write<byte>(76);
        else if(Endianness == Endianness.Big)
            Write<byte>(66);
        else
            //Errors out if endianess is broken
            throw new Exception("Unknown endianess!");

        //Writes a 0 and saves fileSizePos for later
        fileSizePos = Position;
        WriteNulls(4);

        //Writes data block count
        Write(1);

        //Writes data block signature
        WriteString(StringBinaryFormat.FixedLength, BlockSignature, 4);

        //Writes a 0 and saves blockSizePos for later
        blockSizePos = Position;
        WriteNulls(4);

        //Writes a 0 and saves stringTableOffsetPos for later
        stringTableOffsetPos = Position;
        WriteNulls(4);

        //Writes a 0 and saves stringTableSizePos for later
        stringTableSizePos = Position;
        WriteNulls(4);

        //Writes a 0 and saves offsetTableSizePos for later
        offsetTableSizePos = Position;
        WriteNulls(4);

        //Writes and skips the value of RelativeDataOffset, default being 24
        Write(RelativeDataOffset);
        WriteNulls(RelativeDataOffset);
    }

    public override void FinishWrite()
    {
        foreach(var x in arrays)
        {
            SetOffset(x.Key);
            foreach(var i in x.Value)
            {
                if (i.GetType().IsGenericType)
                {
                    MethodInfo method = typeof(BinaryValueWriter).GetMethod(nameof(Write));
                    MethodInfo genericMethod = method.MakeGenericMethod(i.GetType().GetGenericArguments()[0]);
                    genericMethod.Invoke(this, new object[] { i });
                }
                else if (i.GetType() == typeof(string))
                {
                    this.Align(8);
                    WriteStringTableEntry((string)i);
                }
                else if (i.GetType() == typeof(Int16))
                    Write((Int16)i);
                else if (i.GetType() == typeof(Vector2))
                    Write((Vector2)i);
                else
                    ((IBINASerializable)i).Write(this);
            }
        }

        foreach (var x in arrays)
        {
            foreach (var i in x.Value)
            {
                if (i is IBINASerializable)
                {
                    ((IBINASerializable)i).FinishWrite(this);
                }  
            }
        }

        this.Align(4);
        //We save the current position as StringTableOffset here so we can quickly jump back and also write it in the BINA header
        int stringTableOffset = (int)Position;

        //Here we finally fix all the StringTableOffsets
        foreach (var i in StringTableOffsets)
        {
            Seek(i.Key, SeekOrigin.Begin);
            Write(i.Value + stringTableOffset - GenericOffset);
        }
        
        //Now we write the StringTableOffset in the BINA header
        Seek(stringTableOffsetPos, SeekOrigin.Begin);
        Write(stringTableOffset - GenericOffset);

        //We finally write the StringTableEntries
        Seek(stringTableOffset, SeekOrigin.Begin);
        foreach(var i in StringTable)
            WriteChar(i);

        FixPadding(4);

        //We calculate and save the StringTableSize
        int stringTableSize = (int)Position - stringTableOffset;

        //We save the current position
        long prePosOffsetTable = Position;

        //Then go to the position of the StringTableSize, write the size, and then go back
        Seek(stringTableSizePos, SeekOrigin.Begin);
        Write(stringTableSize);
        Seek(prePosOffsetTable, SeekOrigin.Begin);

        //Now we write all the offsets
        //Here we save the last offset position, because the BINA offsets are based on the spaces between offsets
        long lastOffsetPos = 0;

        //We loop through them, convert them into binary, and write them
        foreach (var i in Offsets)
        {
            int difference = (int)(i.Value - lastOffsetPos) >> 2;
            if (difference <= 0x3F)
            {
                int x = difference & 0x3F;
                Write((byte)((byte)64 | x));
            }
            else if (difference <= 0x3FFF)
            {
                int x = difference & 0x3FFF;
                Write((byte)((byte)128 | (x >> 8)));
                Write((byte)(x & 0xFF));
            }
            else if (difference <= 0x3FFFFFFF)
            {
                int x = difference & 0x3FFFFFFF;
                Write((byte)((byte)192 | (x >> 24)));
                Write((byte)((x >> 16) & 0xFF));
                Write((byte)((x >> 8) & 0xFF));
                Write((byte)(x & 0xFF));
            }
            lastOffsetPos = i.Value;
        }

        FixPadding(4);

        //We save the Offset Table Size and File Size because we're at the end of the file
        int offsetTableSize = (int)(Position - prePosOffsetTable);
        int fileSize = (int)Position;

        //Now we go to the respective position and write the final values
        Seek(fileSizePos, SeekOrigin.Begin);
        Write(fileSize);
        Seek(blockSizePos, SeekOrigin.Begin);
        Write(fileSize - 16);
        Seek(offsetTableSizePos, SeekOrigin.Begin);
        Write(offsetTableSize);
    }

    public override void AddOffset(string id, bool write = true)
    {
        Offsets.Add(id, Position - GenericOffset);
        OffsetValues.Add(id, 0);
        OffsetsWrite.Add(id, write);
        if(write)
            this.Skip(8);
    }

    public void WriteBINAArray<T>(List<T> list, string id, bool write = true)
    {
        if (write)
        {
            if (list.Count > 0)
            {
                Write(list.Count);
                this.Align(8);
            }
            else
            {
                WriteNulls(4);
                this.Align(8);
                WriteNulls(8);
            }
        }

        if(list.Count > 0)
        {
            List<object> li = new();
            foreach (var i in list)
                li.Add(i);
            if (arrays.ContainsKey(id))
                arrays[id].AddRange(li);
            else
            {
                AddOffset(id, write);
                arrays.Add(id, li);
            }
        }
    }

    public void WriteBINAArray64<T>(List<T> list, string id)
    {
        if (list.Count > 0)
        {
            AddOffset(id);
            this.Align(8);
            Write((long)list.Count);
            List<object> li = new();
            foreach (var i in list)
                li.Add(i);
            if (arrays.ContainsKey(id))
                arrays[id].AddRange(li);
            else
                arrays.Add(id, li);
        }
        else
            WriteNulls(16);
    }
}
