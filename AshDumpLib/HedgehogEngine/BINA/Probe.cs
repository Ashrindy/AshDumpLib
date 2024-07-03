using Amicitia.IO.Binary;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA;

public class Probe
{
    public const string FileExtension = ".probe";
    public const string BINASignature = "DPIC";

    public int Version = 2;
    public List<ProbeItem> Probes = new();

    public Probe() { }

    public Probe(string filename)
    {
        Open(filename);
    }

    public void Open(string filename)
    {
        Read(new(filename, Endianness.Little, System.Text.Encoding.UTF8));
    }

    public void Save(string filename)
    {
        Write(new(filename, Endianness.Little, System.Text.Encoding.Default));
    }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        long tableOffset = reader.Read<long>();
        long count = reader.Read<long>();
        reader.Jump(tableOffset, SeekOrigin.Begin);
        for(int i = 0; i < count; i++) 
        {
            ProbeItem item = new ProbeItem();
            item.Read(reader);
            Probes.Add(item);
        }
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.AddOffset("table");
        writer.Write<long>(Probes.Count);
        writer.SetOffset("table");
        foreach(var i in Probes)
        {
            i.Write(writer);
        }
        writer.FinishWrite();
        writer.Dispose();
    }
}

public class ProbeItem
{
    public struct UnkStruct
    {
        public Vector3 Unk0;
        public float Unk1;
    }

    public string Name = "";
    public UnkStruct[] Unk0 = new UnkStruct[5];
    public int[] Unk1 = new int[8];

    public void Read(BINAReader reader)
    {
        Unk0 = reader.ReadArray<UnkStruct>(5);
        Name = reader.ReadStringTableEntry();
        Unk1 = reader.ReadArray<int>(8);
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteArray(Unk0);
        writer.WriteStringTableEntry(Name);
        writer.WriteArray(Unk1);
    }
}