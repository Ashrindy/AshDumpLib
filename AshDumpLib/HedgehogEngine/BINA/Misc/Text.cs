using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using System.Reflection.PortableExecutable;

namespace AshDumpLib.HedgehogEngine.BINA.Misc;

public class Text : IFile
{
    public const string FileExtension = ".cnvrs-text";

    public string Language = "en";
    public List<Entry> Entries = new();

    public Text() { }

    public Text(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

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

        for (int i = 0; i < amount; i++)
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
            Key = reader.ReadStringTableEntry();
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
            if (characterPtr != 0)
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
            writer.AddOffset(Key + Text + Font.FontName);
            writer.AddOffset(Key + Text + Text.Length);
            writer.Write((long)Text.Length);
            writer.AddOffset(Key + Text + Characters.Count);
        }

        public void FinishWrite(BINAWriter writer)
        {
            writer.SetOffset(Key + Text + Text.Length);
            foreach (var i in Text.ToArray())
            {
                writer.Write((byte)i);
                writer.WriteNulls(1);
            }
            writer.SetOffset(Key + Text + Font.FontName);
            Font.entryName = Key;
            Font.Write(writer);
            writer.SetOffset(Key + Text + Characters.Count);
            writer.Write((long)Characters.Count);
            writer.AddOffset(Key + Text + Characters.Count + "data");
            writer.SetOffset(Key + Text + Characters.Count + "data");
            foreach (var i in Characters)
                writer.AddOffset(Key + Text + Characters.Count + "data" + i.Name + i.Type);
            foreach (var i in Characters)
            {
                writer.SetOffset(Key + Text + Characters.Count + "data" + i.Name + i.Type);
                i.Write(writer);
            }
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
            writer.WriteStringTableEntry(Type);
            writer.Write(Unk);
            writer.WriteStringTableEntry(Name);
        }

        public void FinishWrite(BINAWriter writer)
        {

        }
    }

    public class Font : IBINASerializable
    {
        public string entryName = "";
        public string FontName = "";
        public string FontName2 = "";
        public float FontSize = 1;
        public float FontPadding = 1;
        public long unk = 0;
        public long unk4 = 0;
        public int unk2 = 0;
        public int unk3 = 0;
        public long unkEnum = 2;

        public string LayoutName = "";
        public float fUnk = 512;
        public float fUnk1 = 128;
        public int[] iUnks = new int[8];

        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
            reader.ReadAtOffset(ptr + 64, () =>
            {
                entryName = reader.ReadStringTableEntry();
                long fontPtr = reader.Read<long>();
                long layoutPtr = reader.Read<long>();
                long unkPtr = reader.Read<long>();
                if (fontPtr > 0)
                {
                    reader.ReadAtOffset(fontPtr + 64, () =>
                    {
                        FontName = reader.ReadStringTableEntry();
                        FontName2 = reader.ReadStringTableEntry();
                        long fontSizePtr = reader.Read<long>();
                        long unkPtr = reader.Read<long>();
                        long paddingPtr = reader.Read<long>();
                        long unk2Ptr = reader.Read<long>();
                        long unkEnumPtr = reader.Read<long>();
                        long unk3Ptr = reader.Read<long>();
                        long unk4Ptr = reader.Read<long>();
                        if (fontSizePtr > 0)
                            FontSize = reader.ReadValueAtOffset<float>(fontSizePtr);
                        if (unkPtr > 0)
                            unk = reader.ReadValueAtOffset<long>(unkPtr);
                        if (paddingPtr > 0)
                            FontPadding = reader.ReadValueAtOffset<float>(paddingPtr);
                        if (unk2Ptr > 0)
                            unk2 = reader.ReadValueAtOffset<int>(unk2Ptr);
                        if (unkEnumPtr > 0)
                            unkEnum = reader.ReadValueAtOffset<long>(unkEnumPtr);
                        if (unk3Ptr > 0)
                            unk3 = reader.ReadValueAtOffset<int>(unk3Ptr);
                        if (unk4Ptr > 0)
                            unk4 = reader.ReadValueAtOffset<long>(unk4Ptr);
                    });
                }
                if (layoutPtr > 0)
                {
                    reader.ReadAtOffset(layoutPtr + 64, () =>
                    {
                        LayoutName = reader.ReadStringTableEntry();
                        long[] unkPtrs = reader.ReadArray<long>(10);
                        for (int i = 0; i < 10; i++)
                        {
                            if (unkPtrs[i] > 0)
                            {
                                switch (i)
                                {
                                    case 0:
                                        fUnk = reader.ReadValueAtOffset<float>(unkPtrs[i]);
                                        break;

                                    case 1:
                                        fUnk1 = reader.ReadValueAtOffset<float>(unkPtrs[i]);
                                        break;

                                    default:
                                        iUnks[i - 2] = reader.ReadValueAtOffset<int>(unkPtrs[i]);
                                        break;
                                }
                            }
                        }
                    });
                }
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(entryName);
            writer.AddOffset(entryName + FontName + FontName2);
            writer.AddOffset(entryName + FontName + FontName2 + LayoutName);
            writer.WriteNulls(8);

            writer.SetOffset(entryName + FontName + FontName2);
            writer.WriteStringTableEntry(FontName);
            writer.WriteStringTableEntry(FontName2);

            writer.AddOffset(entryName + FontName + FontName2 + "0");
            writer.AddOffset(entryName + FontName + FontName2 + "1");
            writer.AddOffset(entryName + FontName + FontName2 + "2");
            writer.AddOffset(entryName + FontName + FontName2 + "3");
            writer.AddOffset(entryName + FontName + FontName2 + "4");
            writer.AddOffset(entryName + FontName + FontName2 + "5");
            writer.AddOffset(entryName + FontName + FontName2 + "6");

            writer.SetOffset(entryName + FontName + FontName2 + "0");
            writer.Write(FontSize);
            writer.Align(8);
            writer.SetOffset(entryName + FontName + FontName2 + "1");
            writer.Write(unk);
            writer.Align(8);
            writer.SetOffset(entryName + FontName + FontName2 + "2");
            writer.Write(FontPadding);
            writer.Align(8);
            writer.SetOffset(entryName + FontName + FontName2 + "3");
            writer.Write(unk2);
            writer.Align(8);
            writer.SetOffset(entryName + FontName + FontName2 + "4");
            writer.Write(unkEnum);
            writer.Align(8);
            writer.SetOffset(entryName + FontName + FontName2 + "5");
            writer.Write(unk3);
            writer.Align(8);
            writer.SetOffset(entryName + FontName + FontName2 + "6");
            writer.Write(unk4);
            writer.Align(8);

            writer.SetOffset(entryName + FontName + FontName2 + LayoutName);
            writer.WriteStringTableEntry(LayoutName);
            for (int i = 0; i < 10; i++)
                writer.AddOffset(entryName + FontName + FontName2 + LayoutName + i.ToString());
            for (int i = 0; i < 10; i++)
            {
                writer.SetOffset(entryName + FontName + FontName2 + LayoutName + i.ToString());
                switch (i)
                {
                    case 0:
                        writer.Write(fUnk);
                        break;

                    case 1:
                        writer.Write(fUnk1);
                        break;

                    default:
                        writer.Write(iUnks[i - 2]);
                        break;
                }
                writer.Align(8);
            }
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}