using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;

namespace AshDumpLib.HedgehogEngine.Mirage.Anim;

public class MaterialAnimation : IFile
{
    public const string FileExtension = ".mat-anim";

    public string MaterialName = "";
    public List<Material> Materials = new();

    public MaterialAnimation() { }

    public MaterialAnimation(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big));
    public override void WriteBuffer() => Write(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big));

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.genericOffset = 0x18;
        reader.Jump(0, SeekOrigin.Begin);
        var matsPointer = reader.Read<uint>();
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

        reader.Jump(matsPointer, SeekOrigin.Begin);
        MaterialName = reader.ReadStringTableEntry();
        var matsCount = reader.Read<int>();
        for (int i = 0; i < matsCount; i++)
        {
            var mat = new Material();
            mat.Read(reader, keyframes);
            Materials.Add(mat);
        }

        reader.Dispose();
    }

    public void Write(AnimWriter writer)
    {
        writer.AnimationType = AnimWriter.AnimType.MaterialAnimation;
        writer.WriteHeader();

        List<Keyframe> keys = new List<Keyframe>();

        writer.AddOffset("materials", false);
        long materialsSizePos = writer.Position;
        writer.WriteNulls(4);

        writer.AddOffset("keyframes", false);
        long keyframesSizePos = writer.Position;
        writer.WriteNulls(4);

        writer.AddOffset("strings", false);
        writer.WriteNulls(4);

        writer.SetOffset("materials");
        writer.WriteStringTableEntry(MaterialName);

        writer.Write(Materials.Count);

        foreach (var mat in Materials)
            writer.AddOffset(mat.Name + "ptr");

        foreach (var mat in Materials)
            mat.Write(writer, keys);

        int materialSize = (int)(writer.Position - writer.GetOffsetValue("materials")) - writer.GenericOffset;
        writer.WriteAt(materialSize, materialsSizePos);

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

public class Material
{
    public string Name = "";
    public float FPS = 30;
    public float FrameStart = 0;
    public float FrameEnd = 0;
    public List<MaterialFrameInfo> FrameInfos = new();

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
            var frameInfo = new MaterialFrameInfo();
            frameInfo.InputID = reader.Read<byte>();
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
            writer.Write((byte)frameInfo.InputID);
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

public struct MaterialFrameInfo
{
    public int InputID;
    public int Flag;
    public List<Keyframe> KeyFrames;
}

public enum MatType
{
    I1X = 0, I1Y, I1Z, I1W, I2X, I2Y, I2Z, I2W, I3X, I3Y, I3Z, I3W, I4X, I4Y, I4Z, I4W, I5X, I5Y, I5Z, I5W, I6X, I6Y, I6Z, I6W, I7X, I7Y, I7Z, I7W, I8X, I8Y, I8Z, I8W, I9X, I9Y, I9Z, I9W, I10X, I10Y, I10Z, I10W, I11X, I11Y, I11Z, I11W, I12X, I12Y, I12Z, I12W
}
