using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.CastleSiege;

public class Animation : IFile
{
    public const string FileExtension = ".rdm";

    public int Version = 1002;
    public int AnimationRange = 0;
    public int TPS = 0;
    public int FPS = 0;
    public List<Bone> Bones = new();
    public List<Object> Objects = new();
    public Label EndLabel = new();

    public Animation() { }
    public Animation(string filename) => Open(filename);
    public Animation(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        Version = reader.Read<int>();
        FileName = Helpers.CastleStrikeString(reader) + ".rdm";
        FilePath = Helpers.CastleStrikeString(reader);
        AnimationRange = reader.Read<int>();
        TPS = reader.Read<int>();
        FPS = reader.Read<int>();
        float unk0 = reader.Read<int>();
        // 0, 1 and 1 as int32's
        reader.Skip(12);
        int numBones = reader.Read<int>();
        int numObjects = reader.Read<int>();
        int numMorphObjects = reader.Read<int>();
        // 20 and 1 as int32's
        reader.Skip(8);
        string maxFilepath = Helpers.CastleStrikeString(reader);

        for (int i = 0; i < numBones; i++)
        {
            Bone bone = new();
            bone.Read(reader);
            Bones.Add(bone);
        }
        foreach (var i in Bones)
            i.ReadData(reader);

        for (int i = 0; i < numObjects; i++)
        {
            Object obj = new();
            obj.Read(reader);
            Objects.Add(obj);
        }
        foreach (var i in Objects)
            i.ReadData(reader);

        EndLabel.Read(reader);

        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.Write(Version);
        writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, FileName, 256);
        writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, FilePath, 256);

        writer.Write(AnimationRange);
        writer.Write(TPS);
        writer.Write(FPS);
        writer.Write(1f);

        writer.Write(0);
        writer.Write(1);
        writer.Write(1);

        writer.Write(Bones.Count);
        writer.Write(Objects.Count);
        writer.Write(0);

        writer.Write(20);
        writer.Write(1);

        writer.WriteNulls(256);

        foreach (var i in Bones)
            i.Write(writer);
        foreach (var i in Bones)
            i.WriteData(writer);

        foreach (var i in Objects)
            i.Write(writer);
        foreach (var i in Objects)
            i.WriteData(writer);

        EndLabel.Write(writer);

        writer.Dispose();
    }


    public class Bone
    {
        public string Name = "";
        public Vector3 Position = new(0, 0, 0);
        public List<int> ChildIndices = new();
        public List<PositionKey> PositionKeys = new();
        public List<RotationKey> RotationKeys = new();

        int numChildren = 0;
        int numPosKey = 0;
        int numRotKey = 0;
        int numLinks = 0; //not sure, going off of the .txt files

        public Bone() { }

        public void Read(ExtendedBinaryReader reader)
        {
            Name = Helpers.CastleStrikeString(reader);
            Position = reader.Read<Vector3>();
            numChildren = reader.Read<int>();
            reader.Skip(4);
            numPosKey = reader.Read<int>();
            numRotKey = reader.Read<int>();
            reader.Skip(8);
            numLinks = reader.Read<int>();
            reader.Skip(4);
        }

        public void ReadData(ExtendedBinaryReader reader)
        {
            for(int i = 0; i < numChildren; i++)
                ChildIndices.Add(reader.Read<int>());
            for (int i = 0; i < numPosKey; i++)
                PositionKeys.Add(reader.Read<PositionKey>());
            for (int i = 0; i < numRotKey; i++)
                RotationKeys.Add(reader.Read<RotationKey>());
        }


        public void Write(ExtendedBinaryWriter writer)
        {
            writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, Name, 256);
            writer.Write(Position);
            writer.Write(ChildIndices.Count);
            writer.WriteNulls(4);
            writer.Write(PositionKeys.Count);
            writer.Write(RotationKeys.Count);
            writer.WriteNulls(8);
            writer.Write(0);
            writer.WriteNulls(4);
        }

        public void WriteData(ExtendedBinaryWriter writer)
        {
            foreach (var i in ChildIndices)
                writer.Write(i);
            foreach (var i in PositionKeys)
                writer.Write(i);
            foreach (var i in RotationKeys)
                writer.Write(i);
        }
    }

    public class Object
    {
        public string Name = "";
        public Vector3 Pivot = new(0, 0, 0);
        public Vector3 Position = new(0, 0, 0);
        public Quaternion Rotation = new(0, 0, 0, 1);
        public Vector3 Scale = new(1, 1, 1);
        public List<PositionKey> PositionKeys = new();
        public List<RotationKey> RotationKeys = new();
        public List<ScaleKey> ScaleKeys = new();
        public KeyType PositionType = KeyType.Linear;
        public KeyType RotationType = KeyType.Linear;
        public KeyType ScaleType = KeyType.Linear;

        int numPosKey = 0;
        int numRotKey = 0;
        int numScaKey = 0;

        public void Read(ExtendedBinaryReader reader)
        {
            Name = Helpers.CastleStrikeString(reader);
            Pivot = reader.Read<Vector3>();
            Position = reader.Read<Vector3>();
            Rotation = reader.Read<Quaternion>();
            Scale = reader.Read<Vector3>();
            numPosKey = reader.Read<int>();
            numRotKey = reader.Read<int>();
            numScaKey = reader.Read<int>();
            PositionType = reader.Read<KeyType>();
            RotationType = reader.Read<KeyType>();
            ScaleType = reader.Read<KeyType>();
            reader.Skip(36);
        }

        public void ReadData(ExtendedBinaryReader reader)
        {
            for (int i = 0; i < numPosKey; i++)
                PositionKeys.Add(reader.Read<PositionKey>());
            for (int i = 0; i < numRotKey; i++)
                RotationKeys.Add(reader.Read<RotationKey>());
            for (int i = 0; i < numScaKey; i++)
                ScaleKeys.Add(reader.Read<ScaleKey>());
        }


        public void Write(ExtendedBinaryWriter writer)
        {
            writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, Name, 256);
            writer.Write(Pivot);
            writer.Write(Position);
            writer.Write(Rotation);
            writer.Write(Scale);
            writer.Write(PositionKeys.Count);
            writer.Write(RotationKeys.Count);
            writer.Write(ScaleKeys.Count);
            writer.Write(PositionType);
            writer.Write(RotationType);
            writer.Write(ScaleType);
            writer.WriteNulls(36);
        }

        public void WriteData(ExtendedBinaryWriter writer)
        {
            foreach (var i in PositionKeys)
                writer.Write(i);
            foreach (var i in RotationKeys) 
                writer.Write(i);
            foreach (var i in ScaleKeys)
                writer.Write(i);
        }
    }

    public class Label
    {
        public int Version = 1;
        public string Name = "EndLabel";
        public int unk0 = 0;
        public int unk1 = 0;

        public void Read(ExtendedBinaryReader reader)
        {
            Version = reader.Read<int>();
            Name = Helpers.CastleStrikeString(reader);
            unk0 = reader.Read<int>();
            unk1 = reader.Read<int>();
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            writer.Write(Version);
            writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, Name, 256);
            writer.Write(unk0);
            writer.Write(unk1);
        }
    }


    public struct PositionKey
    {
        public int Keyframe;
        public Vector3 Value;
    }

    public struct RotationKey
    {
        public int Keyframe;
        public Quaternion Value;
    }

    public struct ScaleKey
    {
        public int Keyframe;
        public Vector3 Value;
        public Quaternion Value1;
    }

    public enum KeyType : int
    {
        Linear = 1
    }
}
