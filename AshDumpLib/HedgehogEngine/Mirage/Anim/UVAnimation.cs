using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;

namespace AshDumpLib.HedgehogEngine.Mirage.Anim;

//Research by Kwasior!

public class UVAnimation : IFile
{
    public const string FileExtension = ".uv-anim";

    public string MaterialName = "";
    public string TextureName = "";
    public List<UV> UVs = new();


    public UVAnimation() { }

    public UVAnimation(string filename) => Open(filename);
    public UVAnimation(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big));
    public override void WriteBuffer() { MemoryStream memStream = new MemoryStream(); Write(new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big)); Data = memStream.ToArray(); }

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.genericOffset = 0x18;
        reader.Jump(0, SeekOrigin.Begin);
        var uvsPointer = reader.Read<uint>();
        reader.Skip(4);
        var keyframesPointer = reader.Read<int>();
        var keyframesSize = reader.Read<int>();
        var stringPointer = reader.Read<int>();
        reader.stringTableOffset = stringPointer;

        List<Keyframe> keyframes = new List<Keyframe>();
        reader.Jump(keyframesPointer, SeekOrigin.Begin);
        for (int i = 0; i < keyframesSize / 8; i++)
        {
            var keyframe = new Keyframe();
            keyframe.Frame = reader.Read<float>();
            keyframe.Value = reader.Read<float>();
            keyframes.Add(keyframe);
        }

        reader.Jump(uvsPointer, SeekOrigin.Begin);
        MaterialName = reader.ReadStringTableEntry(dontCheckForZeroes: true);
        TextureName = reader.ReadStringTableEntry(dontCheckForZeroes: true);
        var uvCount = reader.Read<int>();
        for (int i = 0; i < uvCount; i++)
        {
            var uv = new UV();
            uv.Read(reader, keyframes);
            UVs.Add(uv);
        }

        reader.Dispose();
    }

    public void Write(AnimWriter writer)
    {
        writer.AnimationType = AnimWriter.AnimType.UVAnimation;
        writer.Version = 3;
        writer.WriteHeader();

        List<Keyframe> keys = new List<Keyframe>();

        writer.AddOffset("uvs", false);
        long uvsSizePos = writer.Position;
        writer.WriteNulls(4);

        writer.AddOffset("keyframes", false);
        long keyframesSizePos = writer.Position;
        writer.WriteNulls(4);

        writer.AddOffset("strings", false);
        writer.WriteNulls(4);

        writer.SetOffset("uvs");
        writer.WriteStringTableEntry(MaterialName);
        writer.WriteStringTableEntry(TextureName);

        writer.Write(UVs.Count);

        foreach (var uv in UVs)
            writer.AddOffset(uv.Name + "ptr");

        foreach (var uv in UVs)
            uv.Write(writer, keys);

        int uvsSize = (int)(writer.Position - writer.GetOffsetValue("uvs")) - writer.GenericOffset;
        writer.WriteAt(uvsSize, uvsSizePos);

        writer.SetOffset("keyframes");

        foreach (var key in keys)
        {
            writer.Write(key.Frame);
            writer.Write(key.Value);
        }

        int keyframesSize = (int)(writer.Position - writer.GetOffsetValue("keyframes")) - writer.GenericOffset;
        writer.WriteAt(keyframesSize, keyframesSizePos);

        writer.FinishWrite();

        writer.Dispose();
    }
}

public class UV
{
    public string Name = "";
    public float FPS = 30;
    public float FrameStart = 0;
    public float FrameEnd = 0;
    public List<UVFrameInfo> FrameInfos = new();

    public void Read(ExtendedBinaryReader reader, List<Keyframe> keyframes)
    {
        var pointer = reader.Read<uint>();
        var prePos = reader.Position;
        reader.Jump(pointer, SeekOrigin.Begin);
        Name = reader.ReadStringTableEntry(dontCheckForZeroes: true);
        FPS = reader.Read<float>();
        FrameStart = reader.Read<float>();
        FrameEnd = reader.Read<float>();
        var FrameInfoCount = reader.Read<int>();
        for (int i = 0; i < FrameInfoCount; i++)
        {
            var frameInfo = new UVFrameInfo();
            UVFrameType type = (UVFrameType)reader.Read<byte>();
            frameInfo.Type = type;
            frameInfo.Flag = reader.Read<byte>();
            frameInfo.Interpolation = reader.Read<byte>();
            frameInfo.Flag2 = reader.Read<byte>();
            frameInfo.KeyFrames = new List<Keyframe>();
            var length = reader.Read<int>();
            var indexStart = reader.Read<int>();
            for (int j = indexStart; j < length + indexStart; j++)
            {
                frameInfo.KeyFrames.Add(keyframes[j]);
            }
            FrameInfos.Add(frameInfo);
        }
        reader.Seek(prePos, SeekOrigin.Begin);
    }

    public void Write(ExtendedBinaryWriter writer, List<Keyframe> keyframes)
    {
        writer.SetOffset(Name + "ptr");
        writer.WriteStringTableEntry(Name);
        writer.Write(FPS);
        writer.Write(FrameStart);
        writer.Write(FrameEnd);
        writer.Write(FrameInfos.Count);

        foreach (var frameInfo in FrameInfos)
        {
            writer.Write((byte)frameInfo.Type);
            writer.Write((byte)frameInfo.Flag);
            writer.Write((byte)frameInfo.Interpolation);
            writer.Write((byte)frameInfo.Flag2);
            writer.Write(frameInfo.KeyFrames.Count);
            writer.Write(keyframes.Count);

            foreach (var keyFrame in frameInfo.KeyFrames)
            {
                keyframes.Add(keyFrame);
            }
        }
    }
}

public struct UVFrameInfo
{
    public UVFrameType Type;
    public int Flag;
    public int Flag2;
    public int Interpolation;
    public List<Keyframe> KeyFrames;
}

public enum UVFrameType
{
    PositionX = 0,
    PositionY = 1,
    Rotation = 2,
    ScaleX = 3,
    ScaleY = 4
}
