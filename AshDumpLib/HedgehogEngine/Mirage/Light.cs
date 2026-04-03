using Amicitia.IO.Binary;
using AshDumpLib.HedgehogEngine.Mirage.Anim;
using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.Mirage;

public class Light : IFile
{
    public const string FileExtension = ".light";

    public enum Type : uint
    {
        Sun,
        Point,
        Spot
    }

    public Type LightType = Type.Sun;
    public SunProperties SunProps = new();
    public PointProperties PointProps = new();
    public SpotProperties SpotProps = new();
    public int Version = 1;

    public struct SunProperties
    {
        public Vector3 Direction = new(0, 0, 0);
        public Vector3 Color = new(1, 1, 1);
        public SunProperties() { }
    }

    public struct PointProperties
    {
        public Vector3 Position = new(0, 0, 0);
        public Vector3 Color = new(1, 1, 1);
        public Vector4 Range = new(1, 1, 2, 2);
        public bool ShadowEnabled = true;
        public PointProperties() { }
    }

    public struct SpotProperties
    {
        public Vector3 Position = new(0, 0, 0);
        public Vector3 Rotation = new(0, 0, 0);
        public Vector3 Color = new(1, 1, 1);
        public float InnerConeAngle = 1;
        public float OuterConeAngle = 1;
        public float AttenuationRadius = 1;
        public bool ShadowEnabled = true;
        public SpotProperties() { }
    }

    public Light() { }

    public Light(string filename) => Open(filename);
    public Light(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big));
    public override void WriteBuffer() { MemoryStream memStream = new MemoryStream(); Write(new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big)); Data = memStream.ToArray(); }

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.Skip(4);
        Version = reader.Read<int>();
        reader.genericOffset = 0x18;
        reader.Jump(0, SeekOrigin.Begin);

        LightType = reader.Read<Type>();
        switch (LightType)
        {
            case Type.Sun:
                SunProps = reader.Read<SunProperties>();
                break;

            case Type.Point:
                {
                    if (Version == 1)
                    {
                        PointProps.Position = reader.Read<Vector3>();
                        PointProps.Color = reader.Read<Vector3>();
                        PointProps.ShadowEnabled = reader.Read<bool>();
                        reader.Align(4);
                        PointProps.Range = reader.Read<Vector4>();
                    }
                    else if (Version == 2)
                    {
                        PointProps.Position = reader.Read<Vector3>();
                        PointProps.Color = reader.Read<Vector3>();
                        reader.Skip(4);
                        PointProps.Range = reader.Read<Vector4>();
                        reader.Align(4);
                        reader.Skip(16);
                    }
                    break;
                }

            case Type.Spot:
                SpotProps = reader.Read<SpotProperties>();
                break;
        }

        reader.Dispose();
    }

    public void Write(AnimWriter writer)
    {
        writer.AnimationType = AnimWriter.AnimType.CameraAnimation;
        writer.HasStringTable = false;
        writer.Version = Version;
        writer.AddUnknownOffset = false;
        writer.WriteHeader();

        writer.Write(LightType);
        switch (LightType)
        {
            case Type.Sun:
                writer.Write(SunProps);
                break;

            case Type.Point:
                if (Version == 1)
                {
                    writer.Write(PointProps.Position);
                    writer.Write(PointProps.Color);
                    writer.Write(PointProps.ShadowEnabled);
                    writer.Align(4);
                    writer.Write(PointProps.Range);
                }
                else if (Version == 2)
                {
                    writer.Write(PointProps.Position);
                    writer.Write(PointProps.Color);
                    writer.WriteNulls(4);
                    writer.Write(PointProps.Range);
                    writer.Write(PointProps.ShadowEnabled);
                    writer.Align(4);
                    writer.WriteNulls(16);
                }
                break;

            case Type.Spot:
                writer.Write(SpotProps);
                break;
        }

        writer.FinishWrite();

        writer.Dispose();
    }
}
