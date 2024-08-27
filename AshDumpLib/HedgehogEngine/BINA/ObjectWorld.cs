using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Numerics;
using Newtonsoft.Json;
using libHSON;
using System.Drawing;
using System.Reflection.PortableExecutable;
using System.Diagnostics;
using System.Linq;

namespace AshDumpLib.HedgehogEngine.BINA;

public class ObjectWorld : IFile
{
    public const string FileExtension = ".gedit";

    public List<Object> Objects = new();

    //Using JSON templates from HedgeLib++
    public string TemplateFilePath = "frontiers.json";
    Template.TemplateJSON template;

    public int FileVersion = 3;

    public ObjectWorld() { }

    public ObjectWorld(string filename) => Open(filename);
    public ObjectWorld(string filename, string templateFilePath = "frontiers.json") { TemplateFilePath = templateFilePath; Open(filename); }

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        template = JsonConvert.DeserializeObject<Template.TemplateJSON>(File.ReadAllText(TemplateFilePath));
        FileVersion = Template.GetVersionFromTemplate(template.format);
        reader.FileVersion = FileVersion;

        reader.ReadHeader();
        reader.Skip(16);
        long dataPtr = reader.Read<long>();
        long objectCount = reader.Read<long>();
        long objectCount1 = reader.Read<long>();
        reader.Jump(dataPtr, SeekOrigin.Begin);
        for(int i = 0; i < objectCount; i++)
        {
            Object obj = new();
            obj.template = template;
            obj.Read(reader);
            Objects.Add(obj);
        }
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        template = JsonConvert.DeserializeObject<Template.TemplateJSON>(File.ReadAllText(TemplateFilePath));
        FileVersion = Template.GetVersionFromTemplate(template.format);
        writer.FileVersion = FileVersion;

        writer.WriteHeader();
        writer.WriteNulls(16);
        writer.AddOffset("dataPtr");
        writer.Write((long)Objects.Count);
        writer.Write((long)Objects.Count);
        writer.WriteNulls(8);
        writer.SetOffset("dataPtr");
        foreach(var i in Objects)
            writer.AddOffset(i.ObjectName + i.ID.ToString());
        foreach (var i in Objects)
            i.Write(writer);
        foreach (var i in Objects)
            i.FinishWrite(writer);

        writer.FinishWrite();
        writer.Dispose();
    }


    public class Object : IBINASerializable
    {
        public Guid ID = Guid.Empty;
        public Guid ParentID = Guid.Empty;
        public string ObjectName = "";
        public string TypeName = "";
        public Vector3 Position = new(0, 0, 0);
        public Vector3 Rotation = new(0, 0, 0);
        public Vector3 OffsetPosition = new(0, 0, 0);
        public Vector3 OffsetRotation = new(0, 0, 0);
        public List<Tag> Tags = new();
        public Dictionary<string, object> Parameters = new();
        Dictionary<Tuple<string, long>, object[]> paramArrays = new();

        public Template.TemplateJSON template;

        int GetAlignment(Template.StructTemplateField field, int fileVersion)
        {
            if(field.alignment == null)
            {
                int align = 1;
                switch (field.type)
                {
                    case "bool" or "uint8" or "int8":
                        align = 1;
                        break;

                    case "int16" or "uint16":
                        align = 2;
                        break;

                    case "int32" or "uint32" or "float32":
                        align = 4;
                        break;

                    case "string" or "uint64" or "int64" or "float64" or "vector2":
                        align = 8;
                        break;

                    case "object_reference":
                        if (fileVersion == 2)
                            align = 4;
                        else if(fileVersion == 3)
                            align = 8;
                        break;

                    case "array":
                        if(field.array_size == null)
                            align = 8;
                        else
                            align = GetAlignment(new() { alignment = null, type = field.subtype }, fileVersion);
                        break;

                    case "vector3":
                        align = 12;
                        break;

                    case "vector4":
                        align = 16;
                        break;

                    default:
                        if (field.type.Contains("::"))
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
                        else
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

                case "uint8" or "int8":
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
                    long arrayPtr = 0;
                    long arrayLength = 0;
                    object[] arrayValue = new object[0];
                    if (field.array_size == null)
                    {
                        arrayPtr = reader.Read<long>();
                        arrayLength = reader.Read<long>();
                        long arrayLength2 = reader.Read<long>();
                        reader.Skip(8);
                        if(arrayLength > 0 && arrayPtr > 0)
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
                    value = arrayValue;
                    break;

                case "object_reference":
                    if(reader.FileVersion == 2)
                    {
                        byte[] rawValue = reader.ReadArray<byte>(4);
                        byte[] rawId = new byte[16];
                        rawValue.CopyTo(rawId, 0);
                        value = new Guid(rawId);
                    }
                    else if(reader.FileVersion == 3)
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

                default:
                    if (type.Contains("::"))
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
                    else
                    {
                        isStruct = true;
                        value = ReadStruct(reader, template.structs[type], type);
                    }  
                    break;
            }
            if (field.alignment != null && !isStruct)
                reader.Align((int)field.alignment);
            else if(isStruct)
                reader.Align(GetAlignment(field, reader.FileVersion));
            return value;
        }

        Dictionary<string, object> ReadStruct(BINAReader reader, Template.StructTemplate str, string structName)
        {
            Dictionary<string, object> parameters = new();
            if (str.parent != null)
                parameters.Add(str.parent, ReadStruct(reader, template.structs[str.parent], str.parent));
            if(str.fields != null)
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
                    {
                        Random rnd = new();
                        long r = rnd.NextInt64();
                        writer.AddOffset(ObjectName + ID.ToString() + field.name + "." + field.subtype + r.ToString());
                        writer.Write((long)((object[])value).Length);
                        writer.Write((long)((object[])value).Length);
                        writer.WriteNulls(8);
                        paramArrays.Add(new(field.name + "." + field.subtype, r), (object[])value);
                    }
                    else
                    {
                        for (int i = 0; i < (int)field.array_size; i++)
                            WriteField(writer, new() { name = field.name, type = field.subtype}, ((object[])value)[i]);
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
            if (template.structs[strName].parent != null)
                WriteStruct(writer, (Dictionary<string, object>)str[template.structs[strName].parent], template.structs[strName].parent);
            int start = str.Keys.ToList().IndexOf(template.structs[strName].fields[0].name);
            for(int i = start; i < str.Count; i++)
                WriteField(writer, template.structs[strName].fields[i-start], str.ElementAt(i).Value);
        }
        #endregion

        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
            reader.ReadAtOffset(ptr + 64, () => 
            {
                reader.Skip(8);
                TypeName = reader.ReadStringTableEntry();
                if (reader.FileVersion == 3)
                {
                    ObjectName = reader.ReadStringTableEntry();
                    reader.Skip(8);
                    ID = reader.Read<Guid>();
                    ParentID = reader.Read<Guid>();
                }
                else if(reader.FileVersion == 2)
                {
                    ObjectName = reader.ReadStringTableEntry();
                    byte[] id = new byte[16];
                    byte[] idRaw = reader.ReadArray<byte>(4);
                    idRaw.CopyTo(id, 0);
                    ID = new(id);
                    byte[] parentid = new byte[16];
                    byte[] parentidRaw = reader.ReadArray<byte>(4);
                    parentidRaw.CopyTo(parentid, 0);
                    ParentID = new(parentid);
                }
                Position = reader.Read<Vector3>();
                Rotation = reader.Read<Vector3>();
                OffsetPosition = reader.Read<Vector3>();
                OffsetRotation = reader.Read<Vector3>();
                long tagsPtr = reader.Read<long>();
                long tagsCount = reader.Read<long>();
                long tagsCount2 = reader.Read<long>();
                reader.Skip(8);
                reader.ReadAtOffset(tagsPtr + 64, () =>
                {
                    for(int i = 0; i < tagsCount; i++)
                    {
                        Tag tag = new();
                        tag.Read(reader);
                        Tags.Add(tag);
                    }
                });
                Template.StructTemplate objTmp = template.structs[template.objects[TypeName].structs];
                long paramPtr = reader.Read<long>();
                reader.ReadAtOffset(paramPtr + 64, () =>
                {
                    Parameters.Add(template.objects[TypeName].structs, ReadStruct(reader, objTmp, template.objects[TypeName].structs));
                });
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.SetOffset(ObjectName + ID.ToString());
            writer.WriteNulls(8);
            writer.WriteStringTableEntry(TypeName);
            if (writer.FileVersion == 3)
            {
                writer.WriteStringTableEntry(ObjectName);
                writer.WriteNulls(8);
                writer.Write(ID);
                writer.Write(ParentID);
            }
            else if(writer.FileVersion == 2)
            {
                writer.WriteStringTableEntry(ObjectName);
                writer.WriteArray(new byte[4] { ID.ToByteArray()[0], ID.ToByteArray()[1], ID.ToByteArray()[2], ID.ToByteArray()[3]});
                writer.WriteArray(new byte[4] { ParentID.ToByteArray()[0], ParentID.ToByteArray()[1], ParentID.ToByteArray()[2], ParentID.ToByteArray()[3]});
            }
            writer.Write(Position);
            writer.Write(Rotation);
            writer.Write(OffsetPosition);
            writer.Write(OffsetRotation);
            writer.AddOffset(ObjectName + ID.ToString() + "tags");
            writer.Write((long)Tags.Count);
            writer.Write((long)Tags.Count);
            writer.WriteNulls(8);
            writer.AddOffset(ObjectName + ID.ToString() + "params");
            writer.Align(16);
            writer.SetOffset(ObjectName + ID.ToString() + "tags");
            foreach (var i in Tags)
            {
                i.Owner = this;
                writer.AddOffset(ObjectName + ID.ToString() + i.Name);
            }
            foreach (var i in Tags)
                i.Write(writer);
        }

        public void FinishWrite(BINAWriter writer)
        {
            writer.SetOffset(ObjectName + ID.ToString() + "params");
            if(Parameters.Count > 0)
            {
                WriteStruct(writer, (Dictionary<string, object>)Parameters.ElementAt(0).Value, Parameters.ElementAt(0).Key);
                foreach (var i in paramArrays)
                {
                    writer.SetOffset(ObjectName + ID.ToString() + i.Key.Item1 + i.Key.Item2.ToString());
                    foreach (var x in i.Value)
                        WriteField(writer, new() { type = i.Key.Item1.Substring(i.Key.Item1.IndexOf('.') + 1) }, x);
                }
            }
            foreach(var i in Tags)
                i.FinishWrite(writer);
        }

        public class Tag : IBINASerializable
        {
            public string Name = "";
            public object Data;
            public Object Owner = new();

            public void Read(BINAReader reader)
            {
                long ptr = reader.Read<long>();
                reader.ReadAtOffset(ptr + 64, () =>
                {
                    reader.Skip(8);
                    Name = reader.ReadStringTableEntry();
                    long size = reader.Read<long>();
                    long dataPtr = reader.Read<long>();
                    switch (Name)
                    {
                        case "RangeSpawning":
                            Data = reader.ReadArrayAtOffset<float>(dataPtr + 64, 2);
                            break;

                        default:
                            Data = reader.ReadArrayAtOffset<byte>(dataPtr + 64, (int)size);
                            break;
                    }
                });
            }

            public void Write(BINAWriter writer)
            {
                writer.SetOffset(Owner.ObjectName + Owner.ID.ToString() + Name);
                writer.WriteNulls(8);
                writer.WriteStringTableEntry(Name);
                switch (Name)
                {
                    case "RangeSpawning":
                        writer.Write((long)8);
                        break;

                    default:
                        writer.Write((long)((byte[])Data).Length);
                        break;
                }
                writer.AddOffset(Owner.ObjectName + Owner.ID.ToString() + Name + "data");
                writer.Align(16);
            }

            public void FinishWrite(BINAWriter writer)
            {
                writer.SetOffset(Owner.ObjectName + Owner.ID.ToString() + Name + "data");
                switch (Name)
                {
                    case "RangeSpawning":
                        writer.WriteArray((float[])Data);
                        break;

                    default:
                        writer.WriteArray((byte[])Data);
                        break;
                }
            }
        }

        public struct EnumValue
        {
            public int Selected;
            public Dictionary<int, string> Values;
        }
    }

    #region TemplateReader
    public static class Template
    {
        public static int GetVersionFromTemplate(string version)
        {
            int ver = 3;
            switch(version)
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

    #region HSON

    #region ToHson
    public Project ToHson()
    {
        Project project = new();
        foreach (var i in Objects)
        {
            project.Objects.Add(CreateHsonObject(i));
        }
        return project;
    }


    libHSON.Object CreateHsonObject(Object i)
    {
        libHSON.Object? parentObj = null;
        if (i.ParentID != Guid.Empty)
        {
            Object parentObject = Objects.Find(x => x.ID == i.ParentID);
            parentObj = CreateHsonObject(parentObject);
        }

        libHSON.Object obj = new(i.ID, i.ObjectName, i.TypeName, position: i.Position, rotation: Helpers.ToQuaternion(i.Rotation), parent: parentObj);
        ParameterCollection tags = new();
        foreach (var x in i.Tags)
        {
            switch (x.Name)
            {
                case "RangeSpawning":
                    ParameterCollection paramColl = new(2)
                        {
                            { "rangeIn", new(((float[])x.Data)[0]) },
                            { "rangeOut", new(((float[])x.Data)[1]) }
                        };
                    tags.Add(x.Name, new(paramColl));
                    break;
            }

        }
        obj.LocalParameters.Add("tags", new(tags));

        CreateHsonParameters(i.Parameters, ref obj);
        return obj;
    }

    void CreateHsonParameters(Dictionary<string, object> parameter, ref libHSON.Object obj)
    {
        foreach (var x in parameter)
            obj.LocalParameters.Add(x.Key, CreateHsonParameter((Dictionary<string, object>)x.Value));
    }

    Parameter CreateHsonParameter(Dictionary<string, object> parameter)
    {
        ParameterCollection paramColl = new();
        foreach (var i in parameter)
        {
            Parameter param = new();
            Type type = i.Value.GetType();
            if (type == typeof(byte))
                param = new(((byte)i.Value));
            else if (type == typeof(bool))
                param = new(((bool)i.Value));
            else if (type == typeof(ushort))
                param = new(((ushort)i.Value));
            else if (type == typeof(short))
                param = new(((short)i.Value));
            else if (type == typeof(uint))
                param = new(((uint)i.Value));
            else if (type == typeof(int))
                param = new(((int)i.Value));
            else if (type == typeof(ulong))
                param = new(((ulong)i.Value));
            else if (type == typeof(long))
                param = new(((long)i.Value));
            else if (type == typeof(float))
                param = new(((float)i.Value));
            else if (type == typeof(double))
                param = new(((double)i.Value));
            else if (type == typeof(Object.EnumValue))
                param = new(((Object.EnumValue)i.Value).Values[((Object.EnumValue)i.Value).Selected]);
            else if (type == typeof(string))
                param = new((string)i.Value);
            else if (type == typeof(Dictionary<string, object>))
                param = CreateHsonParameter((Dictionary<string, object>)i.Value);
            else if (type == typeof(Guid))
                param = new(((Guid)i.Value).ToString());
            else if (type == typeof(object[]))
            {
                List<Parameter> paramArray = new();
                foreach (var x in (object[])i.Value)
                {
                    Parameter paramItem = new();
                    Type typee = x.GetType();
                    if (typee == typeof(byte))
                        paramItem = new(((byte)x));
                    else if (typee == typeof(bool))
                        paramItem = new(((bool)x));
                    else if (typee == typeof(ushort))
                        paramItem = new(((ushort)x));
                    else if (typee == typeof(short))
                        paramItem = new(((short)x));
                    else if (typee == typeof(uint))
                        paramItem = new(((uint)x));
                    else if (typee == typeof(int))
                        paramItem = new(((int)x));
                    else if (typee == typeof(ulong))
                        paramItem = new(((ulong)x));
                    else if (typee == typeof(long))
                        paramItem = new(((long)x));
                    else if (typee == typeof(float))
                        paramItem = new(((float)x));
                    else if (typee == typeof(double))
                        paramItem = new(((double)x));
                    else if (typee == typeof(Object.EnumValue))
                        paramItem = new(((Object.EnumValue)x).Values[((Object.EnumValue)x).Selected]);
                    else if (typee == typeof(string))
                        paramItem = new((string)x);
                    else if (typee == typeof(Dictionary<string, object>))
                        paramItem = CreateHsonParameter((Dictionary<string, object>)x);
                    else if (typee == typeof(Guid))
                        paramItem = new(((Guid)x).ToString());
                    paramArray.Add(paramItem);
                }
                param = new(paramArray);
            }
            paramColl.Add(i.Key, param);
        }
        return new(paramColl);
    }
    #endregion

    #region FromHson
    public static ObjectWorld ToGedit(Project project, string templateFilePath) 
    {
        Template.TemplateJSON template = JsonConvert.DeserializeObject<Template.TemplateJSON>(File.ReadAllText(templateFilePath));
        ObjectWorld gedit = new();
        gedit.TemplateFilePath = templateFilePath;
        gedit.template = template;
        foreach (var i in project.Objects)
            gedit.Objects.Add(CreateGeditObject(i, template));
        return gedit;
    }

    static Object CreateGeditObject(libHSON.Object i, Template.TemplateJSON template)
    {
        Object obj = new();
        obj.template = template;
        obj.ObjectName = i.Name;
        obj.TypeName = i.Type;
        obj.Position = i.LocalPosition;
        obj.Rotation = Helpers.ToEulerAngles(i.LocalRotation);
        obj.OffsetPosition = i.LocalPosition;
        obj.OffsetRotation = Helpers.ToEulerAngles(i.LocalRotation);
        foreach (var x in i.LocalParameters["tags"].ValueObject)
        {
            Object.Tag tag = new() { Name = x.Key };
            switch (x.Key)
            {
                case "RangeSpawning":
                    tag.Data = new float[2] { (float)x.Value.ValueObject["rangeIn"].ValueFloatingPoint, (float)x.Value.ValueObject["rangeOut"].ValueFloatingPoint };
                    break;
            }
            obj.Tags.Add(tag);
        }
        obj.Parameters = CreateGeditParameter(new(i.LocalParameters.ElementAt(1).Key, i.LocalParameters.ElementAt(1).Value), template);
        return obj;
    }

    static Dictionary<string, object> CreateGeditParameter(Tuple<string, Parameter> param, Template.TemplateJSON template)
    {
        Dictionary<string, object> prm = new()
        {
            { param.Item1, CreateGeditParameterObject(new(param.Item1, param.Item2), template, param.Item1) }
        };
        return prm;
    }

    static object CreateGeditParameterObject(Tuple<string, Parameter> param, Template.TemplateJSON template, string strName)
    {
        object value = null;
        switch (param.Item2.Type)
        {
            case ParameterType.Boolean:
                value = param.Item2.ValueBoolean;
                break;

            case ParameterType.SignedInteger:
                value = param.Item2.ValueSignedInteger;
                break;

            case ParameterType.UnsignedInteger: 
                value = param.Item2.ValueUnsignedInteger; 
                break;

            case ParameterType.FloatingPoint:
                value = param.Item2.ValueFloatingPoint; 
                break;

            case ParameterType.String:
                if (template.structs.ContainsKey(strName))
                {
                    foreach(var i in template.structs[strName].fields)
                    {
                        if (i.name == param.Item1 && template.enums.ContainsKey(i.type))
                        {
                            Object.EnumValue eVal = new();
                            eVal.Values = new();
                            foreach (var x in template.enums[i.type].values)
                            {
                                eVal.Values.Add(x.Value.value, x.Key);
                            }
                            eVal.Selected = template.enums[i.type].values[param.Item2.ValueString].value;
                        }
                    }
                }
                else
                    value = param.Item2.ValueString; 
                break;

            case ParameterType.Array:
                value = new object[param.Item2.ValueArray.Count];
                for(int i = 0; i < param.Item2.ValueArray.Count; i++)
                    ((object[])value)[i] = CreateGeditParameterObject(new(param.Item1, param.Item2.ValueArray[i]), template, strName);
                break;

            case ParameterType.Object:
                Dictionary<string, object> str = new();
                foreach (var i in param.Item2.ValueObject)
                    str.Add(i.Key, CreateGeditParameterObject(new(i.Key, i.Value), template, i.Key));
                value = str;
                break;
        }
        return value;
    }
    #endregion

    #endregion
}