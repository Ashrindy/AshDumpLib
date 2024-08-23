using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA;

public class SkeletonPXD : IFile
{
    public const string FileExtension = ".skl.pxd";
    public const string BINASignature = "KSXP";

    public int Version = 512;
    public List<Bone> Bones = new();

    public SkeletonPXD() { }

    public SkeletonPXD(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() => Write(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();

        long parentIndexOffset = reader.Read<long>();
        long parentIndexCount = reader.Read<long>();
        reader.Skip(0x10);

        long boneNameTableOffset = reader.Read<long>();
        long boneNameTableCount = reader.Read<long>();
        reader.Skip(0x10);

        long boneMatrixTableOffset = reader.Read<long>();
        long boneMatrixTableCount = reader.Read<long>();
        reader.Skip(0x10);

        reader.Jump(parentIndexOffset, SeekOrigin.Begin);
        for(int i = 0; i < parentIndexCount; i++)
        {
            Bone bone = new Bone();
            bone.ParentIndex = reader.Read<ushort>();
            Bones.Add(bone);
        }

        reader.Jump(boneNameTableOffset, SeekOrigin.Begin);
        for(int i = 0; i < boneNameTableCount; i++)
        {
            Bones[i].Name = reader.ReadStringTableEntry();
            reader.Skip(0x8);
        }

        reader.Jump(boneMatrixTableOffset, SeekOrigin.Begin);
        for(int i = 0; i < boneMatrixTableCount; i++)
        {
            Bones[i].Position = reader.Read<Vector3>();
            reader.Skip(0x4);
            Bones[i].Rotation = Helpers.ToEulerAngles(reader.Read<Quaternion>());
            Bones[i].Scale = reader.Read<Vector3>();
            reader.Skip(0x4);
        }

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);

        writer.AddOffset("boneParents");
        writer.Write<long>(Bones.Count);

        writer.Write<long>(Bones.Count);
        writer.Write<long>(0);

        writer.AddOffset("boneNames");
        writer.Write<long>(Bones.Count);

        writer.Write<long>(Bones.Count);
        writer.Write<long>(0);

        writer.AddOffset("boneTransforms");
        writer.Write<long>(Bones.Count);

        writer.Write<long>(Bones.Count);
        writer.Write<long>(0);

        writer.SetOffset("boneParents");
        foreach (var bone in Bones)
            writer.Write((ushort)bone.ParentIndex);

        writer.SetOffset("boneNames");
        foreach(var bone in Bones)
        {
            writer.WriteStringTableEntry(bone.Name);
            writer.Write<long>(0);
        }

        writer.SetOffset("boneTransforms");
        foreach(var bone in Bones)
        {
            writer.Write(bone.Position);
            writer.Write(0);
            writer.Write(Helpers.ToQuaternion(bone.Rotation));
            writer.Write(bone.Scale);
            writer.Write(0);
        }

        writer.FinishWrite();
        writer.Dispose();
    }

    public class Bone
    {
        public int ParentIndex;
        public string Name;
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale;
    }
}