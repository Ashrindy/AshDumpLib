using Amicitia.IO;
using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using System.Text;

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
        Write(StringTable.Length);
        foreach (var i in entry.ToCharArray())
            StringTable += i;
        StringTable += '\0';
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
        //We save the file size position with the offset system that the ExtendedBinaryWriter has
        AddOffset("fileSize", false);

        //Writes the version which is different across the .*-anim files
        Write(Version);

        //We save the data size position with the offset system that the ExtendedBinaryWriter has
        AddOffset("dataSize", false);

        //We write the offset to the data block
        Write(GenericOffset);

        //We save the offsets pointer position with the offset system that the ExtendedBinaryWriter has
        AddOffset("offsetsPointer", false);

        WriteNulls(4);
    }

    public override void FinishWrite()
    {
        //Sets the string table offset
        SetOffset("strings");

        //Writes the string table
        foreach (var i in StringTable)
            WriteChar(i);

        FixPadding(4);

        //Gets string size by subtracting the current position from the string table pointer
        int stringSize = ((int)Position - GenericOffset) - (int)GetOffsetValue("strings");

        //Writes string size
        WriteAt(stringSize, GetOffset("strings") + 4);

        //Gets data size by getting the position with the generic offset subtracted
        int dataSize = (int)Position - GenericOffset;

        //Writes data size
        WriteAt(dataSize, GetOffset("dataSize"));

        //Gets the offset pointer by getting the current position
        int offsetPointer = (int)Position;

        //Writes offset pointer
        WriteAt(offsetPointer, GetOffset("offsetsPointer"));

        //Saves the offset amount position
        AddOffset("offsetsCount", false);

        //Writes some offsets that I have no idea where they come from
        for (int x = 0; x < 24; x += 4)
            Write(x);

        //Writes offsets
        foreach(var i in Offsets)
            if (OffsetsWrite[i.Key])
                //Sometimes the .*-anim files seem to address the issue that's been made by offseting the offsets by 4 for some reason
                if(AnimationType == AnimType.UVAnimation || AnimationType == AnimType.VisibilityAnimation)
                    Write((int)i.Value - GenericOffset);
                else
                    Write((int)i.Value - GenericOffset - 4);

        //Gets the offset amount by subtracting the current position from the offset count position
        int offsetCount = ((int)Position - ((int)GetOffset("offsetsCount") + 4)) / 4;

        //Writes offset amount
        WriteAt(offsetCount, GetOffset("offsetsCount"));

        //Gets file size, by getting the current writer position
        int fileSize = (int)Position;

        //Writes file size
        WriteAt(fileSize, GetOffset("fileSize"));
    }
}
