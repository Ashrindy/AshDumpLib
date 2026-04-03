using AshDumpLib.HedgehogEngine.BINA;
using AshDumpLib.HedgehogEngine.BINA.RFL;
using System.Numerics;
using System.Xml;
using static AshDumpLib.HedgehogEngine.BINA.RFL.ReflectionData;
using static AshDumpLib.HedgehogEngine.BINA.RFL.ReflectionData.Template;
using static AshDumpLib.Helpers.MathA;

namespace AshDumpTool;

public class ReflectionReader
{
    XmlDocument reader = new();
    string className = "";
    string templateFilepath = "";
    ReflectionData reflectionData = new();

    public ReflectionReader() { }
    public ReflectionReader(XmlDocument reader, string className, string templateFilepath)
    {
        this.reader = reader;
        this.className = className;
        this.templateFilepath = templateFilepath;
    }

    public ReflectionData GetReflectionData()
    {
        reflectionData = new ReflectionData(templateFilepath);
        reflectionData.SetStructName(className);

        reflectionData.Parameters = ReadStruct(reader.DocumentElement.FirstChild, reflectionData.GetTemplateData().structs[className]);

        return reflectionData;
    }

    Dictionary<string, object> ReadStruct(XmlNode elem, StructTemplate str)
    {
        Dictionary<string, object> parameters = new();

        int fieldIdx = 0;
        if (str.parent != null)
        {
            parameters.Add(str.parent, ReadStruct(elem.FirstChild, reflectionData.GetTemplateData().structs[str.parent]));
            fieldIdx++;
        }
        if (str.fields != null)
        {
            foreach (var i in str.fields)
            {
                parameters.Add(i.name, ReadField(elem.ChildNodes[fieldIdx], i.type, i, parameters));
                fieldIdx++;
            }
        }

        return parameters;
    }

    object ReadField(XmlNode elem, string type, StructTemplateField field, Dictionary<string, object> parent)
    {
        object value = null;

        bool isStruct = false;
        string subtype = "";
        if (field.array_size != null)
        {
            subtype = field.subtype;
            type = "array";
        }
        switch (type)
        {
            case "bool":
                value = bool.Parse(elem.InnerXml);
                break;

            case "float32":
                value = float.Parse(elem.InnerXml);
                break;

            case "float64":
                value = double.Parse(elem.InnerXml);
                break;

            case "uint8" or "int8":
                value = byte.Parse(elem.InnerXml);
                break;

            case "uint16":
                value = ushort.Parse(elem.InnerXml);
                break;

            case "int16":
                value = short.Parse(elem.InnerXml);
                break;

            case "uint32":
                value = uint.Parse(elem.InnerXml);
                break;

            case "int32":
                value = int.Parse(elem.InnerXml);
                break;

            case "uint64":
                value = ulong.Parse(elem.InnerXml);
                break;

            case "int64":
                value = long.Parse(elem.InnerXml);
                break;

            case "string":
                value = elem.InnerXml;
                break;


            case "array":
                object[] arrayValue = new object[0];

                if (field.array_size == null) arrayValue = new object[elem.ChildNodes.Count];
                else arrayValue = new object[(int)field.array_size];

                for (int i = 0; i < arrayValue.Length; i++)
                    arrayValue[i] = ReadField(elem.ChildNodes[i], subtype, new() { name = field.name, type = field.subtype }, parent);

                value = arrayValue;
                break;

            case "object_reference":
                if (reflectionData.GetTemplateData().format == "gedit_v3")
                    value = Guid.Parse(elem.InnerXml);
                else
                    value = int.Parse(elem.InnerXml);
                break;

            case "vector2":
                value = new Vector2(float.Parse(elem.ChildNodes[0].InnerText), float.Parse(elem.ChildNodes[1].InnerText));
                break;

            case "vector3":
                value = new Vector3(float.Parse(elem.ChildNodes[0].InnerText), float.Parse(elem.ChildNodes[1].InnerText), float.Parse(elem.ChildNodes[2].InnerText));
                break;

            case "vector4":
                value = new Vector4(float.Parse(elem.ChildNodes[0].InnerText), float.Parse(elem.ChildNodes[1].InnerText), float.Parse(elem.ChildNodes[2].InnerText), float.Parse(elem.ChildNodes[3].InnerText));
                break;

            /*case "matrix44" or "matrix34":
                value = reader.Read<Matrix4x4>();
                break;

            case "color8":
                value = reader.Read<Color8>();
                break;

            case "colorf":
                value = reader.Read<ColorF>();
                break;*/

            /*case "flags":
                if (field.flags == null)
                {
                    switch (field.subtype)
                    {
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
                    }
                }
                switch (field.subtype)
                {
                    case "uint8" or "int8":
                        value = BoolsToBitFlag(FlagsToBools(reader.Read<byte>()), field.flags);
                        break;

                    case "uint16":
                        value = BoolsToBitFlag(FlagsToBools(reader.Read<ushort>()), field.flags);
                        break;

                    case "int16":
                        value = BoolsToBitFlag(FlagsToBools(reader.Read<short>()), field.flags);
                        break;

                    case "uint32":
                        value = BoolsToBitFlag(FlagsToBools(reader.Read<uint>()), field.flags);
                        break;

                    case "int32":
                        value = BoolsToBitFlag(FlagsToBools(reader.Read<int>()), field.flags);
                        break;

                    case "uint64":
                        value = BoolsToBitFlag(FlagsToBools(reader.Read<ulong>()), field.flags);
                        break;

                    case "int64":
                        value = BoolsToBitFlag(FlagsToBools(reader.Read<long>()), field.flags);
                        break;
                }
                break;*/

            default:
                if (type.Contains("::"))
                {
                    if (reflectionData.GetTemplateData().enums.ContainsKey(type))
                    {
                        EnumValue eValue = new();
                        string enumStringValue = elem.InnerXml;
                        eValue.Values = new();
                        switch (reflectionData.GetTemplateData().enums[type].type)
                        {
                            case "int8":
                                eValue.Selected = (byte)reflectionData.GetTemplateData().enums[type].values[enumStringValue].value;
                                if (eValue.Selected == 255)
                                    eValue.Selected = -1;
                                break;

                            case "uint8":
                                eValue.Selected = (byte)reflectionData.GetTemplateData().enums[type].values[enumStringValue].value;
                                break;

                            case "uint16":
                                eValue.Selected = (ushort)reflectionData.GetTemplateData().enums[type].values[enumStringValue].value;
                                break;

                            case "int16":
                                eValue.Selected = (short)reflectionData.GetTemplateData().enums[type].values[enumStringValue].value;
                                break;

                            case "uint32" or "int32" or "uint64" or "int64":
                                eValue.Selected = reflectionData.GetTemplateData().enums[type].values[enumStringValue].value;
                                break;
                        }
                        foreach (var x in reflectionData.GetTemplateData().enums[type].values)
                            if (!eValue.Values.ContainsKey(x.Value.value))
                                eValue.Values.Add(x.Value.value, x.Key);
                        value = eValue;
                    }
                }
                else if (reflectionData.GetTemplateData().structs.ContainsKey(type))
                {
                    isStruct = true;
                    value = ReadStruct(elem, reflectionData.GetTemplateData().structs[type]);
                }
                break;
        }

        return value;
    }
}
