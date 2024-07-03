using Amicitia.IO;
using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AshDumpLib.HedgehogEngine.BINA;

public class BINAWriter : BinaryObjectWriter
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

    Dictionary<long, long> stringTableOffsets = new();
    List<char> stringTable = new();
    Dictionary<string, long> offsets = new();

    public BINAWriter(string filePath, Endianness endianness, Encoding encoding) : base(filePath, endianness, encoding)
    {
    }

    public BINAWriter(string filePath, FileStreamingMode fileStreamingMode, Endianness endianness, Encoding encoding, int bufferSize = 1048576) : base(filePath, fileStreamingMode, endianness, encoding, bufferSize)
    {
    }

    public BINAWriter(Stream stream, StreamOwnership streamOwnership, Endianness endianness, Encoding encoding = null, string fileName = null, int blockSize = 1048576) : base(stream, streamOwnership, endianness, encoding, fileName, blockSize)
    {
    }

    public void WriteNulls(int amount)
    {
        for(int i = 0; i < amount; i++)
        {
            WriteChar('\0');
        }
    }

    public void WriteChar(char value)
    {
        Write(value);
        this.Skip(-1);
    }

    public void WriteHeader()
    {
        //Writes BINA signature
        WriteString(StringBinaryFormat.FixedLength, BINASignature, 4);

        //Writes BINA version
        WriteString(StringBinaryFormat.FixedLength, BINAVersion, 3);

        //Writes endianess
        if(Endianness == Endianness.Little)
        {
            Write<byte>(76);
        }
        else if(Endianness == Endianness.Big)
        {
            Write<byte>(66);
        }
        else
        {
            //Errors out if endianess is broken
            throw new Exception("Unknown endianess!");
        }

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

    public void WriteSignature(string signature)
    {
        //Writes BINA file's unique signature
        WriteString(StringBinaryFormat.FixedLength, signature, 4);
    }

    public void WriteStringTableEntry(string entry)
    {
        //Adds offset to the OffsetTable
        offsets.Add(entry, Position - 64);

        //Adds offset to the StringTableOffset for later correction
        stringTableOffsets.Add(Position, stringTable.Count);

        //Writes the temporary offset in the StringTable
        Write<long>(stringTable.Count);
        foreach (var i in entry.ToCharArray())
        {
            stringTable.Add(i);
        }
        stringTable.Add('\0');
    }

    public void FixPadding(int padding)
    {
        int amount = 0;
        while ((Position + amount) % padding != 0)
            amount++;
        WriteNulls(amount);
    }

    public void FinishWrite()
    {
        //We save the current position as StringTableOffset here so we can quickly jump back and also write it in the BINA header
        int stringTableOffset = (int)Position;

        //Here we finally fix all the StringTableOffsets
        foreach (var i in stringTableOffsets)
        {
            Seek(i.Key, SeekOrigin.Begin);
            Write(i.Value + stringTableOffset - 64);
        }
        
        //Now we write the StringTableOffset in the BINA header
        Seek(stringTableOffsetPos, SeekOrigin.Begin);
        Write(stringTableOffset - 64);

        //We finally write the StringTableEntries
        Seek(stringTableOffset, SeekOrigin.Begin);
        foreach(var i in stringTable)
        {
            WriteChar(i);
        }

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
        foreach(var i in offsets)
        {
            int x = ((int)i.Value - (int)lastOffsetPos) >> 2;
            if(x <= 63)
            {
                Write((byte)((byte)64 | x));
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

    public void AddOffset(string id)
    {
        offsets.Add(id, Position - 64);
        this.Skip(8);
    }

    public void SetOffset(string id)
    {
        long offset = Position - 64;
        Jump(offsets[id], SeekOrigin.Begin);
        Write(offset);
        Jump(offset, SeekOrigin.Begin);
    }

    public void Jump(long offset, SeekOrigin origin)
    {
        Seek(offset + 64, origin);
    }
}
