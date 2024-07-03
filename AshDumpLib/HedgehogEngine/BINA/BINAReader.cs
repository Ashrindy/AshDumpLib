using Amicitia.IO;
using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using System.Text;

namespace AshDumpLib.HedgehogEngine.BINA;

public class BINAReader : BinaryObjectReader
{
    public const string BINASignature = "BINA";
    public const string BINAVersion = "210";
    public int FileSize;
    public const string BlockSignature = "DATA";
    public int BlockSize;
    public int RelativeDataOffset = 24;

    int stringTableOffset;
    int stringTableSize;
    int offsetTableSize;

    public string Signature;

    public BINAReader(string filePath, Endianness endianness, Encoding encoding) : base(filePath, endianness, encoding)
    {
    }

    public BINAReader(string filePath, FileStreamingMode fileStreamingMode, Endianness endianness, Encoding encoding, int bufferSize = 1048576) : base(filePath, fileStreamingMode, endianness, encoding, bufferSize)
    {
    }

    public BINAReader(Stream stream, StreamOwnership streamOwnership, Endianness endianness, Encoding encoding = null, string fileName = null, int blockSize = 1048576) : base(stream, streamOwnership, endianness, encoding, fileName, blockSize)
    {
    }

    public void ReadHeader()
    {
        //Reads the signature here and checks if it is the correct one. If it isn't, it will throw an exception
        string binaSignature = ReadString(StringBinaryFormat.FixedLength, 4);
        if(binaSignature != BINASignature) 
        {
            throw new Exception("Not a BINA file!");
        }

        //Now it reads the version, which is pretty much unneccessary right now
        string version = ReadString(StringBinaryFormat.FixedLength, 3);

        //We read the endianess byte, and set the endianess based on said byte. If it's an unknown byte, it will throw an exception
        byte endianess = Read<byte>();
        if(endianess == 'L')
        {
            Endianness = Endianness.Little;
        }
        else if (endianess == 'B')
        {
            Endianness = Endianness.Big;
        }
        else
        {
            throw new Exception("Unknown endianess!");
        }

        //Yet again, pretty much unneccessary, but it reads the FileSize and the DataBlockCount
        FileSize = Read<int>();
        int blockCount = Read<int>();

        //We check for the Data block signature. If it's incorrect, we throw an exception
        string blockSignature = ReadString(StringBinaryFormat.FixedLength, 4);
        if(blockSignature != BlockSignature)
        {
            throw new Exception("Bad block signature!");
        }

        //Again, very unneccessary, but we read it anyway
        BlockSize = Read<int>();

        //Reads all the other values, where only two of them are used
        stringTableOffset = Read<int>();
        stringTableSize = Read<int>();
        offsetTableSize = Read<int>();
        RelativeDataOffset = Read<int>();
        this.Skip(RelativeDataOffset);
    }

    public void ReadSignature(string signature)
    {
        //Reads and checks for the signature. If it's wrong, it will throw an exception
        Signature = ReadString(StringBinaryFormat.FixedLength, 4);
        if(signature != Signature) { throw new Exception("Wrong signature!"); }
    }

    public string ReadStringTableEntry()
    {
        //It reads the string table pointer
        long pointer = Read<long>();

        //Saves the current position
        long prePos = Position;

        //Jumps to the pointer
        this.Jump(pointer, SeekOrigin.Begin);

        //Reads the string
        string value = ReadString(StringBinaryFormat.NullTerminated);

        //Jumps back
        this.Seek(prePos, SeekOrigin.Begin);

        //Returns the string
        return value;
    }

    public void Jump(long offset, SeekOrigin origin)
    {
        this.Seek(offset + 64, origin);
    }
}
