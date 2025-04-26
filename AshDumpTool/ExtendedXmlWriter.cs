using System.Numerics;
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
        xmlWriter.WriteElementString(name, value.ToString());
    }

    public void Write(string name, Vector2 value)
    {
        WriteObject(name, () =>
        {
            Write("X", value.X.ToString());
            Write("Y", value.Y.ToString());
        });
    }

    public void Write(string name, Vector3 value)
    {
        WriteObject(name, () =>
        {
            Write("X", value.X.ToString());
            Write("Y", value.Y.ToString());
            Write("Z", value.Z.ToString());
        });
    }

    public void Write(string name, Vector4 value)
    {
        WriteObject(name, () =>
        {
            Write("X", value.X.ToString());
            Write("Y", value.Y.ToString());
            Write("Z", value.Z.ToString());
            Write("W", value.W.ToString());
        });
    }

    public void Write(string name, Quaternion value)
    {
        WriteObject(name, () =>
        {
            Write("X", value.X.ToString());
            Write("Y", value.Y.ToString());
            Write("Z", value.Z.ToString());
            Write("W", value.W.ToString());
        });
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
