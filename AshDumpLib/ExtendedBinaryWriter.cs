using Amicitia.IO;
using Amicitia.IO.Binary;
using Amicitia.IO.Streams;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AshDumpLib
{
    public class ExtendedBinaryWriter : BinaryObjectWriter
    {
        public Dictionary<long, long> StringTableOffsets = new();
        Dictionary<string, long> stringTableRaw = new();
        public string StringTable = "";
        public Dictionary<string, long> Offsets = new();
        public Dictionary<string, bool> OffsetsWrite = new();
        public Dictionary<string, long> OffsetValues = new();
        public Dictionary<Type, Tuple<ExtendedBinaryWriter, MemoryStream>> arrays = new();
        public Dictionary<Type, List<Tuple<string, long>>> arrayOffset = new();
        public string CurFilePath = "";

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

        public virtual void WriteStringTableEntry(string rawEntry)
        {
            //Adds offset to the OffsetTable
            Offsets.Add(rawEntry + "." + Position, Position - GenericOffset);
            string entry = rawEntry;

            if (rawEntry == "" && StringTable.Last() != '\0')
                entry = "\0";

            if (!stringTableRaw.ContainsKey(entry))
            {
                //Adds offset to the StringTableOffset for later correction
                StringTableOffsets.Add(Position, StringTable.Length);

                //Adds offset to the OffsetsWrite dictionary
                OffsetsWrite.Add(entry + "." + Position, true);

                //Writes the temporary offset in the StringTable
                stringTableRaw.Add(entry, StringTable.Length);
                Write<long>(StringTable.Length);
                foreach (var i in entry.ToCharArray())
                    StringTable += i;

                StringTable += '\0';
            }
            else
            {
                //Adds offset to the StringTableOffset for later correction
                StringTableOffsets.Add(Position, stringTableRaw[entry]);

                //Adds offset to the OffsetsWrite dictionary
                OffsetsWrite.Add(entry + "." + Position, true);

                //Writes the temporary offset in the StringTable
                Write<long>(stringTableRaw[entry]);
            }
        }

        public void WriteNulls(int amount)
        {
            WriteArray(new byte[amount]);
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
            WriteNulls(8);
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

        public virtual void SetOffset32(string id)
        {
            long offset = Position;
            Seek(Offsets[id], SeekOrigin.Begin);
            Write((int)offset);
            OffsetValues[id] = offset;
            Seek(offset, SeekOrigin.Begin);
        }

        public virtual void WriteHeader()
        {

        }

        public virtual void FinishWrite()
        {

        }

        public virtual void WriteObjectArrayPtr<T>(List<T> values, string id) where T : IExtendedBinarySerializable
        {
            AddOffset(id);
            ExtendedBinaryWriter writer;
            if (arrays.ContainsKey(typeof(T)))
                writer = arrays[typeof(T)].Item1;
            else
            {
                MemoryStream stream = new();
                writer = new(stream, StreamOwnership.Retain, Endianness);
                arrays.Add(typeof(T), new(writer, stream));
            }
            long pos = writer.Position;
            foreach (var i in values)
                i.Write(writer);
            if (arrayOffset.ContainsKey(typeof(T)))
                arrayOffset[typeof(T)].Add(new(id, pos));
            else
                arrayOffset.Add(typeof(T), new() { new(id, pos) });
        }

        public virtual void WriteArrayPtr<T>(List<T> values, string id) where T : unmanaged
        {
            AddOffset(id);
            ExtendedBinaryWriter writer;
            if (arrays.ContainsKey(typeof(T)))
                writer = arrays[typeof(T)].Item1;
            else
            {
                MemoryStream stream = new();
                writer = new(stream, StreamOwnership.Retain, Endianness);
                arrays.Add(typeof(T), new(writer, stream));
            }
            long pos = writer.Position;
            foreach (var i in values)
                writer.Write(i);
            if (arrayOffset.ContainsKey(typeof(T)))
                arrayOffset[typeof(T)].Add(new(id, pos));
            else
                arrayOffset.Add(typeof(T), new() { new(id, pos) });
        }

        public virtual void FinishArrays()
        {
            foreach (var i in arrays)
            {
                i.Value.Item1.FinishArrays();
                i.Value.Item1.Dispose();
                long prePos = Position;
                WriteArray(i.Value.Item2.ToArray());
                long prePos1 = Position;
                foreach(var x in arrayOffset[i.Key])
                {
                    Seek(prePos + x.Item2, SeekOrigin.Begin);
                    SetOffset(x.Item1);
                }
                Seek(prePos1, SeekOrigin.Begin);
            }
        }
    }
}
