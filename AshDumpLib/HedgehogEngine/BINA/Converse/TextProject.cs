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
            public struct FontInfo
            {
                public string IDName = "";
                public string FontName = "";
                public float? DefaultSize;
                public float? Unk1;
                public float? Unk2;
                public int? Unk3;
                public int? Unk4;
                public int? Unk5;
                public int? Unk6;
                public int? Unk7;
                public int? Unk8;
                public int? Unk9;
                public int? Unk10;
                public int? Unk11;

                public FontInfo() { }
            }

            long id = Random.Shared.NextInt64();

            public FontInfo Data = new();

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
                    Data.IDName = reader.ReadStringTableEntry();
                    Data.FontName = reader.ReadStringTableEntry();
                    Data.DefaultSize = ReadValue<float>(reader);
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

            public void Write(BINAWriter writer)
            {
                writer.AddOffset(Data.IDName + Data.FontName + id);
            }

            void WriteValue<T>(BINAWriter writer, string name, T? value) where T : unmanaged
            {
                if(value == null)
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
                    WriteValue(writer, "0", Data.DefaultSize);
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

                    FinishWriteValue(writer, "0", Data.DefaultSize);
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

            public void FinishWrite(BINAWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public class Layout : ILang
        {
            public struct LayoutInfo
            {
                public string IDName = "";
                public float? Unk0;
                public float? Unk1;
                public int? Unk2;
                public int? Unk3;
                public int? Unk4;
                public int? Unk5;
                public int? Unk6;
                public int? Unk7;
                public int? Unk8;

                public LayoutInfo() { }
            }

            long id = Random.Shared.NextInt64();

            public LayoutInfo Data = new();

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
