using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AshDumpTool;

public class ExtendedXmlWriter
{
    XmlWriter xmlWriter;

    string mainObjectName = "";

    public ExtendedXmlWriter() { }

    public ExtendedXmlWriter(string filepath) => Init(filepath, "", new());

    public ExtendedXmlWriter(string filepath, string objectName) => Init(filepath, objectName, new());

    public ExtendedXmlWriter(string filepath, string objectName, List<Tuple<string, object>> arguments) => Init(filepath, objectName, arguments);


    void Init(string filepath, string objectName, List<Tuple<string, object>> arguments)
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.Indent = true;
        xmlWriter = XmlWriter.Create(filepath, settings);
        xmlWriter.WriteStartDocument();
        mainObjectName = objectName;
        if (mainObjectName != "")
        {
            xmlWriter.WriteStartElement(objectName);
            foreach(var i in arguments)
                xmlWriter.WriteAttributeString(i.Item1, i.Item2.ToString());
        }
    }

    public void Write<T>(string name, T value)
    {
        if (typeof(T) == typeof(Vector2))
        {
            WriteObject(name, () =>
            {
                Write("X", ((Vector2)(object)value).X.ToString());
                Write("Y", ((Vector2)(object)value).Y.ToString());
            });
        }
        else if (typeof(T) == typeof(Vector3))
        {
            WriteObject(name, () =>
            {
                Write("X", ((Vector3)(object)value).X.ToString());
                Write("Y", ((Vector3)(object)value).Y.ToString());
                Write("Z", ((Vector3)(object)value).Z.ToString());
            });
        }
        else if (typeof(T) == typeof(Vector4))
        {
            WriteObject(name, () =>
            {
                Write("X", ((Vector4)(object)value).X.ToString());
                Write("Y", ((Vector4)(object)value).Y.ToString());
                Write("Z", ((Vector4)(object)value).Z.ToString());
                Write("W", ((Vector4)(object)value).W.ToString());
            });
        }
        else if (typeof(T) == typeof(Quaternion))
        {
            WriteObject(name, () =>
            {
                Write("X", ((Quaternion)(object)value).X.ToString());
                Write("Y", ((Quaternion)(object)value).Y.ToString());
                Write("Z", ((Quaternion)(object)value).Z.ToString());
                Write("W", ((Quaternion)(object)value).W.ToString());
            });
        }
        else
            xmlWriter.WriteElementString(name, value.ToString());
    }

    public void WriteObject(string name, Action objectData)
    {
        WriteObject(name, new(), objectData);
    }

    public void WriteObject(string name, List<Tuple<string, object>> attributes, Action objectData)
    {
        xmlWriter.WriteStartElement(name);
        foreach(var i in attributes)
            xmlWriter.WriteAttributeString(i.Item1, i.Item2.ToString());
        objectData();
        xmlWriter.WriteEndElement();
    }

    public void Close()
    {
        if (mainObjectName != "")
            xmlWriter.WriteEndElement();
        xmlWriter.WriteEndDocument();
        xmlWriter.Close();
    }
}
