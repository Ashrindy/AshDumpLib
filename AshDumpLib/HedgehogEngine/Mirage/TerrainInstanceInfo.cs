using Amicitia.IO.Binary;
using AshDumpLib.HedgehogEngine.Mirage.Anim;
using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.Mirage;

public class TerrainInstanceInfo : IFile
{
    public const string FileExtension = ".terrain-instanceinfo";

    public string Name = "";
    public string ResourceName = "";
    public Vector3 Position = new(0, 0, 0);
    public Vector3 Rotation = new(0, 0, 0);
    public Vector3 Scale = new(1, 1, 1);

    public TerrainInstanceInfo() { }

    public TerrainInstanceInfo(string filename) => Open(filename);
    public TerrainInstanceInfo(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big));
    public override void WriteBuffer() { MemoryStream memStream = new MemoryStream(); Write(new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big)); Data = memStream.ToArray(); }

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.genericOffset = 0x18;
        reader.Jump(0, SeekOrigin.Begin);

        reader.ReadAtOffset(reader.Read<int>() + 0x18, () => Name = reader.ReadString(StringBinaryFormat.NullTerminated));
        reader.ReadAtOffset(reader.Read<int>() + 0x18, () =>
        {
            Matrix4x4 mat = reader.Read<Matrix4x4>();
            mat = Matrix4x4.Transpose(mat);
            Quaternion quat;
            Matrix4x4.Decompose(mat, out Scale, out quat, out Position);
            Rotation = Helpers.ToEulerAngles(quat);
        });
        reader.ReadAtOffset(reader.Read<int>() + 0x18, () => ResourceName = reader.ReadString(StringBinaryFormat.NullTerminated));

        reader.Dispose();
    }

    public void Write(AnimWriter writer)
    {
        writer.WriteNulls(0x0C); //Skip the file size, node ver and node size for now
        writer.Write(0x18); //Hacky, but it's always the same eitherway (meant to be the node offset)
        writer.WriteNulls(0x04); //Skips the offset offset
        writer.WriteNulls(0x04);

        writer.AddOffset("name");
        writer.AddOffset("matrix");
        writer.AddOffset("resName");

        writer.SetOffset("name");
        writer.WriteString(StringBinaryFormat.NullTerminated, Name);
        writer.Align(4);

        writer.SetOffset("matrix");
        Quaternion quat = Helpers.ToQuaternion(Rotation);
        Matrix4x4 mat = Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(quat) * Matrix4x4.CreateTranslation(Position);
        mat = Matrix4x4.Transpose(mat);
        writer.Write(mat);
        writer.Align(4);

        writer.SetOffset("resName");
        writer.WriteString(StringBinaryFormat.NullTerminated, ResourceName);
        writer.Align(4);

        long nodeSize = writer.Position - 0x18;

        long offsetsPos = writer.Position;
        var offsets = writer.Offsets.Where(x => writer.OffsetsWrite[x.Key]);
        writer.Write(offsets.Count());
        foreach (var i in offsets)
            writer.Write((int)i.Value - 0x18);

        long filesize = writer.Position;

        writer.Seek(0, SeekOrigin.Begin);
        writer.Write((int)filesize);
        writer.Skip(0x04);
        writer.Write((int)nodeSize);
        writer.Skip(0x04);
        writer.Write((int)offsetsPos);

        writer.Dispose();
    }
}
