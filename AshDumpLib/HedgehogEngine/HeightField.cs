using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine;

public class HeightField : IFile
{
    public const string FileExtension = ".heightfield";
    public const string Signature = "HTFD";

    public int Version = 1;
    public Vector2 TerrainSize = new(4096, 4096);
    public float TexelDensity = 1;
    public float HeightPrecision = 0.03051851f;
    public float HeightRange = 1000;
    public float[] CollisionData = new float[0];
    public byte[] MaterialData = new byte[0];
    public List<Material> Materials = new();

    public HeightField() { }

    public HeightField(string filename) => Open(filename);
    public HeightField(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.ReadSignature(Signature);
        int fileSize = reader.Read<int>();
        Version = reader.Read<int>();
        int unk0 = reader.Read<int>();
        int unk1 = reader.Read<int>();
        TerrainSize.X = reader.Read<int>();
        TerrainSize.Y = reader.Read<int>();
        TexelDensity = reader.Read<float>();
        float unk2 = reader.Read<float>();
        HeightPrecision = reader.Read<float>();
        HeightRange = reader.Read<float>();
        ushort[] collData = reader.ReadArray<ushort>((int)TerrainSize.X * (int)TerrainSize.Y);
        CollisionData = new float[(int)TerrainSize.X * (int)TerrainSize.Y];
        for(int i = 0; i < collData.Length; i++)
            CollisionData[i] = (float)collData[i] / 32768f;
        int matCount = reader.Read<int>();
        Materials.AddRange(reader.ReadArray<Material>(matCount));
        MaterialData = reader.ReadArray<byte>(((int)TerrainSize.X - 1) * ((int)TerrainSize.Y - 1));
        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteSignature(Signature);
        writer.AddOffset("fileSize");
        writer.Write(Version);
        writer.Write(2);
        writer.Write(0);
        writer.Write((int)TerrainSize.X);
        writer.Write((int)TerrainSize.Y);
        writer.Write(TexelDensity);
        writer.Write(1f);
        writer.Write(HeightPrecision);
        writer.Write(HeightRange);
        foreach (var i in CollisionData)
            writer.Write((ushort)(i * 32768));
        writer.Write(Materials.Count);
        writer.WriteArray(Materials.ToArray());
        foreach (var i in MaterialData)
            writer.Write(i);
        long fileSize = writer.Position;
        writer.WriteAt(fileSize, writer.GetOffset("fileSize"));
        writer.Dispose();
    }

    public struct Material
    {
        public short Unk0;
        public short Unk1;
        public int Unk2;
    }
}
