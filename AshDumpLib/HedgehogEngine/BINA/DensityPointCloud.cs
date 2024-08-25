using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA;

public class DensityPointCloud : IFile
{
    public const string FileExtension = ".densitypointcloud";
    public const string BINASignature = "EIYD";

    public int Version = 4;
    public List<FoliagePoint> FoliagePoints = new();

    public DensityPointCloud() { }

    public DensityPointCloud(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() => Write(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);

        Version = reader.Read<int>();
        long unk0 = reader.Read<long>();
        long dataPtr = reader.Read<long>();
        long pointCount = reader.Read<long>();
        reader.Jump(dataPtr, SeekOrigin.Begin);
        for (int i = 0; i < pointCount; i++)
        {
            FoliagePoint foliagepoint = new();
            foliagepoint.Read(reader);
            FoliagePoints.Add(foliagepoint);
        }

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);

        writer.Write(Version);
        writer.Write((long)2);
        writer.AddOffset("dataOffset");
        writer.Write(FoliagePoints.Count);
        writer.SetOffset("dataOffset");
        foreach (var i in FoliagePoints)
            i.Write(writer);

        writer.FinishWrite();
        writer.Dispose();
    }

    public class FoliagePoint : IBINASerializable
    {
        public int ID = 0;
        public Vector3 Position = new(0, 0, 0);
        public Quaternion Rotation = new(0, 0, 0, 1);
        public Vector3 Scale = new(1, 1, 1);
        public int Unk = 0;

        public void Read(BINAReader reader)
        {
            Position = reader.Read<Vector3>();
            reader.Skip(4);
            Scale = reader.Read<Vector3>();
            reader.Skip(4);
            Rotation = reader.Read<Quaternion>();
            reader.Skip(16);
            Unk = reader.Read<int>();
            ID = reader.Read<int>();
            reader.Skip(12);
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(Position);
            writer.WriteNulls(4);
            writer.Write(Scale);
            writer.WriteNulls(4);
            writer.Write(Rotation);
            writer.WriteNulls(16);
            writer.Write(Unk);
            writer.Write(ID);
            writer.WriteNulls(12);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}