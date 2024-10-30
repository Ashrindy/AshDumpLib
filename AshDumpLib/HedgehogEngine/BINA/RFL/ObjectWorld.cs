using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Numerics;
using libHSON;

namespace AshDumpLib.HedgehogEngine.BINA.RFL;

public class ObjectWorld : IFile
{
    public const string FileExtension = ".gedit";

    public List<Object> Objects = new();

    //Using JSON templates from HedgeLib++
    public static string TemplateFilePath = "frontiers.json";
    public static ReflectionData.Template.TemplateJSON TemplateData;

    public int FileVersion = 3;

    public ObjectWorld() { }

    public ObjectWorld(string filename) => Open(filename);
    public ObjectWorld(string filename, byte[] data) => Open(filename, data);
    public ObjectWorld(string filename, string templateFilePath = "frontiers.json") { TemplateFilePath = templateFilePath; TemplateData = ReflectionData.Template.GetTemplateFromFilePath(templateFilePath); Open(filename); }
    public ObjectWorld(string filename, byte[] data, string templateFilePath = "frontiers.json") { TemplateFilePath = templateFilePath; TemplateData = ReflectionData.Template.GetTemplateFromFilePath(templateFilePath); Open(filename, data); }

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        FileVersion = ReflectionData.Template.GetVersionFromTemplate(TemplateData.format);
        reader.FileVersion = FileVersion;

        reader.ReadHeader();
        reader.Skip(16);
        long dataPtr = reader.Read<long>();
        long objectCount = reader.Read<long>();
        long objectCount1 = reader.Read<long>();
        reader.Jump(dataPtr, SeekOrigin.Begin);
        for (int i = 0; i < objectCount; i++)
        {
            Object obj = new();
            obj.Read(reader);
            Objects.Add(obj);
        }
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        FileVersion = ReflectionData.Template.GetVersionFromTemplate(TemplateData.format);
        writer.FileVersion = FileVersion;

        writer.WriteHeader();
        writer.WriteNulls(16);
        writer.AddOffset("dataPtr");
        writer.Write((long)Objects.Count);
        writer.Write((long)Objects.Count);
        writer.WriteNulls(8);
        writer.SetOffset("dataPtr");
        foreach (var i in Objects)
            writer.AddOffset(i.ObjectName + i.ID.ToString());
        foreach (var i in Objects)
            i.Write(writer);
        foreach (var i in Objects)
            i.FinishWrite(writer);
        foreach (var i in Objects)
            foreach (var x in i.Tags)
                x.FinishWrite(writer);

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
        public ReflectionData Parameters = new(TemplateData);

        static int GetAlignment(ReflectionData.Template.StructTemplateField field, int fileVersion)
        {
            var template = TemplateData;
            if (field.alignment == null)
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
                        else if (fileVersion == 3)
                            align = 8;
                        break;

                    case "array":
                        if (field.array_size == null)
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

        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
            reader.ReadAtOffset(ptr + 64, () =>
            {
                reader.Skip(8);
                TypeName = reader.ReadStringTableEntry();
                ObjectName = reader.ReadStringTableEntry();
                if (reader.FileVersion == 3)
                {
                    reader.Skip(8);
                    ID = reader.Read<Guid>();
                    ParentID = reader.Read<Guid>();
                }
                else if (reader.FileVersion == 2)
                {
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
                    for (int i = 0; i < tagsCount; i++)
                    {
                        Tag tag = new();
                        tag.Read(reader);
                        Tags.Add(tag);
                    }
                });
                if (Parameters.GetTemplateData().objects.ContainsKey(TypeName))
                {
                    ReflectionData.Template.StructTemplate objTmp = Parameters.GetTemplateData().structs[Parameters.GetTemplateData().objects[TypeName].structs];
                    long paramPtr = reader.Read<long>();
                    reader.ReadAtOffset(paramPtr + 64, () =>
                    {
                        Parameters.SetStructName(TypeName);
                        Parameters.SetGedit(true);
                        Parameters.Read(reader);
                    });
                }
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
            else if (writer.FileVersion == 2)
            {
                writer.WriteStringTableEntry(ObjectName);
                writer.WriteArray(new byte[4] { ID.ToByteArray()[0], ID.ToByteArray()[1], ID.ToByteArray()[2], ID.ToByteArray()[3] });
                writer.WriteArray(new byte[4] { ParentID.ToByteArray()[0], ParentID.ToByteArray()[1], ParentID.ToByteArray()[2], ParentID.ToByteArray()[3] });
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
            Parameters.SetStructName(Parameters.GetTemplateData().objects[TypeName].structs);
            Parameters.SetGedit(true);
            Parameters.Write(writer);
        }

        public class Tag : IBINASerializable
        {
            public string Name = "";
            public ReflectionData Parameters = new(TemplateData);
            public Object Owner = new();

            private long dataSizePtr = 0;

            public void Read(BINAReader reader)
            {
                long ptr = reader.Read<long>();
                reader.ReadAtOffset(ptr + 64, () =>
                {
                    reader.Skip(8);
                    Name = reader.ReadStringTableEntry();
                    long size = reader.Read<long>();
                    long dataPtr = reader.Read<long>();
                    reader.ReadAtOffset(dataPtr + 64, () =>
                    {
                        if (Parameters.GetTemplateData().tags != null)
                        {
                            Parameters.SetStructName(Parameters.GetTemplateData().tags[Name].structs);
                            Parameters.SetGedit(true);
                            Parameters.Read(reader);
                        }
                    });
                });
            }

            public void Write(BINAWriter writer)
            {
                writer.SetOffset(Owner.ObjectName + Owner.ID.ToString() + Name);
                writer.WriteNulls(8);
                writer.WriteStringTableEntry(Name);
                dataSizePtr = writer.Position;
                writer.WriteNulls(8);
                writer.AddOffset(Owner.ObjectName + Owner.ID.ToString() + Name + "data");
                writer.Align(16);
            }

            public void FinishWrite(BINAWriter writer)
            {
                Dictionary<string, object> tempParam = new()
                {
                    { TemplateData.tags[Name].structs, Parameters.Parameters.ElementAt(0).Value }
                };
                Parameters.Parameters = tempParam;
                writer.Align(GetAlignment(Parameters.GetTemplateData().structs[Parameters.Parameters.ElementAt(0).Key].fields[0], writer.FileVersion));
                writer.SetOffset(Owner.ObjectName + Owner.ID.ToString() + Name + "data");
                long dataSize = writer.Position;
                Parameters.SetStructName(TemplateData.tags[Name].structs);
                Parameters.SetGedit(true);
                Parameters.Write(writer);
                dataSize = writer.Position - dataSize;
                writer.WriteAt(dataSize, dataSizePtr);
            }
        }
    }

    #region HSON

    #region ToHson
    public Project ToHson()
    {
        Project project = new();
        foreach (var i in Objects)
            project.Objects.Add(CreateHsonObject(i));
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
            tags = CreateHsonParameterCollection(x.Parameters.Parameters);
        obj.LocalParameters.Add("tags", new(tags));
        Dictionary<string, object> param = GeditToHsonParams(i.Parameters.Parameters);
        CreateHsonParameterCollection(param, ref obj.LocalParameters);
        return obj;
    }

    static Dictionary<string, object> GeditToHsonParams(Dictionary<string, object> ogparam)
    {
        Dictionary<string, object> newparam = new();
        foreach (var x in ogparam)
        {
            if (TemplateData.structs.ContainsKey(x.Key))
            {
                Dictionary<string,object> tempparam = GeditToHsonParams((Dictionary<string, object>)x.Value);
                foreach (var i in tempparam)
                    newparam.Add(i.Key, i.Value);
            }
            else
                newparam.Add(x.Key, x.Value);
        }
        return newparam;
    }

    ParameterCollection CreateHsonParameterCollection(Dictionary<string, object> parameter)
    {
        ParameterCollection paramColl = new();
        CreateHsonParameterCollection(parameter, ref paramColl);
        return paramColl;
    }

    void CreateHsonParameterCollection(Dictionary<string, object> parameter, ref ParameterCollection paramColl)
    {
        foreach (var i in parameter)
        {
            Parameter param = new();
            Type type = i.Value.GetType();
            if (type == typeof(byte))
                param = new((byte)i.Value);
            else if (type == typeof(bool))
                param = new((bool)i.Value);
            else if (type == typeof(ushort))
                param = new((ushort)i.Value);
            else if (type == typeof(short))
                param = new((short)i.Value);
            else if (type == typeof(uint))
                param = new((uint)i.Value);
            else if (type == typeof(int))
                param = new((int)i.Value);
            else if (type == typeof(ulong))
                param = new((ulong)i.Value);
            else if (type == typeof(long))
                param = new((long)i.Value);
            else if (type == typeof(float))
                param = new((float)i.Value);
            else if (type == typeof(double))
                param = new((double)i.Value);
            else if (type == typeof(ReflectionData.EnumValue))
                param = new(((ReflectionData.EnumValue)i.Value).Values[((ReflectionData.EnumValue)i.Value).Selected]);
            else if (type == typeof(string))
                param = new((string)i.Value);
            else if (type == typeof(Dictionary<string, object>))
                param = new(CreateHsonParameterCollection((Dictionary<string, object>)i.Value));
            else if (type == typeof(Guid))
                param = new(((Guid)i.Value).ToString());
            else if (type == typeof(Vector2))
            {
                List<Parameter> vector2 = new()
                {
                    new(((Vector2)i.Value).X),
                    new(((Vector2)i.Value).Y)
                };
                param = new(vector2);
            }
            else if (type == typeof(Vector3))
            {
                List<Parameter> vector3 = new()
                {
                    new(((Vector3)i.Value).X),
                    new(((Vector3)i.Value).Y),
                    new(((Vector3)i.Value).Z)
                };
                param = new(vector3);
            }
            else if (type == typeof(object[]))
            {
                List<Parameter> paramArray = new();
                foreach (var x in (object[])i.Value)
                {
                    Parameter paramItem = new();
                    Type typee = x.GetType();
                    if (typee == typeof(byte))
                        paramItem = new((byte)x);
                    else if (typee == typeof(bool))
                        paramItem = new((bool)x);
                    else if (typee == typeof(ushort))
                        paramItem = new((ushort)x);
                    else if (typee == typeof(short))
                        paramItem = new((short)x);
                    else if (typee == typeof(uint))
                        paramItem = new((uint)x);
                    else if (typee == typeof(int))
                        paramItem = new((int)x);
                    else if (typee == typeof(ulong))
                        paramItem = new((ulong)x);
                    else if (typee == typeof(long))
                        paramItem = new((long)x);
                    else if (typee == typeof(float))
                        paramItem = new((float)x);
                    else if (typee == typeof(double))
                        paramItem = new((double)x);
                    else if (typee == typeof(ReflectionData.EnumValue))
                        paramItem = new(((ReflectionData.EnumValue)x).Values[(int)((ReflectionData.EnumValue)x).Selected]);
                    else if (typee == typeof(string))
                        paramItem = new((string)x);
                    else if (typee == typeof(Dictionary<string, object>))
                        paramItem = new(CreateHsonParameterCollection((Dictionary<string, object>)x));
                    else if (typee == typeof(Guid))
                        paramItem = new(((Guid)x).ToString());
                    paramArray.Add(paramItem);
                }
                param = new(paramArray);
            }
            paramColl.Add(i.Key, param);
        }
    }
    #endregion

    #region FromHson
    public static ObjectWorld ToGedit(Project project)
    {
        ObjectWorld gedit = new();
        foreach (var i in project.Objects)
            gedit.Objects.Add(CreateGeditObject(i));
        return gedit;
    }

    static Object CreateGeditObject(libHSON.Object i)
    {
        Object obj = new();
        obj.ID = i.Id;
        obj.ParentID = i.ParentId;
        obj.ObjectName = i.Name;
        obj.TypeName = i.Type;
        obj.Position = i.LocalPosition;
        obj.Rotation = Helpers.ToEulerAngles(i.LocalRotation);
        obj.OffsetPosition = i.LocalPosition;
        obj.OffsetRotation = Helpers.ToEulerAngles(i.LocalRotation);
        foreach (var x in i.LocalParameters["tags"].ValueObject)
        {
            Object.Tag tag = new() { Name = x.Key };
            tag.Parameters = new(TemplateData);
            tag.Parameters.Parameters = CreateGeditParameter(new(TemplateData.tags[tag.Name].structs, x.Value), TemplateData.tags[tag.Name].structs);
            tag.Owner = obj;
            obj.Tags.Add(tag);
        }
        obj.Parameters = new(TemplateData);
        i.LocalParameters.Remove("tags");
        i.LocalParameters = ConvertHSONToGedit(i.LocalParameters, TemplateData.objects[obj.TypeName].structs);
        obj.Parameters.Parameters = CreateGeditParameter(new(TemplateData.objects[obj.TypeName].structs, new(i.LocalParameters)), TemplateData.objects[obj.TypeName].structs);
        obj.Parameters.Parameters = (Dictionary<string, object>)obj.Parameters.Parameters.ElementAt(0).Value;
        return obj;
    }

    static ParameterCollection ConvertHSONToGedit(ParameterCollection paramColl, string strName)
    {
        ParameterCollection mainColl = new();
        int start = 0;
        if (TemplateData.structs[strName].parent != null)
        {
            ParameterCollection parentColl = new();
            start = TemplateData.structs[TemplateData.structs[strName].parent].fields.Length;
            for (int i = 0; i < TemplateData.structs[TemplateData.structs[strName].parent].fields.Length; i++)
                parentColl.Add(paramColl.ElementAt(i).Key, paramColl.ElementAt(i).Value);
            mainColl.Add(TemplateData.structs[strName].parent, new(parentColl));
        }

        for (int i = start; i < paramColl.Count; i++)
            mainColl.Add(paramColl.ElementAt(i).Key, paramColl.ElementAt(i).Value);

        ParameterCollection finalColl = new()
        { { strName, new(mainColl) } };
        return finalColl;
    }

    static Dictionary<string, object> CreateGeditParameter(Tuple<string, Parameter> param, string strName)
    {
        Dictionary<string, object> prm = new()
        {
            { param.Item1, CreateGeditParameterObject(new(param.Item1, param.Item2), strName) }
        };
        return prm;
    }

    static object CreateGeditParameterObject(Tuple<string, Parameter> param, string strName)
    {
        var template = TemplateData;
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
                if(Guid.TryParse(param.Item2.ValueString, out _))
                {
                    value = Guid.Parse(param.Item2.ValueString);
                }
                else if (template.structs.ContainsKey(strName))
                {
                    bool isEnum = false;
                    foreach (var i in template.structs[strName].fields)
                    {
                        if (i.name == param.Item1 && template.enums.ContainsKey(i.type))
                        {
                            isEnum = true;
                            ReflectionData.EnumValue eVal = new();
                            eVal.Values = new();
                            foreach (var x in template.enums[i.type].values)
                                eVal.Values.Add(x.Value.value, x.Key);
                            eVal.Selected = template.enums[i.type].values[param.Item2.ValueString].value;
                            value = eVal;
                        }
                    }
                    if(!isEnum)
                        value = param.Item2.ValueString;
                }
                else
                    value = param.Item2.ValueString;
                break;

            case ParameterType.Array:
                if (param.Item2.ValueArray.Where(x => x.ValueFloatingPoint != null).Count() == 3)
                    value = new Vector3((float)param.Item2.ValueArray[0].ValueFloatingPoint, (float)param.Item2.ValueArray[1].ValueFloatingPoint, (float)param.Item2.ValueArray[2].ValueFloatingPoint);
                else if (param.Item2.ValueArray.Where(x => x.ValueFloatingPoint != null).Count() == 2)
                    value = new Vector2((float)param.Item2.ValueArray[0].ValueFloatingPoint, (float)param.Item2.ValueArray[1].ValueFloatingPoint);
                else
                {
                    value = new object[param.Item2.ValueArray.Count];
                    for (int i = 0; i < param.Item2.ValueArray.Count; i++)
                        ((object[])value)[i] = CreateGeditParameterObject(new(param.Item1, param.Item2.ValueArray[i]), TemplateData.structs[strName].fields.Where(x => x.name == param.Item1).First().subtype);
                }
                break;

            case ParameterType.Object:
                Dictionary<string, object> str = new();
                if(strName == param.Item1)
                    foreach (var i in param.Item2.ValueObject)
                        str.Add(i.Key, CreateGeditParameterObject(new(i.Key, i.Value), strName));
                else if(param.Item1 == TemplateData.structs[strName].parent)
                    foreach (var i in param.Item2.ValueObject)
                        str.Add(i.Key, CreateGeditParameterObject(new(i.Key, i.Value), TemplateData.structs[strName].parent));
                else
                    foreach (var i in param.Item2.ValueObject)
                        str.Add(i.Key, CreateGeditParameterObject(new(i.Key, i.Value), TemplateData.structs[strName].fields.Where(x => x.name == param.Item1).First().type));
                value = str;
                break;
        }
        return value;
    }
    #endregion

    #endregion
}