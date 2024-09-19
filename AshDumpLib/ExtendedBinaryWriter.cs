using Amicitia.IO;
using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using System.Text;

namespace AshDumpLib
{
    public class ExtendedBinaryWriter : BinaryObjectWriter
    {
        public Dictionary<long, long> StringTableOffsets = new();
        public string StringTable = "";
        public Dictionary<string, long> Offsets = new();
        public Dictionary<string, bool> OffsetsWrite = new();
        public Dictionary<string, long> OffsetValues = new();

        public int GenericOffset = 0;
        public int FileVersion = 0;

        public ExtendedBinaryWriter(string filePath, Endianness endianness, Encoding encoding) : base(filePath, endianness, encoding)
        {
        }

        public ExtendedBinaryWriter(string filePath, FileStreamingMode fileStreamingMode, Endianness endianness, Encoding encoding, int bufferSize = 1048576) : base(filePath, fileStreamingMode, endianness, encoding, bufferSize)
        {
        }

        public ExtendedBinaryWriter(Stream stream, StreamOwnership streamOwnership, Endianness endianness, Encoding encoding = null, string fileName = null, int blockSize = 1048576) : base(stream, streamOwnership, endianness, encoding, fileName, blockSize)
        {
        }

        public virtual void WriteStringTableEntry(string entry)
        {
            //Adds offset to the OffsetTable
            Offsets.Add(entry + "." + Position, Position - GenericOffset);

            if (!StringTable.Contains(entry + "\0"))
            {
                //Adds offset to the StringTableOffset for later correction
                StringTableOffsets.Add(Position, StringTable.Length);

                //Adds offset to the OffsetsWrite dictionary
                OffsetsWrite.Add(entry + "." + Position, true);

                //Writes the temporary offset in the StringTable
                Write<long>(StringTable.Length);
                foreach (var i in entry.ToCharArray())
                    StringTable += i;

                StringTable += '\0';
            }
            else
            {
                //Adds offset to the StringTableOffset for later correction
                StringTableOffsets.Add(Position, StringTable.IndexOf(entry));

                //Adds offset to the OffsetsWrite dictionary
                OffsetsWrite.Add(entry + "." + Position, true);

                //Writes the temporary offset in the StringTable
                Write<long>(StringTable.IndexOf(entry));
            }
        }

        public void WriteNulls(int amount)
        {
            for (int i = 0; i < amount; i++)
                WriteChar('\0');
        }

        
        public void WriteAt<T>(T value, long offset) where T : unmanaged
        {
            long prePos = Position;
            Seek(offset, SeekOrigin.Begin);
            Write(value);
            Seek(prePos, SeekOrigin.Begin);
        }

        public void WriteSignature(string signature)
        {
            //Writes file's unique signature
            WriteString(StringBinaryFormat.FixedLength, signature, 4);
        }

        public void WriteChar(char value)
        {
            Write(value);
            this.Skip(-1);
        }

        public void FixPadding(int padding)
        {
            int amount = 0;
            while ((Position + amount) % padding != 0)
                amount++;
            WriteNulls(amount);
        }

        public virtual void Jump(long offset, SeekOrigin origin)
        {
            Seek(offset + GenericOffset, origin);
        }

        public virtual void AddOffset(string id, bool write = true)
        {
            Offsets.Add(id, Position);
            OffsetValues.Add(id, 0);
            OffsetsWrite.Add(id, write);
            this.Skip(4);
        }

        public long GetOffset(string id)
        {
            return Offsets[id];
        }

        public long GetOffsetValue(string id)
        {
            return OffsetValues[id];
        }

        public virtual void SetOffset(string id)
        {
            long offset = Position;
            Seek(Offsets[id], SeekOrigin.Begin);
            Write(offset);
            OffsetValues[id] = offset;
            Seek(offset, SeekOrigin.Begin);
        }

        public virtual void WriteHeader()
        {

        }

        public virtual void FinishWrite()
        {

        }
    }
}
