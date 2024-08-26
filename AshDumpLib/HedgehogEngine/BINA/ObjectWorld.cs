using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Numerics;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;

namespace AshDumpLib.HedgehogEngine.BINA;

public class ObjectWorld : IFile
{
    public const string FileExtension = ".gedit";

    public List<Object> Objects = new();

    //Using JSON templates from HedgeLib++
    public string TemplateFilePath = "frontiers.json";
    Template.TemplateJSON template;

    public ObjectWorld() { }

    public ObjectWorld(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        template = JsonConvert.DeserializeObject<Template.TemplateJSON>(File.ReadAllText(TemplateFilePath));
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
        public Dictionary<string, object[]> paramArrays = new();

        public Template.TemplateJSON template;

        int GetAlignment(Template.StructTemplateField field)
        {
            if(field.alignment == null)
            {
                int align = 1;
                switch (field.type)
                {
                    case "bool":
                        align = 1;
                        break;

                    case "float32":
                        align = 4;
                        break;

                    case "uint8" or "int8":
                        align = 1;
                        break;

                    case "uint32":
                        align = 4;
                        break;

                    case "int32":
                        align = 4;
                        break;

                    case "string":
                        align = 8;
                        break;

                    case "array":
                        if(field.array_size == null)
                            align = 8;
                        else
                            align = GetAlignment(new() { alignment = null, type = field.subtype });
                        break;

                    case "object_reference":
                        align = 8;
                        break;

                    case "vector2":
                        align = 8;
                        break;

                    case "vector3":
                        align = 12;
                        break;

                    default:
                        if (field.type.Contains("::"))
                        {
                            switch (template.enums[field.type].type)
                            {
                                case "uint8" or "int8":
                                    align = 1;
                                    break;

                                case "uint32":
                                    align = 4;
                                    break;

                                case "int32":
                                    align = 4;
                                    break;
                            }
                        }
                        else
                        {
                            int largestAlignStr = 0;
                            foreach (var i in template.structs[field.type].fields)
                            {
                                align = GetAlignment(i);
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

        object ReadField(BINAReader reader, string type, Template.StructTemplateField field, Dictionary<string, object> parent)
        {
            object value = null;
            reader.Align(GetAlignment(field));
            bool isStruct = false;
            switch (type)
            {
                case "bool":
                    value = reader.Read<byte>() == 1;
                    break;

                case "float32":
                    value = reader.Read<float>();
                    break;

                case "uint8" or "int8":
                    value = reader.Read<byte>();
                    break;

                case "uint32":
                    value = reader.Read<uint>();
                    break;

                case "int32":
                    value = reader.Read<int>();
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
                        if(arrayPtr == 0)
                            arrayPtr = reader.Read<long>();
                        arrayLength = reader.Read<long>();
                        long arrayLength2 = reader.Read<long>();
                        reader.Skip(8);
                        if(arrayLength > 0)
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
                    value = reader.Read<Guid>();
                    break;

                case "vector2":
                    value = reader.Read<Vector2>();
                    break;

                case "vector3":
                    value = reader.Read<Vector3>();
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
                reader.Align(GetAlignment(field));
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

        void LoopParentsRead(BINAReader reader, Template.StructTemplate str, string structName)
        {
            Parameters.Add(structName, ReadStruct(reader, str, structName));
        }

        void WriteField(BINAWriter writer, Template.StructTemplateField field, object value)
        {
            writer.Align(GetAlignment(field));
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

                case "uint32":
                    writer.Write((uint)value);
                    break;

                case "int32":
                    writer.Write((int)value);
                    break;

                case "string":
                    writer.WriteStringTableEntry((string)value);
                    writer.WriteNulls(8);
                    break;

                case "array":
                    if (field.array_size == null)
                    {
                        writer.AddOffset(ObjectName + ID.ToString() + field.name + "." + field.subtype);
                        writer.Write((long)((object[])value).Length);
                        writer.Write((long)((object[])value).Length);
                        writer.WriteNulls(8);
                        paramArrays.Add(field.name + "." + field.subtype, (object[])value);
                    }
                    else
                    {
                        for (int i = 0; i < (int)field.array_size; i++)
                            WriteField(writer, new() { name = field.name, type = field.subtype}, ((object[])value)[i]);
                    }
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

                default:
                    if (field.type.Contains("::"))
                    {
                        switch (template.enums[field.type].type)
                        {
                            case "uint8" or "int8":
                                writer.Write((byte)((EnumValue)value).Selected);
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
                writer.Align(GetAlignment(field));
        }

        void WriteStruct(BINAWriter writer, Dictionary<string, object> str, string strName)
        {
            for(int i = 0; i < str.Count; i++)
                WriteField(writer, template.structs[strName].fields[i], str.ElementAt(i).Value);
        }

        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
            reader.ReadAtOffset(ptr + 64, () => 
            {
                reader.Skip(8);
                TypeName = reader.ReadStringTableEntry();
                ObjectName = reader.ReadStringTableEntry();
                reader.Align(16);
                ID = reader.Read<Guid>();
                ParentID = reader.Read<Guid>();
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
                    LoopParentsRead(reader, objTmp, template.objects[TypeName].structs);
                });
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.SetOffset(ObjectName + ID.ToString());
            writer.WriteNulls(8);
            writer.WriteStringTableEntry(TypeName);
            writer.WriteStringTableEntry(ObjectName);
            writer.Write(ID);
            writer.Write(ParentID);
            writer.Write(Position);
            writer.Write(Rotation);
            writer.Write(OffsetPosition);
            writer.Write(OffsetRotation);
            writer.AddOffset(ObjectName + ID.ToString() + "tags");
            writer.Write((long)Tags.Count);
            writer.Write((long)Tags.Count);
            writer.WriteNulls(8);
            writer.AddOffset(ObjectName + ID.ToString() + "params");
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
            WriteStruct(writer, (Dictionary<string, object>)Parameters.ElementAt(0).Value, Parameters.ElementAt(0).Key);
            foreach (var i in paramArrays)
            {
                writer.SetOffset(ObjectName + ID.ToString() + i.Key);
                foreach (var x in i.Value)
                    WriteField(writer, new() { type = i.Key.Substring(i.Key.IndexOf('.') + 1) }, x);
            }
        }

        public class Tag : IBINASerializable
        {
            public string Name = "";
            public byte[] Data = new byte[0];
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
                    Data = reader.ReadArrayAtOffset<byte>(dataPtr + 64, (int)size);
                });
            }

            public void Write(BINAWriter writer)
            {
                writer.SetOffset(Owner.ObjectName + Owner.ID.ToString() + Name);
                writer.WriteNulls(8);
                writer.WriteStringTableEntry(Name);
                writer.Write((long)Data.Length);
                writer.AddOffset(Owner.ObjectName + Owner.ID.ToString() + Name + "data");
                writer.Align(16);
                writer.SetOffset(Owner.ObjectName + Owner.ID.ToString() + Name + "data");
                writer.WriteArray(Data);
            }

            public void FinishWrite(BINAWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct EnumValue
        {
            public int Selected;
            public Dictionary<int, string> Values;
        }
    }



    public static class Template
    {
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
}