using AshDumpLib.Helpers.Archives;
using Newtonsoft.Json;
using System.Numerics;
using Amicitia.IO.Binary;

using static AshDumpLib.HedgehogEngine.MathA;

namespace AshDumpLib.HedgehogEngine.BINA;

public class Reflection : IFile
{
    public const string FileExtension = ".rfl";

    public Dictionary<string, object> Parameters = new();


    public string TemplateFilePath = "frontiers.rfl.json";
    public string RFLName = "PhotoModeParameters";
    Template.TemplateJSON template;
    Template.StructTemplate structTemplate;

    public int FileVersion = 1;

    public Reflection() { }

    public Reflection(string filename) => Open(filename);
    public Reflection(string filename, string templateFilePath = "frontiers.rfl.json", string rflName = "PhotoModeParameters") { TemplateFilePath = templateFilePath; RFLName = rflName; Open(filename); }

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        template = JsonConvert.DeserializeObject<Template.TemplateJSON>(File.ReadAllText(TemplateFilePath));
        reader.FileVersion = FileVersion;

        reader.ReadHeader();

        structTemplate = template.structs[RFLName];
        Parameters.Add(RFLName, ReadStruct(reader, structTemplate, RFLName));

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        template = JsonConvert.DeserializeObject<Template.TemplateJSON>(File.ReadAllText(TemplateFilePath));
        writer.FileVersion = FileVersion;

        writer.WriteHeader();



        writer.FinishWrite();
        writer.Dispose();
    }

    int GetAlignment(Template.StructTemplateField field, int fileVersion)
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
                        throw new NotImplementedException();
                    else
                        align = GetAlignment(new() { alignment = null, type = subtype }, fileVersion);
                    break;

                case "vector3" or "vector4" or "matrix34" or "matrix44":
                    align = 16;
                    break;

                default:
                    if (field.type.Contains("::"))
                    {
                        if (template.enums.ContainsKey(field.type))
                        {
                            switch (template.enums[field.type].type)
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
                    else if (template.structs.ContainsKey(field.type))
                    {
                        int largestAlignStr = 0;
                        foreach (var i in template.structs[field.type].fields)
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
    object ReadField(BINAReader reader, string type, Template.StructTemplateField field, Dictionary<string, object> parent)
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
                object[] arrayValue;
                if (field.array_size == null)
                {
                    throw new NotImplementedException();
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
                    if(template.enums.ContainsKey(type))
                    {
                        EnumValue eValue = new();
                        switch (template.enums[type].type)
                        {
                            case "uint8" or "int8":
                                eValue.Values = new();
                                eValue.Selected = reader.Read<byte>();
                                foreach (var x in template.enums[type].values)
                                {
                                    if (!eValue.Values.ContainsKey(x.Value.value))
                                        eValue.Values.Add(x.Value.value, x.Key);
                                }
                                value = eValue;
                                break;

                            case "uint16":
                                eValue.Values = new();
                                eValue.Selected = reader.Read<ushort>();
                                foreach (var x in template.enums[type].values)
                                {
                                    if (!eValue.Values.ContainsKey(x.Value.value))
                                        eValue.Values.Add(x.Value.value, x.Key);
                                }
                                value = eValue;
                                break;

                            case "int16":
                                eValue.Values = new();
                                eValue.Selected = reader.Read<short>();
                                foreach (var x in template.enums[type].values)
                                {
                                    if (!eValue.Values.ContainsKey(x.Value.value))
                                        eValue.Values.Add(x.Value.value, x.Key);
                                }
                                value = eValue;
                                break;

                            case "uint32":
                                eValue.Values = new();
                                eValue.Selected = (int)reader.Read<uint>();
                                foreach (var x in template.enums[type].values)
                                {
                                    if (!eValue.Values.ContainsKey(x.Value.value))
                                        eValue.Values.Add(x.Value.value, x.Key);
                                }
                                value = eValue;
                                break;

                            case "int32":
                                eValue.Values = new();
                                eValue.Selected = reader.Read<int>();
                                foreach (var x in template.enums[type].values)
                                {
                                    if (!eValue.Values.ContainsKey(x.Value.value))
                                        eValue.Values.Add(x.Value.value, x.Key);
                                }
                                value = eValue;
                                break;

                            case "uint64":
                                eValue.Values = new();
                                eValue.Selected = (int)reader.Read<ulong>();
                                foreach (var x in template.enums[type].values)
                                {
                                    if (!eValue.Values.ContainsKey(x.Value.value))
                                        eValue.Values.Add(x.Value.value, x.Key);
                                }
                                value = eValue;
                                break;

                            case "int64":
                                eValue.Values = new();
                                eValue.Selected = (int)reader.Read<long>();
                                foreach (var x in template.enums[type].values)
                                {
                                    if (!eValue.Values.ContainsKey(x.Value.value))
                                        eValue.Values.Add(x.Value.value, x.Key);
                                }
                                value = eValue;
                                break;
                        }
                    }
                }
                else if (template.structs.ContainsKey(type))
                {
                    isStruct = true;
                    value = ReadStruct(reader, template.structs[type], type);
                }
                break;
        }
        reader.Align(GetAlignment(field, reader.FileVersion));
        return value;
    }

    Dictionary<string, object> ReadStruct(BINAReader reader, Template.StructTemplate str, string structName)
    {
        Dictionary<string, object> parameters = new();
        if (str.parent != null)
            parameters.Add(str.parent, ReadStruct(reader, template.structs[str.parent], str.parent));
        if (str.fields != null)
        {
            foreach (var i in str.fields)
                parameters.Add(i.name, ReadField(reader, i.type, i, parameters));
        }
        return parameters;
    }
    #endregion

    #region WritingParameters
    void WriteField(BINAWriter writer, Template.StructTemplateField field, object value)
    {
        writer.Align(GetAlignment(field, writer.FileVersion));
        bool isStruct = false;
        switch (field.type)
        {
            case "bool":
                writer.Write(((bool)value) ? (byte)1 : (byte)0);
                break;

            case "float32":
                writer.Write((float)value);
                break;

            case "uint8" or "int8":
                writer.Write((byte)value);
                break;

            case "uint16":
                writer.Write((ushort)value);
                break;

            case "int16":
                writer.Write((short)value);
                break;

            case "uint32":
                writer.Write((uint)value);
                break;

            case "int32":
                writer.Write((int)value);
                break;

            case "uint64":
                writer.Write((ulong)value);
                break;

            case "int64":
                writer.Write((long)value);
                break;

            case "string":
                writer.WriteStringTableEntry((string)value);
                writer.WriteNulls(8);
                break;

            case "array":
                if (field.array_size == null)
                    throw new NotImplementedException();
                else
                    for (int i = 0; i < (int)field.array_size; i++)
                        WriteField(writer, new() { name = field.name, type = field.subtype }, ((object[])value)[i]);
                break;

            case "object_reference":
                writer.Write((Guid)value);
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
                    if (template.enums.ContainsKey(field.type))
                    {
                        switch (template.enums[field.type].type)
                        {
                            case "uint8" or "int8":
                                writer.Write((byte)((EnumValue)value).Selected);
                                break;

                            case "uint":
                                writer.Write((ushort)((EnumValue)value).Selected);
                                break;

                            case "int16":
                                writer.Write((short)((EnumValue)value).Selected);
                                break;

                            case "uint32":
                                writer.Write((uint)((EnumValue)value).Selected);
                                break;

                            case "int32":
                                writer.Write((int)((EnumValue)value).Selected);
                                break;
                        }
                    }
                    else if (template.structs.ContainsKey(field.type))
                    {
                        isStruct = true;
                        WriteStruct(writer, (Dictionary<string, object>)value, field.type);
                    }
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
        if (template.structs[strName].parent != null)
            WriteStruct(writer, (Dictionary<string, object>)str[template.structs[strName].parent], template.structs[strName].parent);
        int start = str.Keys.ToList().IndexOf(template.structs[strName].fields[0].name);
        for (int i = start; i < str.Count; i++)
            WriteField(writer, template.structs[strName].fields[i - start], str.ElementAt(i).Value);
    }
    #endregion

    public struct EnumValue
    {
        public int Selected;
        public Dictionary<int, string> Values;
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

        [Serializable]
        public class TemplateJSON
        {
            public int version;
            public string format;
            public Dictionary<string, EnumTemplate> enums = new Dictionary<string, EnumTemplate>();
            public Dictionary<string, StructTemplate> structs = new Dictionary<string, StructTemplate>();
            public Dictionary<string, ObjectTemplate> objects = new Dictionary<string, ObjectTemplate>();
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
}
