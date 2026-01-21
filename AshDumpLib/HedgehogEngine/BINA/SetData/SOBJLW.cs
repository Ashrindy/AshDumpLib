using Amicitia.IO.Binary;
using AshDumpLib.HedgehogEngine.BINA.RFL;
using AshDumpLib.Helpers;
using AshDumpLib.Helpers.Archives;
using libHSON;
using Newtonsoft.Json.Linq;
using System;
using System.Numerics;
using static AshDumpLib.HedgehogEngine.BINA.Converse.TextMeta;

namespace AshDumpLib.HedgehogEngine.BINA.SetData;

public class SOBJLW : IFile {
    public const string FileExtension = ".orc";
    public const string BINASignature = "JBOS";

    public List<Object> Objects = new();

    //Using JSON templates from HedgeLib++
    public static string TemplateFilePath = "lostworld.json";
    public static ReflectionData.Template.TemplateJSON TemplateData;

    public int FileVersion = 1;

    public SOBJLW() { }

    public SOBJLW(string filename) => Open(filename);
    public SOBJLW(string filename, byte[] data) => Open(filename, data);
    public SOBJLW(string filename, string templateFilePath = "frontiers.json") { TemplateFilePath = templateFilePath; TemplateData = ReflectionData.Template.GetTemplateFromFilePath(templateFilePath); Open(filename); }
    public SOBJLW(string filename, byte[] data, string templateFilePath = "frontiers.json") { TemplateFilePath = templateFilePath; TemplateData = ReflectionData.Template.GetTemplateFromFilePath(templateFilePath); Open(filename, data); }

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader) {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        FileVersion = reader.Read<int>();
        reader.FileVersion = FileVersion;
        reader.Bit64 = false;

        int objectTypeCount = reader.Read<int>();
        List<ObjectType> objectTypes = new();
        reader.ReadAtOffset(reader.ReadPointer() + 64, () =>
        {
            for (int i = 0; i < objectTypeCount; i++) {
                ObjectType type = new();
                type.Read(reader);
                objectTypes.Add(type);
            }
        });

        long bvhPtr = reader.ReadPointer();

        long objectPtr = reader.ReadPointer();
        int objectCount = reader.Read<int>();
        List<RawObject> rawObjects = new();
        reader.ReadAtOffset(objectPtr + 64, () =>
        {
            for (int i = 0; i < objectCount; i++)
            {
                RawObject rawObj = new();
                rawObj.Read(reader);
                rawObjects.Add(rawObj);
            }
        });

        int bvhNodeCount = reader.Read<int>();
        int objectsCount = reader.Read<int>();

        foreach (var i in objectTypes) {
            foreach (var x in i.ObjectIndices) {
                var rawObj = rawObjects[x];
                var obj = rawObj.ConvertTo();
                obj.TypeName = i.Name;
                reader.ReadAtOffset(rawObj.parametersOffset, () =>
                {
                    obj.Parameters.SetStructName(TemplateData.objects[obj.TypeName].structs);
                    obj.Parameters.Read(reader);
                });
                Objects.Add(obj);
            }
        }

        reader.Dispose();
    }

    public void Write(BINAWriter writer) {
        writer.FileVersion = FileVersion;
        writer.Bit64 = false;
        writer.BINAVersion = "200";

        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(FileVersion);

        Dictionary<string, List<short>> objectTypes = new();
        int instanceCount = 0;

        for (int i = 0; i < Objects.Count; i++)
        {
            var typeName = Objects[i].TypeName;
            if (objectTypes.ContainsKey(typeName))
                objectTypes[typeName].Add((short)i);
            else
                objectTypes.Add(typeName, new() { (short)i });

            instanceCount += Objects[i].Instances.Count;
        }

        writer.Write(objectTypes.Count);
        writer.AddOffset("objectTypes");

        writer.Write(0);

        writer.AddOffset("objects");
        writer.Write(Objects.Count);

        writer.Write(0);
        writer.Write(instanceCount);

        writer.SetOffset("objectTypes");
        foreach (var i in objectTypes)
        {
            writer.WriteStringTableEntry(i.Key);
            writer.Write(i.Value.Count);
            writer.AddOffset($"{i.Key}indices");
        }

        foreach (var i in objectTypes) {
            writer.SetOffset($"{i.Key}indices");
            foreach (var x in i.Value) writer.Write(x);
        }

        writer.FixPadding(4);

        writer.SetOffset("objects");
        for (int i = 0; i < Objects.Count; i++) writer.AddOffset($"objectAt{i}");

        for (int i = 0; i < Objects.Count; i++) {
            var obj = Objects[i];
            writer.FixPadding(16);
            writer.SetOffset($"objectAt{i}");
            writer.Write(obj.ID);
            writer.Write(obj.ObjectClassID);
            writer.Write(obj.BVHNode);
            writer.Write(obj.ReplicationInterval);
            writer.Write(obj.Distance);
            writer.Write(obj.Range);
            writer.Write(obj.ParentID);
            writer.AddOffset($"objectAt{i}transforms");
            writer.Write(obj.Instances.Count);
            writer.FixPadding(16);
            obj.Parameters.Write(writer);
        }

        for (int i = 0; i < Objects.Count; i++) {
            var obj = Objects[i];
            writer.SetOffset($"objectAt{i}transforms");
            foreach (var x in obj.Instances)
                writer.Write(x);
        }

        writer.FinishWrite();
        writer.Dispose();
    }

    class ObjectType {
        public string Name = "";
        public List<short> ObjectIndices = new();

        public void Read(BINAReader reader) {
            Name = reader.ReadStringTableEntry();
            int objectIndexCount = reader.Read<int>();
            reader.ReadAtOffset(reader.ReadPointer() + 64, () =>
            {
                for (int i = 0; i < objectIndexCount; i++)
                    ObjectIndices.Add(reader.Read<short>());
            });
        }
    }

    public struct TransformData
    {
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 localPosition;
        public Vector3 localRotation;
    }

    class RawObject {
        public uint ID = 0;
        public uint ObjectClassID = 0;
        public int BVHNode = 0;
        public float ReplicationInterval = 0;
        public float Distance = 0;
        public float Range = 0;
        public uint ParentID = 0;
        public List<TransformData> transformDatas = new();
        public long parametersOffset = 0;

        public void Read(BINAReader reader) {
            reader.ReadAtOffset(reader.ReadPointer() + 64, () =>
            {
                ID = reader.Read<uint>();
                ObjectClassID = reader.Read<uint>();
                BVHNode = reader.Read<int>();
                ReplicationInterval = reader.Read<float>();
                Distance = reader.Read<float>();
                Range = reader.Read<float>();
                ParentID = reader.Read<uint>();
                long transformDataPtr = reader.ReadPointer();
                int transformDataCount = reader.Read<int>();
                reader.ReadAtOffset(transformDataPtr + 64, () =>
                {
                    for (int i = 0; i < transformDataCount; i++)
                        transformDatas.Add(reader.Read<TransformData>());
                });
                reader.Align(16);
                parametersOffset = reader.Position;
            });
        }

        public Object ConvertTo() {
            Object obj = new();
            obj.ID = ID;
            obj.ObjectClassID = ObjectClassID;
            obj.BVHNode = BVHNode;
            obj.ReplicationInterval = ReplicationInterval;
            obj.Distance = Distance;
            obj.Range = Range;
            obj.ParentID = ParentID;
            obj.Instances = transformDatas;
            return obj;
        }
    }

    public class Object {
        public uint ID = 0;
        public string TypeName = "";
        public uint ObjectClassID = 0;
        public int BVHNode = 0;
        public float ReplicationInterval = 0;
        public float Distance = 0;
        public float Range = 0;
        public uint ParentID = 0;
        public List<TransformData> Instances = new();
        public ReflectionData Parameters = new(TemplateData);

        public override string ToString() => $"{ID} - {TypeName}";
    }

    #region HSON

    public string ToHsonString(System.Text.Json.JsonWriterOptions options = default)
    {
        MemoryStream memStream = new();
        System.Text.Json.Utf8JsonWriter teswrti = new(memStream, options);
        Project project = ToHson();
        project.Write(teswrti);
        teswrti.Dispose();
        string value = System.Text.Encoding.UTF8.GetString(memStream.ToArray());
        memStream.Dispose();
        memStream.Close();
        return value;
    }

    public static libHSON.Project FromHsonString(string value)
    {
        return libHSON.Project.FromData(System.Text.Encoding.UTF8.GetBytes(value));
    }


    #region ToHson
    public Project ToHson()
    {
        Project project = new();
        foreach (var i in Objects) {
            var objs = CreateHsonObject(i);
            foreach (var x in objs)
                project.Objects.Add(x);
        }
        foreach (var i in Objects) {
            if (i.ParentID != 0)
                project.Objects.Where(x => x.Id == IntToGuid(i.ID) && x.InstanceOf == null).First().Parent = project.Objects.Where(x => x.Id == IntToGuid(i.ParentID) && x.InstanceOf == null).First();
        }
        return project;
    }

    public static Guid IntToGuid(uint id) {
        byte[] bytes = new byte[16];
        BitConverter.GetBytes(id).CopyTo(bytes, 0);
        return new Guid(bytes);
    }

    List<libHSON.Object> CreateHsonObject(Object i)
    {
        List<libHSON.Object> objects = new();
        libHSON.Object obj = new();
        obj.Id = IntToGuid(i.ID);
        obj.Type = i.TypeName;
        obj.LocalPosition = i.Instances[0].position + i.Instances[0].localPosition;
        obj.LocalRotation = Helpers.ToQuaternion(i.Instances[0].rotation) * Helpers.ToQuaternion(i.Instances[0].localRotation);
        ParameterCollection tags = new();
        ParameterCollection rangeSpawningTags = new()
        {
            { "rangeIn", new(i.Range) },
            { "rangeOut", new(i.Distance) }
        };
        tags.Add("RangeSpawning", new(rangeSpawningTags));
        obj.LocalParameters.Add("tags", new(tags));
        Dictionary<string, object> param = SOBJToHsonParams(i.Parameters.Parameters);
        CreateHsonParameterCollection(param, ref obj.LocalParameters);
        objects.Add(obj);

        for (int x = 1; x < i.Instances.Count; x++) {
            libHSON.Object instance = new();
            byte[] bytes = new byte[16];
            BitConverter.GetBytes(i.ID).CopyTo(bytes, 0);
            BitConverter.GetBytes(x).CopyTo(bytes, 8);
            instance.Id = new Guid(bytes);
            instance.Type = i.TypeName;
            instance.InstanceOf = obj;
            instance.LocalPosition = i.Instances[x].position + i.Instances[x].localPosition;
            instance.LocalRotation = Helpers.ToQuaternion(i.Instances[x].rotation) * Helpers.ToQuaternion(i.Instances[x].localRotation);
            objects.Add(instance);
        }
        return objects;
    }

    static Dictionary<string, object> SOBJToHsonParams(Dictionary<string, object> ogparam)
    {
        Dictionary<string, object> newparam = new();
        foreach (var x in ogparam)
        {
            if (TemplateData.structs.ContainsKey(x.Key))
            {
                Dictionary<string, object> tempparam = SOBJToHsonParams((Dictionary<string, object>)x.Value);
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
    public static SOBJLW ToSOBJ(Project project)
    {
        SOBJLW sobj = new();
        foreach (var i in project.Objects)
            if (!i.IsExcluded || i.InstanceOf == null)
                sobj.Objects.Add(CreateSOBJObject(i, ref project.Objects));
        return sobj;
    }

    static Object CreateSOBJObject(libHSON.Object i, ref ObjectCollection coll)
    {
        Object obj = new();
        obj.ID = BitConverter.ToUInt32(i.Id.ToByteArray(), 0);
        obj.ParentID = BitConverter.ToUInt32(i.ParentId.ToByteArray(), 0);
        obj.TypeName = i.Type;
        TransformData rootTransform = new();
        rootTransform.position = i.LocalPosition;
        rootTransform.rotation = Helpers.ToEulerAngles(i.LocalRotation);
        obj.Instances.Add(rootTransform);
        foreach (var x in coll) {
            if (x.InstanceOfId == i.Id)
            {
                TransformData transform = new();
                transform.position = x.LocalPosition;
                transform.rotation = Helpers.ToEulerAngles(x.LocalRotation);
                obj.Instances.Add(transform);
            }    
        }
        var param = i.LocalParameters["tags"].ValueObject["RangeSpawning"].ValueObject;
        obj.Distance = (float)param["rangeIn"].ValueFloatingPoint;
        obj.Range = (float)param["rangeOut"].ValueFloatingPoint;
        obj.Parameters = new(TemplateData);
        i.LocalParameters.Remove("tags");
        i.LocalParameters = ConvertHSONToSOBJ(i.LocalParameters, TemplateData.objects[obj.TypeName].structs);
        obj.Parameters.SetStructName(TemplateData.objects[obj.TypeName].structs);
        obj.Parameters.Parameters = (Dictionary<string, object>)CreateSOBJParameterObject(new(TemplateData.objects[obj.TypeName].structs, new(i.LocalParameters)), TemplateData.objects[obj.TypeName].structs);
        obj.Parameters.Parameters = (Dictionary<string, object>)obj.Parameters.Parameters.ElementAt(0).Value;
        return obj;
    }

    static ParameterCollection ConvertHSONToSOBJ(ParameterCollection paramColl, string strName)
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

    static Dictionary<string, object> CreateSOBJParameter(Tuple<string, Parameter> param, string strName)
    {
        Dictionary<string, object> prm = new()
        {
            { param.Item1, CreateSOBJParameterObject(new(param.Item1, param.Item2), strName) }
        };
        return prm;
    }

    static object CreateSOBJParameterObject(Tuple<string, Parameter> param, string strName)
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
                if (Guid.TryParse(param.Item2.ValueString, out _))
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
                    if (!isEnum)
                        value = param.Item2.ValueString;
                }
                else
                    value = param.Item2.ValueString;
                break;

            case ParameterType.Array:
                if (param.Item2.ValueArray[0].IsFloatingPoint)
                {
                    if (param.Item2.ValueArray.Where(x => x.ValueFloatingPoint != null).Count() == 3)
                        value = new Vector3((float)param.Item2.ValueArray[0].ValueFloatingPoint, (float)param.Item2.ValueArray[1].ValueFloatingPoint, (float)param.Item2.ValueArray[2].ValueFloatingPoint);
                    else if (param.Item2.ValueArray.Where(x => x.ValueFloatingPoint != null).Count() == 2)
                        value = new Vector2((float)param.Item2.ValueArray[0].ValueFloatingPoint, (float)param.Item2.ValueArray[1].ValueFloatingPoint);
                    else
                    {
                        value = new object[param.Item2.ValueArray.Count];
                        for (int i = 0; i < param.Item2.ValueArray.Count; i++)
                            ((object[])value)[i] = CreateSOBJParameterObject(new(param.Item1, param.Item2.ValueArray[i]), TemplateData.structs[strName].fields.Where(x => x.name == param.Item1).First().subtype);
                    }
                }
                else
                {
                    value = new object[param.Item2.ValueArray.Count];
                    for (int i = 0; i < param.Item2.ValueArray.Count; i++)
                        ((object[])value)[i] = CreateSOBJParameterObject(new(param.Item1, param.Item2.ValueArray[i]), TemplateData.structs[strName].fields.Where(x => x.name == param.Item1).First().subtype);
                }
                break;

            case ParameterType.Object:
                Dictionary<string, object> str = new();
                if (strName == param.Item1)
                    foreach (var i in param.Item2.ValueObject)
                        str.Add(i.Key, CreateSOBJParameterObject(new(i.Key, i.Value), strName));
                else if (param.Item1 == TemplateData.structs[strName].parent)
                    foreach (var i in param.Item2.ValueObject)
                        str.Add(i.Key, CreateSOBJParameterObject(new(i.Key, i.Value), TemplateData.structs[strName].parent));
                else
                    foreach (var i in param.Item2.ValueObject)
                    {
                        if (TemplateData.structs[strName].fields.Where(x => x.name == param.Item1).Count() > 0)
                            str.Add(i.Key, CreateSOBJParameterObject(new(i.Key, i.Value), TemplateData.structs[strName].fields.Where(x => x.name == param.Item1).First().type));
                        else
                        {
                            str.Add(i.Key, CreateSOBJParameterObject(new(i.Key, i.Value), strName));
                        }

                    }

                value = str;
                break;
        }
        return value;
    }
    #endregion
    #endregion
}