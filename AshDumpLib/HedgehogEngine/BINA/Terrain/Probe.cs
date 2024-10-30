using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA.Terrain;

public class Probe : IFile
{
    public const string FileExtension = ".probe";
    public const string BINASignature = "DPIC";

    public int Version = 2;
    public List<ProbeItem> Probes = new();

    public Probe() { }

    public Probe(string filename) => Open(filename);
    public Probe(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        Probes = reader.ReadBINAArrayStruct64<ProbeItem>();
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.WriteBINAArray64(Probes, "probes");
        writer.FinishWrite();
        writer.Dispose();
    }

    public class ProbeItem : IBINASerializable
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

        public void FinishWrite(BINAWriter writer) { }
    }
}