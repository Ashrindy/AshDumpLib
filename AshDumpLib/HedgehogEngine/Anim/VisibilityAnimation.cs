using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine.Anim;

public class VisibilityAnimation
{
    public const string FileExtension = ".vis-anim";

    public string SkeletonName = "";
    public string InputName = "";
    public List<Visibility> Visibilities = new();


    public VisibilityAnimation() { }

    public VisibilityAnimation(string filename)
    {
        Open(filename);
    }

    public void Open(string filename)
    {
        Read(new(filename, Endianness.Big, System.Text.Encoding.UTF8));
    }

    public void Save(string filename)
    {
        Write(new(filename, Endianness.Big, System.Text.Encoding.UTF8));
    }

    public void Read(ExtendedBinaryReader reader)
    {
        reader.genericOffset = 24;
        reader.Jump(0, SeekOrigin.Begin);
        var vissPointer = reader.Read<uint>();
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

        reader.Jump(vissPointer, SeekOrigin.Begin);
        SkeletonName = reader.ReadStringTableEntry();
        InputName = reader.ReadStringTableEntry();
        var visCount = reader.Read<int>();
        for (int i = 0; i < visCount; i++)
        {
            var vis = new Visibility();
            vis.Read(reader, keyframes);
            Visibilities.Add(vis);
        }

        reader.Dispose();
    }

    public void Write(AnimWriter writer)
    {
        writer.AnimationType = AnimWriter.AnimType.VisibilityAnimation;
        writer.Version = 1;
        writer.WriteHeader();

        List<Keyframe> keys = new List<Keyframe>();

        writer.AddOffset("visibilities", false);
        long visibilitySizePos = writer.Position;
        writer.WriteNulls(4);

        writer.AddOffset("keyframes", false);
        long keyframesSizePos = writer.Position;
        writer.WriteNulls(4);

        writer.AddOffset("strings", false);
        writer.WriteNulls(4);

        writer.SetOffset("visibilities");

        writer.WriteStringTableEntry(SkeletonName);
        writer.WriteStringTableEntry(InputName);
        writer.Write(Visibilities.Count);

        foreach (var vis in Visibilities)
            writer.AddOffset(vis.Name + "ptr");

        foreach (var vis in Visibilities)
            vis.Write(writer, keys);

        int visibilitySize = (int)(writer.Position - writer.GetOffsetValue("visibilities")) - writer.GenericOffset;
        writer.WriteAt(visibilitySize, visibilitySizePos);

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

public class Visibility
{
    public string Name = "";
    public float FPS = 30;
    public float FrameStart = 0;
    public float FrameEnd = 0;
    public List<VisibilityFrameInfo> FrameInfos = new();

    public void Read(ExtendedBinaryReader reader, List<Keyframe> keyframes)
    {
        var pointer = reader.Read<uint>();
        var prePos = reader.Position;
        reader.Jump(pointer, SeekOrigin.Begin);
        Name = reader.ReadStringTableEntry();
        FPS = reader.Read<float>();
        FrameStart = reader.Read<float>();
        FrameEnd = reader.Read<float>();
        var FrameInfoCount = reader.Read<int>();
        for (int i = 0; i < FrameInfoCount; i++)
        {
            var frameInfo = new VisibilityFrameInfo();
            frameInfo.Type = reader.Read<byte>();
            frameInfo.Flag = reader.Read<byte>();
            reader.Skip(2);
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

    public void Write(AnimWriter writer, List<Keyframe> keyframes)
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
            writer.Skip(2);
            writer.Write(frameInfo.KeyFrames.Count);
            writer.Write(keyframes.Count);

            foreach (var keyFrame in frameInfo.KeyFrames)
            {
                keyframes.Add(keyFrame);
            }
        }
    }
}

public struct VisibilityFrameInfo
{
    public int Type;
    public int Flag;
    public List<Keyframe> KeyFrames;
}