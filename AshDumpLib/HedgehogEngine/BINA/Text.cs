using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA;

public class Text : IFile
{
    public const string FileExtension = ".cnvrs-text";

    public string Language = "en";
    public List<Entry> Entries = new();

    public Text() { }

    public Text(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() => Write(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        byte unk0 = reader.Read<byte>();
        byte unk1 = reader.Read<byte>();
        int amount = reader.Read<byte>();
        reader.Align(8);
        long dataOffset = reader.Read<long>();
        Language = reader.ReadStringTableEntry64();
        long unk2 = reader.Read<long>();
        reader.Jump(dataOffset, SeekOrigin.Begin);

        for(int i = 0; i < amount; i++)
        {
            Entry entry = new();
            entry.Read(reader);
            Entries.Add(entry);
        }

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.Write((short)0);
        writer.Write((byte)Entries.Count);
        writer.Align(8);
        writer.AddOffset("dataOffset");
        writer.WriteStringTableEntry(Language);
        writer.Write<long>(0);
        writer.SetOffset("dataOffset");

        foreach (var i in Entries)
            i.Write(writer);

        foreach (var i in Entries)
            i.FinishWrite(writer);

        writer.FinishWrite();
        writer.Dispose();
    }

    public class Entry : IBINASerializable
    {
        public long ID = 0;
        public string Key = "";
        public string Text = "";
        public Font Font = new();
        public List<Character> Characters = new();

        public void Read(BINAReader reader)
        {
            ID = reader.Read<long>();
            Key = reader.ReadStringTableEntry64();
            Font.Read(reader);
            long textPtr = reader.Read<long>();
            long textLength = reader.Read<long>();
            reader.ReadAtOffset(textPtr + 64, () =>
            {
                for (int i = 0; i < textLength; i++)
                {
                    Text += reader.ReadString(StringBinaryFormat.FixedLength, 1);
                    reader.Skip(1);
                }
                    
            });
            long characterPtr = reader.Read<long>();
            if(characterPtr != 0)
            {
                reader.ReadAtOffset(characterPtr + 64, () =>
                {
                    long charAmount = reader.Read<long>();
                    long charsPtr = reader.Read<long>();
                    reader.ReadAtOffset(charsPtr + 64, () =>
                    {
                        for (int i = 0; i < charAmount; i++)
                        {
                            Character chara = new();
                            chara.Read(reader);
                            Characters.Add(chara);
                        }
                    });
                });
            }
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(ID);
            writer.WriteStringTableEntry(Key);
            Font.Write(writer);
            writer.AddOffset(Key + Text);
            writer.AddOffset(Key + Text + Text.Length);
            writer.AddOffset(Key + Text + Characters.Count);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class Character : IBINASerializable
    {
        public string Type = "";
        public long Unk = 0;
        public string Name = "";

        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
            reader.ReadAtOffset(ptr + 64, () =>
            {
                Type = reader.ReadStringTableEntry64();
                Unk = reader.Read<long>();
                Name = reader.ReadStringTableEntry64();
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.AddOffset(Type + Name);
        }

        public void FinishWrite(BINAWriter writer)
        {
            writer.SetOffset(Type + Name);
            writer.WriteStringTableEntry(Type);
            writer.Write(Unk);
            writer.WriteStringTableEntry(Name);
        }
    }

    public class Font : IBINASerializable
    {
        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
        }

        public void Write(BINAWriter writer)
        {
            throw new NotImplementedException();
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}