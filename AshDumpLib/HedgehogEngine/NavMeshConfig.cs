﻿using Amicitia.IO.Binary;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine;

public class NavMeshConfig
{
    public const string FileExtension = ".nmc";
    public const string Signature = "CVAN";

    public int Version = 1;
    public Vector3 NavMeshSize = new(0, 0, 0);
    public Vector2 Unk1 = new(0, 0);
    public int NavMeshTileCount = 0;

    public NavMeshConfig() { }

    public NavMeshConfig(string filename)
    {
        Open(filename);
    }

    public void Open(string filename)
    {
        Read(new(filename, Endianness.Little, System.Text.Encoding.UTF8));
    }

    public void Save(string filename)
    {
        Write(new(filename, Endianness.Little, System.Text.Encoding.UTF8));
    }

    public void Read(ExtendedBinaryReader reader)
    {
        reader.ReadSignature(Signature);
        Version = reader.Read<int>();
        NavMeshSize = reader.Read<Vector3>();
        Unk1 = reader.Read<Vector2>();
        NavMeshTileCount = (int)reader.Read<long>();
        reader.Dispose();
    }

    public void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteSignature(Signature);
        writer.Write(Version);
        writer.Write(NavMeshSize);
        writer.Write(Unk1);
        writer.Write((long)NavMeshTileCount);
        writer.Dispose();
    }
}
