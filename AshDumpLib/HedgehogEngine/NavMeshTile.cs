using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine;

public class NavMeshTile : IFile
{
    public const string FileExtension = ".nmt";
    public const string Signature = "MVAN";

    public int Version = 1;
    public List<Vector3> Points = new();

    public NavMeshTile() { }

    public NavMeshTile(string filename) => Open(filename);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.ReadSignature(Signature);
        Version = reader.Read<int>();
        reader.Seek(0x34, SeekOrigin.Begin);
        int count = reader.Read<int>();
        reader.Seek(0x7C, SeekOrigin.Begin);
        for(int i = 0; i < count; i++)
            Points.Add(reader.Read<Vector3>());
        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteSignature(Signature);
        writer.Write(Version);
        
        writer.Dispose();
    }
}
