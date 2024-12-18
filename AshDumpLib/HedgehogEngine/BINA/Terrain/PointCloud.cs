﻿using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA.Terrain;

public class PointCloud : IFile
{
    public const string FileExtension = ".pcmodel, .pcrt, .pccol";
    public const string BINASignature = "CPIC";

    public int Version = 2;
    public List<Point> Points = new();

    public PointCloud() { }

    public PointCloud(string filename) => Open(filename);
    public PointCloud(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);

        Version = reader.Read<int>();
        Points = reader.ReadBINAArrayStruct64<Point>();

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);

        writer.Write(Version);
        writer.WriteBINAArray64(Points, "points");

        writer.FinishWrite();
        writer.Dispose();
    }

    public class Point : IBINASerializable
    {
        public string InstanceName = "";
        public string ResourceName = "";
        public Vector3 Position = new(0, 0, 0);
        public Vector3 Rotation = new(0, 0, 0);
        public Vector3 Scale = new(1, 1, 1);

        public void Read(BINAReader reader)
        {
            InstanceName = reader.ReadStringTableEntry();
            ResourceName = reader.ReadStringTableEntry();
            Position = reader.Read<Vector3>();
            Rotation = reader.Read<Vector3>();
            reader.Skip(4);
            Scale = reader.Read<Vector3>();
            reader.Skip(8);
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(InstanceName);
            writer.WriteStringTableEntry(ResourceName);
            writer.Write(Position);
            writer.Write(Rotation);
            writer.Write(1);
            writer.Write(Scale);
            writer.WriteNulls(8);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }
    }
}