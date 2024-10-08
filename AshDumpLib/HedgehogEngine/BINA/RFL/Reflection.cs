﻿using AshDumpLib.Helpers.Archives;
using Newtonsoft.Json;
using System.Numerics;
using Amicitia.IO.Binary;

using static AshDumpLib.HedgehogEngine.MathA;
using static AshDumpLib.HedgehogEngine.BINA.RFL.ReflectionData.Template;

namespace AshDumpLib.HedgehogEngine.BINA.RFL;

public class ReflectionData
{
    public Dictionary<string, object> Parameters = new();

    public string TemplateFilePath = "";
    public TemplateJSON TemplateData;
    public string StructName = "";

    Dictionary<Tuple<string, long>, object[]> paramArrays = new();
    long Id = 0;

    public ReflectionData()
    {
        if(TemplateFilePath != "")
            TemplateData = Template.GetTemplateFromFilePath(TemplateFilePath);
        Id = Random.Shared.NextInt64();
    }

    public ReflectionData(string templateFilePath)
    {
        TemplateFilePath = templateFilePath;
        if (TemplateFilePath != "")
            TemplateData = GetTemplateFromFilePath(TemplateFilePath);
        Id = Random.Shared.NextInt64();
    }

    public ReflectionData(TemplateJSON templateData)
    {
        TemplateData = templateData;
        Id = Random.Shared.NextInt64();
    }

    public void Read(BINAReader reader)
    {
        if(TemplateData.tags != null && TemplateData.tags.ContainsKey(StructName))
            Parameters = ReadStruct(reader, TemplateData.tags[StructName]);
        else if(TemplateData.objects != null && TemplateData.objects.ContainsKey(StructName))
            Parameters.Add(TemplateData.objects[StructName].structs, ReadStruct(reader, TemplateData.structs[TemplateData.objects[StructName].structs]));
        else
            Parameters.Add(StructName, ReadStruct(reader, TemplateData.structs[StructName]));
    }

    public void Write(BINAWriter writer)
    {
        if (Parameters.Count > 0)
        {
            WriteStruct(writer, (Dictionary<string, object>)Parameters.ElementAt(0).Value, Parameters.ElementAt(0).Key);
            foreach (var i in paramArrays)
            {
                writer.SetOffset(Id.ToString() + i.Key.Item1 + i.Key.Item2.ToString());
                foreach (var x in i.Value)
                    WriteField(writer, new() { type = i.Key.Item1.Substring(i.Key.Item1.IndexOf('.') + 1) }, x);
            }
        }
    } 

    #region TemplateReader
    public static class Template
    {
        public static int GetVersionFromTemplate(string version)
        {
            int ver = 3;
            switch (version)
            {
                case "gedit_v2":
                    ver = 2;
                    break;

                case "gedit_v3":
                    ver = 3;
                    break;

                default:
                    throw new NotImplementedException();
                    break;
            }
            return ver;
        }

        public static TemplateJSON GetTemplateFromFilePath(string filepath)
        {
            return JsonConvert.DeserializeObject<TemplateJSON>(File.ReadAllText(filepath));
        }

        [Serializable]
        public class TemplateJSON
        {
            public int version;
            public string format;
            public Dictionary<string, EnumTemplate> enums = new Dictionary<string, EnumTemplate>();
            public Dictionary<string, StructTemplate> structs = new Dictionary<string, StructTemplate>();
            public Dictionary<string, ObjectTemplate> objects = new Dictionary<string, ObjectTemplate>();
            public Dictionary<string, StructTemplate>? tags = new Dictionary<string, StructTemplate>();
        }

        [Serializable]
        public class EnumTemplate
        {
            public string type;
            public Dictionary<string, EnumTemplateValue> values = new Dictionary<string, EnumTemplateValue>();
        }

        [Serializable]
        public class EnumTemplateValue
        {
            public int value;
            public Dictionary<string, string> descriptions = new Dictionary<string, string>();
        }

        [Serializable]
        public class StructTemplate
        {
            public string? parent;
            public StructTemplateField[] fields;
        }

        [Serializable]
        public class StructTemplateField
        {
            public string name;
            public string type;
            public string? subtype;
            public int? array_size;
            public int? alignment;
            public object? min_range;
            public object? max_range;
            public object? step;
            public Dictionary<string, string> descriptions = new Dictionary<string, string>();
        }

        [Serializable]
        public class ObjectTemplate
        {
            [JsonProperty("struct")]
            public string? structs;
            public string? category;
        }
    }
    #endregion

    int GetAlignment(StructTemplateField field, int fileVersion)
    {
        string subtype = "";
        string type = field.type;
        if (field.array_size != null)
        {
            subtype = type;
            type = "array";
        }
        if (field.alignment == null)
        {
            int align = 1;
            switch (type)
            {
                case "bool" or "uint8" or "int8" or "flags":
                    align = 1;
                    break;

                case "int16" or "uint16":
                    align = 2;
                    break;

                case "int32" or "uint32" or "float32" or "vector2" or "color8" or "colorf":
                    align = 4;
                    break;

                case "string" or "uint64" or "int64" or "float64":
                    align = 8;
                    break;

                case "object_reference":
                    if (fileVersion == 2)
                        align = 4;
                    else if (fileVersion == 3)
                        align = 8;
                    break;

                case "array":
                    if (field.array_size == null)
                        align = 8;
                    else
                        align = GetAlignment(new() { alignment = null, type = subtype }, fileVersion);
                    break;

                case "vector3" or "vector4" or "matrix34" or "matrix44":
                    align = 16;
                    break;

                default:
                    if (field.type.Contains("::"))
                    {
                        if (TemplateData.enums.ContainsKey(field.type))
                        {
                            switch (TemplateData.enums[field.type].type)
                            {
                                case "uint8" or "int8":
                                    align = 1;
                                    break;

                                case "uint16" or "int16":
                                    align = 2;
                                    break;

                                case "int32" or "uint32":
                                    align = 4;
                                    break;

                                case "int64" or "uint64":
                                    align = 8;
                                    break;
                            }
                        }
                    }
                    else if (TemplateData.structs.ContainsKey(field.type))
                    {
                        int largestAlignStr = 0;
                        foreach (var i in TemplateData.structs[field.type].fields)
                        {
                            align = GetAlignment(i, fileVersion);
                            if (align > largestAlignStr)
                                largestAlignStr = align;
                        }
                        return largestAlignStr;
                    }
                    break;
            }
            return align;
        }
        else
            return (int)field.alignment;
    }

    #region ReadingParameters
    object ReadField(BINAReader reader, string type, StructTemplateField field, Dictionary<string, object> parent)
    {
        object value = null;
        reader.Align(GetAlignment(field, reader.FileVersion));
        bool isStruct = false;
        string subtype = "";
        if (field.array_size != null)
        {
            subtype = type;
            type = "array";
        }
        switch (type)
        {
            case "bool":
                value = reader.Read<byte>() == 1;
                break;

            case "float32":
                value = reader.Read<float>();
                break;

            case "float64":
                value = reader.Read<double>();
                break;

            case "uint8" or "int8" or "flags":
                value = reader.Read<byte>();
                break;

            case "uint16":
                value = reader.Read<ushort>();
                break;

            case "int16":
                value = reader.Read<short>();
                break;

            case "uint32":
                value = reader.Read<uint>();
                break;

            case "int32":
                value = reader.Read<int>();
                break;

            case "uint64":
                value = reader.Read<ulong>();
                break;

            case "int64":
                value = reader.Read<long>();
                break;

            case "string":
                value = reader.ReadStringTableEntry();
                reader.Skip(8);
                break;


            case "array":
                object[] arrayValue = new object[0];
                if (field.array_size == null)
                {
                    long arrayPtr = 0;
                    long arrayLength = 0;
                    if (field.array_size == null)
                    {
                        arrayPtr = reader.Read<long>();
                        arrayLength = reader.Read<long>();
                        long arrayLength2 = reader.Read<long>();
                        reader.Skip(8);
                        if (arrayLength > 0 && arrayPtr > 0)
                        {
                            arrayValue = new object[arrayLength];
                            reader.ReadAtOffset(arrayPtr + 64, () =>
                            {
                                for (int i = 0; i < arrayLength; i++)
                                    arrayValue[i] = ReadField(reader, field.subtype, field, parent);
                            });
                        }
                    }
                    else
                    {
                        arrayLength = (int)field.array_size;
                        arrayValue = new object[arrayLength];
                        for (int i = 0; i < arrayLength; i++)
                            arrayValue[i] = ReadField(reader, field.subtype, field, parent);
                    }
                }
                else
                {
                    arrayValue = new object[(int)field.array_size];
                    for (int i = 0; i < (int)field.array_size; i++)
                        arrayValue[i] = ReadField(reader, subtype, new() { name = field.name, type = subtype }, parent);
                }
                value = arrayValue;
                break;

            case "object_reference":
                value = reader.Read<Guid>();
                break;

            case "vector2":
                value = reader.Read<Vector2>();
                break;

            case "vector3":
                value = reader.Read<Vector3>();
                break;

            case "vector4":
                value = reader.Read<Vector4>();
                break;

            case "matrix34":
                //value = reader.Read<Matrix3x4>();
                break;

            case "matrix44":
                value = reader.Read<Matrix4x4>();
                break;

            case "color8":
                value = reader.Read<Color8>();
                break;

            case "colorf":
                value = reader.Read<ColorF>();
                break;

            default:
                if (type.Contains("::"))
                {
                    if (TemplateData.enums.ContainsKey(type))
                    {
                        EnumValue eValue = new();
                        eValue.Values = new();
                        switch (TemplateData.enums[type].type)
                        {
                            case "uint8" or "int8":
                                eValue.Selected = reader.Read<byte>();
                                
                                break;

                            case "uint16":
                                eValue.Selected = reader.Read<ushort>();
                                break;

                            case "int16":
                                eValue.Selected = reader.Read<short>();
                                break;

                            case "uint32":
                                eValue.Selected = (int)reader.Read<uint>();
                                break;

                            case "int32":
                                eValue.Selected = reader.Read<int>();
                                break;

                            case "uint64":
                                eValue.Selected = (int)reader.Read<ulong>();
                                break;

                            case "int64":
                                eValue.Selected = (int)reader.Read<long>();
                                break;
                        }
                        foreach (var x in TemplateData.enums[type].values)
                            if (!eValue.Values.ContainsKey(x.Value.value))
                                eValue.Values.Add(x.Value.value, x.Key);
                        value = eValue;
                    }
                }
                else if (TemplateData.structs.ContainsKey(type))
                {
                    isStruct = true;
                    value = ReadStruct(reader, TemplateData.structs[type]);
                }
                break;
        }
        reader.Align(GetAlignment(field, reader.FileVersion));
        return value;
    }

    Dictionary<string, object> ReadStruct(BINAReader reader, StructTemplate str)
    {
        Dictionary<string, object> parameters = new();
        if (str.parent != null)
            parameters.Add(str.parent, ReadStruct(reader, TemplateData.structs[str.parent]));
        if (str.fields != null)
            foreach (var i in str.fields)
                parameters.Add(i.name, ReadField(reader, i.type, i, parameters));
        return parameters;
    }
    #endregion

    #region WritingParameters
    void WriteField(BINAWriter writer, StructTemplateField field, object value)
    {
        writer.Align(GetAlignment(field, writer.FileVersion));
        bool isStruct = false;
        switch (field.type)
        {
            case "bool":
                writer.Write(Convert.ToBoolean(value) ? (byte)1 : (byte)0);
                break;

            case "float32":
                writer.Write(Convert.ToSingle(value));
                break;

            case "uint8" or "int8":
                writer.Write(Convert.ToByte(value));
                break;

            case "uint16":
                writer.Write(Convert.ToUInt16(value));
                break;

            case "int16":
                writer.Write(Convert.ToInt16(value));
                break;

            case "uint32":
                writer.Write(Convert.ToUInt32(value));
                break;

            case "int32":
                writer.Write(Convert.ToInt32(value));
                break;

            case "uint64":
                writer.Write(Convert.ToUInt64(value));
                break;

            case "int64":
                writer.Write(Convert.ToInt64(value));
                break;

            case "string":
                writer.WriteStringTableEntry((string)value);
                writer.WriteNulls(8);
                break;

            case "array":
                if (field.array_size == null)
                {
                    Random rnd = new();
                    long r = rnd.NextInt64();
                    writer.AddOffset(Id.ToString() + field.name + "." + field.subtype + r.ToString());
                    writer.Write((long)((object[])value).Length);
                    writer.Write((long)((object[])value).Length);
                    writer.WriteNulls(8);
                    paramArrays.Add(new(field.name + "." + field.subtype, r), (object[])value);
                }
                else
                {
                    for (int i = 0; i < (int)field.array_size; i++)
                        WriteField(writer, new() { name = field.name, type = field.subtype }, ((object[])value)[i]);
                }
                break;

            case "object_reference":
                if (writer.FileVersion == 2)
                    writer.Write((Guid)value);
                else if (writer.FileVersion == 3)
                    writer.WriteArray(new byte[4] { ((Guid)value).ToByteArray()[0], ((Guid)value).ToByteArray()[1], ((Guid)value).ToByteArray()[2], ((Guid)value).ToByteArray()[3] });
                break;

            case "vector2":
                writer.Write((Vector2)value);
                break;

            case "vector3":
                writer.Write((Vector3)value);
                break;

            case "vector4":
                writer.Write((Vector4)value);
                break;

            default:
                if (field.type.Contains("::"))
                {
                    switch (TemplateData.enums[field.type].type)
                    {
                        case "uint8" or "int8":
                            writer.Write((byte)EnumValue.GetEnumValueFromSelectedString((string)value, TemplateData.enums[field.type]).Selected);
                            break;

                        case "uint":
                            writer.Write((ushort)EnumValue.GetEnumValueFromSelectedString((string)value, TemplateData.enums[field.type]).Selected);
                            break;

                        case "int16":
                            writer.Write((short)EnumValue.GetEnumValueFromSelectedString((string)value, TemplateData.enums[field.type]).Selected);
                            break;

                        case "uint32":
                            writer.Write((uint)EnumValue.GetEnumValueFromSelectedString((string)value, TemplateData.enums[field.type]).Selected);
                            break;

                        case "int32":
                            writer.Write(EnumValue.GetEnumValueFromSelectedString((string)value, TemplateData.enums[field.type]).Selected);
                            break;
                    }
                }
                else
                {
                    isStruct = true;
                    WriteStruct(writer, (Dictionary<string, object>)value, field.type);
                }
                break;
        }
        if (field.alignment != null && !isStruct)
            writer.Align((int)field.alignment);
        else if (isStruct)
            writer.Align(GetAlignment(field, writer.FileVersion));
    }

    void WriteStruct(BINAWriter writer, Dictionary<string, object> str, string strName)
    {
        if (TemplateData.tags == null || TemplateData.tags != null && !TemplateData.tags.ContainsKey(strName))
        {
            if (TemplateData.structs[strName].parent != null)
                WriteStruct(writer, (Dictionary<string, object>)str[TemplateData.structs[strName].parent], TemplateData.structs[strName].parent);
            int start = str.Keys.ToList().IndexOf(TemplateData.structs[strName].fields[0].name);
            for (int i = start; i < str.Count; i++)
                WriteField(writer, TemplateData.structs[strName].fields[i - start], str.ElementAt(i).Value);
        }
        else if (TemplateData.tags != null)
        {
            if (TemplateData.tags[strName].parent != null)
                WriteStruct(writer, (Dictionary<string, object>)str[TemplateData.tags[strName].parent], TemplateData.tags[strName].parent);
            int start = str.Keys.ToList().IndexOf(TemplateData.tags[strName].fields[0].name);
            for (int i = start; i < str.Count; i++)
                WriteField(writer, TemplateData.tags[strName].fields[i - start], str.ElementAt(i).Value);
        }
    }
    #endregion

    public struct EnumValue
    {
        public int Selected;
        public Dictionary<int, string> Values;

        public static EnumValue GetEnumValueFromSelectedString(string selected, Template.EnumTemplate templ)
        {
            EnumValue enumValue = new();
            enumValue.Values = new();
            int x = 0;
            foreach (var i in templ.values)
            {
                enumValue.Values.Add(i.Value.value, i.Key);
                if (enumValue.Selected == null)
                    if (i.Key == selected)
                        enumValue.Selected = x;
                x++;
            }
            return enumValue;
        }
    }

    public struct BitFlag
    {
        public Dictionary<string, bool> Flags;
    }
}

public class Reflection : IFile
{
    public const string FileExtension = ".rfl";

    public static string TemplateFilePath = "frontiers.rfl.json";
    public string RFLName = "PhotoModeParameters";

    public ReflectionData Parameters;

    public int FileVersion = 1;

    public Reflection() { }

    public Reflection(string filename) => Open(filename);
    public Reflection(string filename, string templateFilePath = "frontiers.rfl.json", string rflName = "PhotoModeParameters") { TemplateFilePath = templateFilePath; Parameters = new(TemplateFilePath); RFLName = rflName; Open(filename); }

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.FileVersion = FileVersion;

        reader.ReadHeader();

        Parameters.StructName = RFLName;
        Parameters.Read(reader);

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.FileVersion = FileVersion;

        writer.WriteHeader();

        Parameters.StructName = RFLName;
        Parameters.Write(writer);

        writer.FinishWrite();
        writer.Dispose();
    }
}
