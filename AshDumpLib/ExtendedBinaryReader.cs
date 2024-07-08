using Amicitia.IO;
using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using System.Text;

namespace AshDumpLib;

public class ExtendedBinaryReader : BinaryObjectReader
{
    public int stringTableOffset = 0;
    public int genericOffset = 0;
    public string Signature = "";

    public ExtendedBinaryReader(string filePath, Endianness endianness, Encoding encoding) : base(filePath, endianness, encoding)
    {
    }

    public ExtendedBinaryReader(string filePath, FileStreamingMode fileStreamingMode, Endianness endianness, Encoding encoding, int bufferSize = 1048576) : base(filePath, fileStreamingMode, endianness, encoding, bufferSize)
    {
    }

    public ExtendedBinaryReader(Stream stream, StreamOwnership streamOwnership, Endianness endianness, Encoding encoding = null, string fileName = null, int blockSize = 1048576) : base(stream, streamOwnership, endianness, encoding, fileName, blockSize)
    {
    }

    public void ReadSignature(string signature)
    {
        //Reads and checks for the signature. If it's wrong, it will throw an exception
        Signature = ReadString(StringBinaryFormat.FixedLength, 4);
        if (signature != Signature)
            throw new Exception("Wrong signature!");
    }

    public virtual void Jump(long offset, SeekOrigin origin)
    {
        Seek(offset + genericOffset, origin);
    }

    public virtual string ReadStringTableEntry()
    {
        long pointer = Read<int>();
        long prePos = Position;
        Jump(pointer + stringTableOffset, SeekOrigin.Begin);
        string value = ReadString(StringBinaryFormat.NullTerminated);
        Seek(prePos, SeekOrigin.Begin);
        return value;
    }
}
