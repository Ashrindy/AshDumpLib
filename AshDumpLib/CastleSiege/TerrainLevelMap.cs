using AshDumpLib.Helpers.Archives;

namespace AshDumpLib.CastleSiege;

public class TerrainLevelMap : IFile
{
    public const string FileExtension = ".tlm";

    public List<int> Points = new();

    public TerrainLevelMap() { }
    public TerrainLevelMap(string filepath) => Open(filepath);

    public override void Read(ExtendedBinaryReader reader)
    {
        for(int i = 0; i < Data.Length / 4; i++)
            Points.Add(reader.Read<int>());
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        foreach(var i in Points)
            writer.Write(i);
    }
}
