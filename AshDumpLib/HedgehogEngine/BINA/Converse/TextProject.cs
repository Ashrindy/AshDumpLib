using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using static AshDumpLib.Helpers.MathA;
using System.Data.SqlTypes;

namespace AshDumpLib.HedgehogEngine.BINA.Converse;

public class TextProject : IFile
{
    public const string FileExtension = ".cnvrs-proj";

    public long Version = 4;
    public ProjectSettings ProjSettings = new();
    public LanguageSettings LangSettings = new();

    public TextProject() { }

    public TextProject(string filename) => Open(filename);
    public TextProject(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();

        Version = reader.Read<long>();
        ProjSettings.Read(reader);
        LangSettings.Read(reader);

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        
        writer.Write(Version);
        writer.AddOffset("projSettings");
        writer.AddOffset("langSettings");
        writer.SetOffset("projSettings");
        ProjSettings.Write(writer);
        writer.SetOffset("langSettings");
        LangSettings.Write(writer);

        writer.FinishWrite();
        writer.Dispose();
    }

    public class ProjectSettings
    {
        public class Color : IBINASerializable
        {
            public string Name = "";
            public Color8 Value = new();

            public void Read(BINAReader reader)
            {
                long ptr = reader.Read<long>();
                reader.ReadAtOffset(ptr + 64, () =>
                {
                    Name = reader.ReadStringTableEntry();
                    Value = reader.Read<Color8>();
                    //the rest aligning and space for a pointer that the game utilizes afterwards
                });
            }

            public void Write(BINAWriter writer)
            {
                writer.AddOffset(Name + Value.r.ToString());
            }

            public void FinishWrite(BINAWriter writer)
            {
                writer.SetOffset(Name + Value.r.ToString());
                writer.WriteStringTableEntry(Name);
                writer.Write(Value);
                writer.WriteNulls(12);
            }
        }

        public class Language : IBINASerializable
        {
            public string Name = "";
            public string ShortName = "";
            public long ID = 0;

            public void Read(BINAReader reader)
            {
                long ptr = reader.Read<long>();
                reader.ReadAtOffset(ptr + 64, () =>
                {
                    Name = reader.ReadStringTableEntry();
                    ShortName = reader.ReadStringTableEntry();
                    ID = reader.Read<long>();
                    //the rest aligning and space for a pointer that the game utilizes afterwards
                });
            }

            public void Write(BINAWriter writer)
            {
                writer.AddOffset(Name + ShortName + ID);
            }

            public void FinishWrite(BINAWriter writer)
            {
                writer.SetOffset(Name + ShortName + ID);
                writer.WriteStringTableEntry(Name);
                writer.WriteStringTableEntry(ShortName);
                writer.Write(ID);
                writer.WriteNulls(8);
            }
        }

        public List<Color> Colors = new();
        public List<Language> Languages = new();

        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
            reader.ReadAtOffset(ptr + 64, () =>
            {
                long colorptr = reader.Read<long>();
                long langptr = reader.Read<long>();
                reader.ReadAtOffset(colorptr + 64, () =>
                {
                    Colors = reader.ReadBINAArrayStruct64<Color>(false);
                });
                reader.ReadAtOffset(langptr + 64, () =>
                {
                    Languages = reader.ReadBINAArrayStruct64<Language>(false);
                });
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.AddOffset("colors");
            writer.AddOffset("langs");
            writer.WriteNulls(8);
            writer.SetOffset("colors");
            writer.Write((long)Colors.Count);
            writer.AddOffset("colorsList");
            writer.WriteNulls(8);
            writer.SetOffset("colorsList");
            foreach (var i in Colors)
                i.Write(writer);
            foreach (var i in Colors)
                i.FinishWrite(writer);
            writer.SetOffset("langs");
            writer.Write((long)Languages.Count);
            writer.AddOffset("langsList");
            writer.WriteNulls(8);
            writer.SetOffset("langsList");
            foreach (var i in Languages)
                i.Write(writer);
            foreach (var i in Languages)
                i.FinishWrite(writer);
        }
    }

    public class LanguageSettings : IBINASerializable
    {
        public interface ILang : IBINASerializable
        {
            public void FinishWrite(BINAWriter writer, ref Dictionary<string, long> values);
        }

        public class LanguageSetting<T> where T : ILang, new()
        {
            public string LanguageName = "";
            public List<T> Values = new();

            public void Read(BINAReader reader)
            {
                long ptr = reader.Read<long>();
                reader.ReadAtOffset(ptr + 64, () =>
                {
                    LanguageName = reader.ReadStringTableEntry();
                    Values = reader.ReadBINAArrayStruct64<T>(false);
                });
            }

            public void Write(BINAWriter writer)
            {
                writer.AddOffset(LanguageName + typeof(T).ToString() + Values.Count);
            }

            public void FinishWrite(BINAWriter writer)
            {
                writer.SetOffset(LanguageName + typeof(T).ToString() + Values.Count);
                writer.WriteStringTableEntry(LanguageName);
                writer.Write((long)Values.Count);
                writer.AddOffset("values" + LanguageName + typeof(T).ToString() + Values.Count);
                writer.WriteNulls(8);
                writer.SetOffset("values" + LanguageName + typeof(T).ToString() + Values.Count);
                foreach (var i in Values)
                    i.Write(writer);
            }

            public void FinishWriteValues(BINAWriter writer, ref Dictionary<string, long> values)
            {
                foreach (var i in Values)
                    i.FinishWrite(writer, ref values);
            }
        }

        public class Font : ILang
        {
            long id = Random.Shared.NextInt64();

            public string IDName = "";
            public string FontName = "";
            public float? Unk0 = null;
            public float? Unk1 = null;
            public float? Unk2 = null;
            public int? Unk3 = null;
            public int? Unk4 = null;
            public int? Unk5 = null;
            public int? Unk6 = null;
            public int? Unk7 = null;
            public int? Unk8 = null;
            public int? Unk9 = null;
            public int? Unk10 = null;
            public int? Unk11 = null;

            public Font() { }

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
                reader.ReadAtOffset(ptr + 64, () =>
                {
                    IDName = reader.ReadStringTableEntry();
                    FontName = reader.ReadStringTableEntry();
                    Unk0 = ReadValue<float>(reader);
                    Unk1 = ReadValue<float>(reader);
                    Unk2 = ReadValue<float>(reader);
                    Unk3 = ReadValue<int>(reader);
                    Unk4 = ReadValue<int>(reader);
                    Unk5 = ReadValue<int>(reader);
                    Unk6 = ReadValue<int>(reader);
                    Unk7 = ReadValue<int>(reader);
                    Unk8 = ReadValue<int>(reader);
                    Unk8 = ReadValue<int>(reader);
                    Unk9 = ReadValue<int>(reader);
                    Unk10 = ReadValue<int>(reader);
                    Unk11 = ReadValue<int>(reader);
                });
            }

            public void Write(BINAWriter writer)
            {
                writer.AddOffset(IDName + FontName + id);
            }

            void WriteValue<T>(BINAWriter writer, string name, T? value) where T : unmanaged
            {
                if(value == null)
                    writer.WriteNulls(8);
                else
                    writer.AddOffset(IDName + FontName + id + name);
            }

            void FinishWriteValue<T>(BINAWriter writer, string name, T? value) where T : unmanaged
            {
                if (value != null)
                {
                    writer.SetOffset(IDName + FontName + id + name);
                    writer.Write((T)value);
                    writer.WriteNulls(4);
                }
            }

            public void FinishWrite(BINAWriter writer, ref Dictionary<string, long> values)
            {
                if (values.ContainsKey(IDName + FontName))
                {
                    long prePos = writer.Position;
                    writer.Seek(values[IDName + FontName], SeekOrigin.Begin);
                    writer.SetOffset(IDName + FontName + id);
                    writer.Seek(prePos, SeekOrigin.Begin);
                }
                else
                {
                    values.Add(IDName + FontName, writer.Position);
                    writer.SetOffset(IDName + FontName + id);
                    writer.WriteStringTableEntry(IDName);
                    writer.WriteStringTableEntry(FontName);
                    WriteValue(writer, "0", Unk0);
                    WriteValue(writer, "1", Unk1);
                    WriteValue(writer, "2", Unk2);
                    WriteValue(writer, "3", Unk3);
                    WriteValue(writer, "4", Unk4);
                    WriteValue(writer, "5", Unk5);
                    WriteValue(writer, "6", Unk6);
                    WriteValue(writer, "7", Unk7);
                    WriteValue(writer, "8", Unk8);
                    WriteValue(writer, "9", Unk9);
                    WriteValue(writer, "10", Unk10);
                    WriteValue(writer, "11", Unk11);
                    writer.WriteNulls(8);

                    FinishWriteValue(writer, "0", Unk0);
                    FinishWriteValue(writer, "1", Unk1);
                    FinishWriteValue(writer, "2", Unk2);
                    FinishWriteValue(writer, "3", Unk3);
                    FinishWriteValue(writer, "4", Unk4);
                    FinishWriteValue(writer, "5", Unk5);
                    FinishWriteValue(writer, "6", Unk6);
                    FinishWriteValue(writer, "7", Unk7);
                    FinishWriteValue(writer, "8", Unk8);
                    FinishWriteValue(writer, "9", Unk9);
                    FinishWriteValue(writer, "10", Unk10);
                    FinishWriteValue(writer, "11", Unk11);
                }
            }

            public void FinishWrite(BINAWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public class Layout : ILang
        {
            long id = Random.Shared.NextInt64();

            public string IDName = "";
            public float? Unk0 = null;
            public float? Unk1 = null;
            public int? Unk2 = null;
            public int? Unk3 = null;
            public int? Unk4 = null;
            public int? Unk5 = null;
            public int? Unk6 = null;
            public int? Unk7 = null;
            public int? Unk8 = null;

            public Layout() { }

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
                reader.ReadAtOffset(ptr + 64, () =>
                {
                    IDName = reader.ReadStringTableEntry();
                    reader.Skip(8);
                    Unk0 = ReadValue<float>(reader);
                    Unk1 = ReadValue<float>(reader);
                    Unk2 = ReadValue<int>(reader);
                    Unk3 = ReadValue<int>(reader);
                    Unk4 = ReadValue<int>(reader);
                    Unk5 = ReadValue<int>(reader);
                    Unk6 = ReadValue<int>(reader);
                    Unk7 = ReadValue<int>(reader);
                    Unk8 = ReadValue<int>(reader);
                });
            }

            public void Write(BINAWriter writer)
            {
                writer.AddOffset(IDName + id);
            }

            void WriteValue<T>(BINAWriter writer, string name, T? value) where T : unmanaged
            {
                if (value == null)
                    writer.WriteNulls(8);
                else
                    writer.AddOffset(IDName + id + name);
            }

            void FinishWriteValue<T>(BINAWriter writer, string name, T? value) where T : unmanaged
            {
                if (value != null)
                {
                    writer.SetOffset(IDName + id + name);
                    writer.Write((T)value);
                    writer.WriteNulls(4);
                }
            }

            public void FinishWrite(BINAWriter writer, ref Dictionary<string, long> values)
            {
                if (values.ContainsKey(IDName))
                {
                    long prePos = writer.Position;
                    writer.Seek(values[IDName], SeekOrigin.Begin);
                    writer.SetOffset(IDName + id);
                    writer.Seek(prePos, SeekOrigin.Begin);
                }
                else
                {
                    values.Add(IDName, writer.Position);
                    writer.SetOffset(IDName + id);
                    writer.WriteStringTableEntry(IDName);
                    writer.WriteNulls(8);
                    WriteValue(writer, "0", Unk0);
                    WriteValue(writer, "1", Unk1);
                    WriteValue(writer, "2", Unk2);
                    WriteValue(writer, "3", Unk3);
                    WriteValue(writer, "4", Unk4);
                    WriteValue(writer, "5", Unk5);
                    WriteValue(writer, "6", Unk6);
                    WriteValue(writer, "7", Unk7);
                    WriteValue(writer, "8", Unk8);
                    writer.WriteNulls(8);

                    FinishWriteValue(writer, "0", Unk0);
                    FinishWriteValue(writer, "1", Unk1);
                    FinishWriteValue(writer, "2", Unk2);
                    FinishWriteValue(writer, "3", Unk3);
                    FinishWriteValue(writer, "4", Unk4);
                    FinishWriteValue(writer, "5", Unk5);
                    FinishWriteValue(writer, "6", Unk6);
                    FinishWriteValue(writer, "7", Unk7);
                    FinishWriteValue(writer, "8", Unk8);
                }
            }

            public void FinishWrite(BINAWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public List<LanguageSetting<Font>> Fonts = new();
        public List<LanguageSetting<Layout>> Layouts = new();

        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
            reader.ReadAtOffset(ptr + 64, () =>
            {
                long count = reader.Read<long>();
                long fontsPtr = reader.Read<long>();
                long layoutsPtr = reader.Read<long>();
                reader.ReadAtOffset(fontsPtr + 64, () =>
                {
                    for(int i = 0; i < count; i++)
                    {
                        LanguageSetting<Font> setting = new();
                        setting.Read(reader);
                        Fonts.Add(setting);
                    }
                });
                reader.ReadAtOffset(layoutsPtr + 64, () =>
                {
                    for (int i = 0; i < count; i++)
                    {
                        LanguageSetting<Layout> setting = new();
                        setting.Read(reader);
                        Layouts.Add(setting);
                    }
                });
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.Write((long)Fonts.Count);
            writer.AddOffset("fontsLang");
            writer.AddOffset("layoutsLang");
            writer.SetOffset("fontsLang");
            foreach (var i in Fonts)
                i.Write(writer);
            writer.WriteNulls(8);
            writer.SetOffset("layoutsLang");
            foreach (var i in Layouts)
                i.Write(writer);
            writer.WriteNulls(8);
            Dictionary<string, long> alreadyWritten = new();
            foreach (var i in Fonts)
                i.FinishWrite(writer);
            foreach (var i in Layouts)
                i.FinishWrite(writer);
            foreach (var i in Fonts)
                i.FinishWriteValues(writer, ref alreadyWritten);
            foreach (var i in Layouts)
                i.FinishWriteValues(writer, ref alreadyWritten);
        }

        public void FinishWrite(BINAWriter writer)
        {
        }
    }
}
