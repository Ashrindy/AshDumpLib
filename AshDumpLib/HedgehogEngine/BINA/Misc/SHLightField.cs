using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA.Misc;

public class SHLightField : IFile
{
    public const string FileExtension = ".shlf, .lf";

    public uint Version = 1;
    public float[] DefaultProbeLightingData = new float[36];
    public List<Node> Nodes = new();

    public SHLightField() { }

    public SHLightField(string filename) => Open(filename);
    public SHLightField(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();

        Version = reader.Read<uint>();
        DefaultProbeLightingData = reader.ReadArray<float>(36);
        uint nodeCount = reader.Read<uint>();
        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
        {
            for (int i = 0; i < nodeCount; i++)
            {
                Node node = new();
                node.Read(reader);
                Nodes.Add(node);
            }
        });

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();

        writer.Write(Version);
        writer.WriteArray(DefaultProbeLightingData);
        writer.Write((uint)Nodes.Count);
        writer.AddOffset("nodes");
        writer.SetOffset("nodes");
        foreach (var i in Nodes)
            i.Write(writer);

        writer.FinishWrite();
        writer.Dispose();
    }

    public class Node
    {
        public string Name = "";
        public uint[] Resolution = { 0, 0, 0 };
        public Vector3 Position = new(0, 0, 0);
        public Vector3 Rotation = new(0, 0, 0);
        public Vector3 Scale = new(1, 1, 1);

        public Node() { }

        public void Read(BINAReader reader)
        {
            Name = reader.ReadStringTableEntry();
            Resolution = reader.ReadArray<uint>(3);
            Position = reader.Read<Vector3>();
            Rotation = reader.Read<Vector3>();
            Scale = reader.Read<Vector3>();
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(Name);
            writer.WriteArray(Resolution);
            writer.Write(Position);
            writer.Write(Rotation);
            writer.Write(Scale);
        }
    }
}