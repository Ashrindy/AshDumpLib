using AshDumpLib.HedgehogEngine.BINA.RFL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AshDumpTool;

public class ReflectionWriter
{
    ExtendedXmlWriter writer;
    ReflectionData rfl;

    public ReflectionWriter() { }
    public ReflectionWriter(ExtendedXmlWriter writer, ReflectionData rfl) { this.writer = writer; this.rfl = rfl; }

    public void Write()
    {
        WriteParam(rfl.Parameters.First().Key, rfl.Parameters.First().Value);
    }

    void WriteParam(string name, object value)
    {
        if(value != null)
        {
            Type type = value.GetType();
            if (type == typeof(byte))
                writer.Write(name, (byte)value);
            else if (type == typeof(bool))
                writer.Write(name, (bool)value);
            else if (type == typeof(ushort))
                writer.Write(name, (ushort)value);
            else if (type == typeof(short))
                writer.Write(name, (short)value);
            else if (type == typeof(uint))
                writer.Write(name, (uint)value);
            else if (type == typeof(int))
                writer.Write(name, (int)value);
            else if (type == typeof(ulong))
                writer.Write(name, (ulong)value);
            else if (type == typeof(long))
                writer.Write(name, (long)value);
            else if (type == typeof(float))
                writer.Write(name, (float)value);
            else if (type == typeof(double))
                writer.Write(name, (double)value);
            else if (type == typeof(ReflectionData.EnumValue))
                writer.Write(name, ((ReflectionData.EnumValue)value).Values[(int)((ReflectionData.EnumValue)value).Selected]);
            else if (type == typeof(string))
                writer.Write(name, (string)value);
            else if (type == typeof(Dictionary<string, object>))
                writer.WriteObject(name, () =>
                {
                    foreach (var i in (Dictionary<string, object>)value)
                        WriteParam(i.Key, i.Value);
                });
            else if (type == typeof(Guid))
                writer.Write(name, (Guid)value);
            else if (type == typeof(ReflectionData.BitFlag))
                writer.WriteObject(name, () =>
                {
                    foreach (var i in ((ReflectionData.BitFlag)value).Flags)
                        writer.Write(i.Key, i.Value);
                });
            else if (type == typeof(object[]))
            {
                writer.WriteObject(name, () =>
                {
                    int id = 0;
                    foreach (var x in (object[])value)
                    {
                        type = x.GetType();
                        if (type == typeof(byte))
                            writer.Write($"{name}-{id.ToString()}", (byte)x);
                        else if (type == typeof(bool))
                            writer.Write($"{name}-{id.ToString()}", (bool)x);
                        else if (type == typeof(ushort))
                            writer.Write($"{name}-{id.ToString()}", (ushort)x);
                        else if (type == typeof(short))
                            writer.Write($"{name}-{id.ToString()}", (short)x);
                        else if (type == typeof(uint))
                            writer.Write($"{name}-{id.ToString()}", (uint)x);
                        else if (type == typeof(int))
                            writer.Write($"{name}-{id.ToString()}", (int)x);
                        else if (type == typeof(ulong))
                            writer.Write($"{name}-{id.ToString()}", (ulong)x);
                        else if (type == typeof(long))
                            writer.Write($"{name}-{id.ToString()}", (long)x);
                        else if (type == typeof(float))
                            writer.Write($"{name}-{id.ToString()}", (float)x);
                        else if (type == typeof(double))
                            writer.Write($"{name}-{id.ToString()}", (double)x);
                        else if (type == typeof(ReflectionData.EnumValue))
                            writer.Write($"{name}-{id.ToString()}", ((ReflectionData.EnumValue)x).Values[(int)((ReflectionData.EnumValue)x).Selected]);
                        else if (type == typeof(string))
                            writer.Write($"{name}-{id.ToString()}", (string)x);
                        else if (type == typeof(Dictionary<string, object>))
                            writer.WriteObject(name, () =>
                            {
                                foreach (var i in (Dictionary<string, object>)x)
                                    WriteParam(i.Key, i.Value);
                            });
                        else if (type == typeof(Guid))
                            writer.Write($"{name}-{id.ToString()}", (Guid)x);
                        id++;
                    }
                });
            }
        }
    }
}
