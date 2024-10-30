using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.CastleSiege;

public class HeightLevelMap : IFile
{
    public const string FileExtension = ".hlm";

    public List<Point> Points = new();

    public HeightLevelMap() { }
    public HeightLevelMap(string filepath) => Open(filepath);
    public HeightLevelMap(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        for(int i = 0; i < Data.Length / 36; i++)
            Points.Add(reader.Read<Point>());
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        foreach(var i in Points)
            writer.Write(i);
    }


    public struct Point
    {
        public Vector3 Position;
        public Vector3 Unk0;
        public Vector2 Unk1;
        public int Unk2;
    }
}
