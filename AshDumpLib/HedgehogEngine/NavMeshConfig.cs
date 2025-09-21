using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine;

// NavMeshConfig is just https://recastnav.com/DetourNavMesh_8h_source.html - dtNavMesh

public class NavMeshConfig : IFile
{
    public const string FileExtension = ".nmc";
    public const string Signature = "CVAN";

    public int Version = 1;
    public Vector3 NavMeshSize = new(0, 0, 0);
    public Vector2 TileSize = new(0, 0);
    public int MaxTiles = 0;
    public int MaxPolys = 0;

    public NavMeshConfig() { }

    public NavMeshConfig(string filename) => Open(filename);
    public NavMeshConfig(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.ReadSignature(Signature);
        Version = reader.Read<int>();
        NavMeshSize = reader.Read<Vector3>();
        TileSize = reader.Read<Vector2>();
        MaxTiles = reader.Read<int>();
        MaxPolys = reader.Read<int>();
        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteSignature(Signature);
        writer.Write(Version);
        writer.Write(NavMeshSize);
        writer.Write(TileSize);
        writer.Write(MaxTiles);
        writer.Write(MaxPolys);
        writer.Dispose();
    }
}
