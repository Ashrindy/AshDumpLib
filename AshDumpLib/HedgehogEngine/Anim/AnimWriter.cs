using Amicitia.IO;
using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AshDumpLib.HedgehogEngine.Anim;

public class AnimWriter : ExtendedBinaryWriter
{
    public enum AnimType : int
    {
        CameraAnimation = 0,
        MaterialAnimation,
        UVAnimation,
        VisibilityAnimation
    }

    public int Version = 2;
    public AnimType AnimationType = AnimType.CameraAnimation;

    public AnimWriter(string filePath, Endianness endianness, Encoding encoding) : base(filePath, endianness, encoding)
    {
        GenericOffset = 24;
    }

    public AnimWriter(string filePath, FileStreamingMode fileStreamingMode, Endianness endianness, Encoding encoding, int bufferSize = 1048576) : base(filePath, fileStreamingMode, endianness, encoding, bufferSize)
    {
        GenericOffset = 24;
    }

    public AnimWriter(Stream stream, StreamOwnership streamOwnership, Endianness endianness, Encoding encoding = null, string fileName = null, int blockSize = 1048576) : base(stream, streamOwnership, endianness, encoding, fileName, blockSize)
    {
        GenericOffset = 24;
    }

    public override void WriteStringTableEntry(string entry)
    {
        //Adds offset to the OffsetTable
        Offsets.Add(entry, Position);

        //Adds offset to the OffsetsWrite dictionary
        OffsetsWrite.Add(entry, false);

        //Writes the offset in the StringTable
        Write(StringTable.Count);
        foreach (var i in entry.ToCharArray())
        {
            StringTable.Add(i);
        }
        StringTable.Add('\0');
    }

    public override void SetOffset(string id)
    {
        long offset = Position;
        Seek(Offsets[id], SeekOrigin.Begin);
        Write((int)offset - GenericOffset);
        OffsetValues[id] = offset - GenericOffset;
        Seek(offset, SeekOrigin.Begin);
    }

    public override void WriteHeader()
    {
        AddOffset("fileSize", false);

        Write(Version);

        AddOffset("dataSize", false);

        Write(GenericOffset);

        AddOffset("offsetsPointer", false);

        WriteNulls(4);
    }

    public override void FinishWrite()
    {
        SetOffset("strings");

        foreach (var i in StringTable)
            WriteChar(i);

        FixPadding(4);

        int dataSize = (int)Position - 24;
        int stringSize = dataSize - (int)GetOffsetValue("strings");

        WriteAt(dataSize, GetOffset("dataSize"));
        WriteAt(stringSize, GetOffset("strings") + 4);

        int offsetPointer = (int)Position;
        WriteAt(offsetPointer, GetOffset("offsetsPointer"));

        AddOffset("offsetsCount", false);

        for (int x = 0; x < 24; x += 4)
            Write(x);

        foreach(var i in Offsets)
            if (OffsetsWrite[i.Key])
                if(AnimationType == AnimType.UVAnimation || AnimationType == AnimType.VisibilityAnimation)
                    Write((int)i.Value - GenericOffset);
                else
                    Write((int)i.Value - GenericOffset - 4);

        int offsetCount = ((int)Position - ((int)GetOffset("offsetsCount") + 4)) / 4;
        WriteAt(offsetCount, GetOffset("offsetsCount"));

        int fileSize = (int)Position;

        WriteAt(fileSize, GetOffset("fileSize"));
    }
}
