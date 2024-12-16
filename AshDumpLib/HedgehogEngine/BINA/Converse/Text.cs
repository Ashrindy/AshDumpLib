using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using Newtonsoft.Json.Linq;
using static AshDumpLib.HedgehogEngine.BINA.Converse.TextProject.LanguageSettings.Font;
using static AshDumpLib.HedgehogEngine.BINA.Converse.TextProject.LanguageSettings.Layout;

namespace AshDumpLib.HedgehogEngine.BINA.Converse;

public class Text : IFile
{
    public const string FileExtension = ".cnvrs-text";

    public string Language = "en";
    public byte unk0 = 6;
    public byte unk1 = 1;
    public List<Entry> Entries = new();

    public Text() { }

    public Text(string filename) => Open(filename);
    public Text(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        unk0 = reader.Read<byte>();
        unk1 = reader.Read<byte>();
        int amount = reader.Read<short>();
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
        writer.Write(unk0);
        writer.Write(unk1);
        writer.Write((short)Entries.Count);
        writer.Align(8);
        writer.AddOffset("dataOffset");
        writer.WriteStringTableEntry(Language);
        writer.Write<long>(0);
        writer.SetOffset("dataOffset");

        foreach (var i in Entries)
            i.Write(writer);

        foreach (var i in Entries)
        {
            writer.SetOffset(i.Key + i.Text + i.Text.Length + i.ID);
            byte[] textbytes = System.Text.Encoding.Unicode.GetBytes(i.Text);
            writer.WriteArray(textbytes);
            writer.Align(8);
        }

        Dictionary<string, long> alreadyWritten = new();
        foreach (var i in Entries)
        {
            writer.SetOffset(i.Key + i.Text + i.FontLayout.FontInfo.Data.FontName);
            i.FontLayout.EntryName = i.Key;
            i.FontLayout.Write(writer);
        }

        foreach(var i in Entries)
            i.FontLayout.FinishWrite(writer, ref alreadyWritten);

        foreach (var i in Entries)
            i.FinishWrite(writer);

        writer.FinishWrite();
        writer.Dispose();
    }

    public class Entry
    {
        public long ID = 0;
        public string Key = "";
        public string Text = "";
        public FontLayout FontLayout = new();
        public List<Character> Characters = new();

        long id = Random.Shared.NextInt64();

        public static long GenerateID(string text)
        {
            int hash = 0;
            foreach(var i in text)
            {
                hash = (hash * 0x7F) + i;
            }
            return hash;
        }

        public void Read(BINAReader reader)
        {
            ID = reader.Read<long>();
            Key = reader.ReadStringTableEntry();
            Font.Read(reader);
            long textPtr = reader.Read<long>();
            long textLength = reader.Read<long>();
            reader.ReadAtOffset(textPtr + 64, () =>
            {
                byte[] textbytes = reader.ReadArray<byte>((int)textLength * 2);
                Text = System.Text.Encoding.Unicode.GetString(textbytes);
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
            writer.AddOffset(Key + Text + FontLayout.FontInfo.Data.FontName);
            writer.AddOffset(Key + Text + Text.Length + ID);
            writer.Write((long)Text.Length);
            writer.AddOffset(Key + Text + Characters.Count + ID + id);
        }

        public void FinishWrite(BINAWriter writer)
        {
            writer.SetOffset(Key + Text + Characters.Count + ID + id);
            writer.Write((long)Characters.Count);
            writer.AddOffset(Key + Text + Characters.Count + ID + id + "data");
            writer.SetOffset(Key + Text + Characters.Count + ID + id + "data");
            foreach (var x in Characters)
                writer.AddOffset(Key + Text + Characters.Count + ID + "data" + x.Name + x.Type);
            foreach (var i in Characters)
            {
                writer.SetOffset(Key + Text + Characters.Count + ID + "data" + i.Name + i.Type);
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

    public class FontLayout
    {
        public class Font
        {
            long id = Random.Shared.NextInt64();

            public FontInfo Data = new();

            T? ReadValue<T>(BINAReader reader) where T : unmanaged
            {
                long ptr = reader.Read<long>();
                if (ptr == 0)
                    return null;
                else
                    return reader.ReadValueAtOffset<T>(ptr + 64);
            }

            public void Read(BINAReader reader)
            {
                long ptr = reader.Read<long>();
                if (ptr > 0)
                {
                    reader.ReadAtOffset(ptr + 64, () =>
                    {
                        Data.IDName = reader.ReadStringTableEntry();
                        Data.FontName = reader.ReadStringTableEntry();
                        Data.Unk0 = ReadValue<float>(reader);
                        Data.Unk1 = ReadValue<float>(reader);
                        Data.Unk2 = ReadValue<float>(reader);
                        Data.Unk3 = ReadValue<int>(reader);
                        Data.Unk4 = ReadValue<int>(reader);
                        Data.Unk5 = ReadValue<int>(reader);
                        Data.Unk6 = ReadValue<int>(reader);
                        Data.Unk7 = ReadValue<int>(reader);
                        Data.Unk8 = ReadValue<int>(reader);
                        Data.Unk8 = ReadValue<int>(reader);
                        Data.Unk9 = ReadValue<int>(reader);
                        Data.Unk10 = ReadValue<int>(reader);
                        Data.Unk11 = ReadValue<int>(reader);
                    });
                }
            }

            public void Write(BINAWriter writer)
            {
                writer.AddOffset(Data.IDName + Data.FontName + id);
            }

            void WriteValue<T>(BINAWriter writer, string name, T? value) where T : unmanaged
            {
                if (value == null)
                    writer.WriteNulls(8);
                else
                    writer.AddOffset(Data.IDName + Data.FontName + id + name);
            }

            void FinishWriteValue<T>(BINAWriter writer, string name, T? value) where T : unmanaged
            {
                if (value != null)
                {
                    writer.SetOffset(Data.IDName + Data.FontName + id + name);
                    writer.Write((T)value);
                    writer.WriteNulls(4);
                }
            }

            public void FinishWrite(BINAWriter writer, ref Dictionary<string, long> values)
            {
                if (values.ContainsKey(Data.IDName + Data.FontName))
                {
                    long prePos = writer.Position;
                    writer.Seek(values[Data.IDName + Data.FontName], SeekOrigin.Begin);
                    writer.SetOffset(Data.IDName + Data.FontName + id);
                    writer.Seek(prePos, SeekOrigin.Begin);
                }
                else
                {
                    values.Add(Data.IDName + Data.FontName, writer.Position);
                    writer.SetOffset(Data.IDName + Data.FontName + id);
                    writer.WriteStringTableEntry(Data.IDName);
                    writer.WriteStringTableEntry(Data.FontName);
                    WriteValue(writer, "0", Data.Unk0);
                    WriteValue(writer, "1", Data.Unk1);
                    WriteValue(writer, "2", Data.Unk2);
                    WriteValue(writer, "3", Data.Unk3);
                    WriteValue(writer, "4", Data.Unk4);
                    WriteValue(writer, "5", Data.Unk5);
                    WriteValue(writer, "6", Data.Unk6);
                    WriteValue(writer, "7", Data.Unk7);
                    WriteValue(writer, "8", Data.Unk8);
                    WriteValue(writer, "9", Data.Unk9);
                    WriteValue(writer, "10", Data.Unk10);
                    WriteValue(writer, "11", Data.Unk11);
                    writer.WriteNulls(8);

                    FinishWriteValue(writer, "0", Data.Unk0);
                    FinishWriteValue(writer, "1", Data.Unk1);
                    FinishWriteValue(writer, "2", Data.Unk2);
                    FinishWriteValue(writer, "3", Data.Unk3);
                    FinishWriteValue(writer, "4", Data.Unk4);
                    FinishWriteValue(writer, "5", Data.Unk5);
                    FinishWriteValue(writer, "6", Data.Unk6);
                    FinishWriteValue(writer, "7", Data.Unk7);
                    FinishWriteValue(writer, "8", Data.Unk8);
                    FinishWriteValue(writer, "9", Data.Unk9);
                    FinishWriteValue(writer, "10", Data.Unk10);
                    FinishWriteValue(writer, "11", Data.Unk11);
                }
            }
        }
        public class Layout
        {
            public LayoutInfo Data = new();

            long id = Random.Shared.NextInt64();

            T? ReadValue<T>(BINAReader reader) where T : unmanaged
            {
                long ptr = reader.Read<long>();
                if (ptr == 0)
                    return null;
                else
                    return reader.ReadValueAtOffset<T>(ptr + 64);
            }

            public void Read(BINAReader reader)
            {
                long ptr = reader.Read<long>();
                if (ptr > 0)
                {
                    reader.ReadAtOffset(ptr + 64, () =>
                    {
                        Data.IDName = reader.ReadStringTableEntry();
                        reader.Skip(8);
                        Data.Unk0 = ReadValue<float>(reader);
                        Data.Unk1 = ReadValue<float>(reader);
                        Data.Unk2 = ReadValue<int>(reader);
                        Data.Unk3 = ReadValue<int>(reader);
                        Data.Unk4 = ReadValue<int>(reader);
                        Data.Unk5 = ReadValue<int>(reader);
                        Data.Unk6 = ReadValue<int>(reader);
                        Data.Unk7 = ReadValue<int>(reader);
                        Data.Unk8 = ReadValue<int>(reader);
                    });
                }
            }

            public void Write(BINAWriter writer)
            {
                writer.AddOffset(Data.IDName + id);
            }

            void WriteValue<T>(BINAWriter writer, string name, T? value) where T : unmanaged
            {
                if (value == null)
                    writer.WriteNulls(8);
                else
                    writer.AddOffset(Data.IDName + id + name);
            }

            void FinishWriteValue<T>(BINAWriter writer, string name, T? value) where T : unmanaged
            {
                if (value != null)
                {
                    writer.SetOffset(Data.IDName + id + name);
                    writer.Write((T)value);
                    writer.WriteNulls(4);
                }
            }

            public void FinishWrite(BINAWriter writer, ref Dictionary<string, long> values)
            {
                if (values.ContainsKey(Data.IDName))
                {
                    long prePos = writer.Position;
                    writer.Seek(values[Data.IDName], SeekOrigin.Begin);
                    writer.SetOffset(Data.IDName + id);
                    writer.Seek(prePos, SeekOrigin.Begin);
                }
                else
                {
                    values.Add(Data.IDName, writer.Position);
                    writer.SetOffset(Data.IDName + id);
                    writer.WriteStringTableEntry(Data.IDName);
                    writer.WriteNulls(8);
                    WriteValue(writer, "0", Data.Unk0);
                    WriteValue(writer, "1", Data.Unk1);
                    WriteValue(writer, "2", Data.Unk2);
                    WriteValue(writer, "3", Data.Unk3);
                    WriteValue(writer, "4", Data.Unk4);
                    WriteValue(writer, "5", Data.Unk5);
                    WriteValue(writer, "6", Data.Unk6);
                    WriteValue(writer, "7", Data.Unk7);
                    WriteValue(writer, "8", Data.Unk8);
                    writer.WriteNulls(8);

                    FinishWriteValue(writer, "0", Data.Unk0);
                    FinishWriteValue(writer, "1", Data.Unk1);
                    FinishWriteValue(writer, "2", Data.Unk2);
                    FinishWriteValue(writer, "3", Data.Unk3);
                    FinishWriteValue(writer, "4", Data.Unk4);
                    FinishWriteValue(writer, "5", Data.Unk5);
                    FinishWriteValue(writer, "6", Data.Unk6);
                    FinishWriteValue(writer, "7", Data.Unk7);
                    FinishWriteValue(writer, "8", Data.Unk8);
                }
            }
        }

        public string EntryName = "";
        public Font FontInfo = new();
        public Layout LayoutInfo = new();

        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
            reader.ReadAtOffset(ptr + 64, () =>
            {
                EntryName = reader.ReadStringTableEntry();
                FontInfo.Read(reader);
                LayoutInfo.Read(reader);
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(EntryName);
            FontInfo.Write(writer);
            LayoutInfo.Write(writer);
            writer.WriteNulls(8);
        }

        public void FinishWrite(BINAWriter writer, ref Dictionary<string, long> alreadyWritten)
        {
            FontInfo.FinishWrite(writer, ref alreadyWritten);
            LayoutInfo.FinishWrite(writer, ref alreadyWritten);
        }
    }
}