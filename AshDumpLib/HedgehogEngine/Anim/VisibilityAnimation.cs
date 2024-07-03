using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine.Anim;

public class VisibilityAnimation : IBinarySerializable
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

    public void Read(BinaryObjectReader reader)
    {
        uint StringTableOffset = 0;
        reader.Seek(0x18, SeekOrigin.Begin);
        var vissPointer = reader.Read<uint>();
        reader.Skip(4);
        var keyframesPointer = reader.Read<uint>();
        var keyframesSize = reader.Read<uint>();
        var stringPointer = reader.Read<uint>();
        StringTableOffset = stringPointer + 0x18;

        List<Keyframe> keyframes = new List<Keyframe>();
        reader.Seek(0x18 + keyframesPointer, SeekOrigin.Begin);
        for (int i = 0; i < keyframesSize / 8; i++)
        {
            var keyframe = new Keyframe();
            keyframe.Frame = reader.Read<float>();
            keyframe.Value = reader.Read<float>();
            keyframes.Add(keyframe);
        }

        reader.Seek(0x18 + vissPointer, SeekOrigin.Begin);
        SkeletonName = Helpers.ReadStringTableEntry(reader, (int)StringTableOffset);
        InputName = Helpers.ReadStringTableEntry(reader, (int)StringTableOffset);
        var visCount = reader.Read<int>();
        for (int i = 0; i < visCount; i++)
        {
            var vis = new Visibility();
            vis.Read(reader, StringTableOffset, keyframes);
            Visibilities.Add(vis);
        }

        reader.Dispose();
    }

    public void Write(BinaryObjectWriter writer)
    {
        int fileSize = 0;
        int dataSize = 0;
        int offsetPointer = 0;
        List<Keyframe> keys = new List<Keyframe>();

        List<int> offsets = new List<int>
        {
            0,
            4,
            8,
            12,
            16,
            20
        };

        List<char> strings = new List<char>();

        writer.Write(fileSize);
        writer.Write(3);
        writer.Write(dataSize);
        writer.Write(24);
        writer.Write(offsetPointer);
        writer.Write(0);

        writer.Write(24);
        int animVisSize;
        writer.Seek(0x30, SeekOrigin.Begin);
        int stringOffset = 0;
        foreach (var str in strings)
        {
            stringOffset++;
        }
        writer.Write(stringOffset);
        foreach (var c in SkeletonName)
        {
            strings.Add(c);
        }
        strings.Add('\0');
        stringOffset = 0;
        foreach (var str in strings)
        {
            stringOffset++;
        }
        writer.Write(stringOffset);
        foreach (var c in InputName)
        {
            strings.Add(c);
        }
        strings.Add('\0');
        writer.Write(Visibilities.Count);
        writer.Skip(4 * Visibilities.Count);
        List<long> visPtrs = new List<long>();

        foreach (var vis in Visibilities)
        {
            visPtrs.Add(writer.Position - 0x18);
            vis.Write(writer, strings, keys);
        }

        animVisSize = (int)writer.Position - 0x30;

        writer.Seek(0x3c, SeekOrigin.Begin);

        foreach (var ptr in visPtrs)
        {
            offsets.Add((int)writer.Position - 0x18);
            writer.Write((int)ptr);
        }

        writer.Seek(0x1c, SeekOrigin.Begin);
        writer.Write(animVisSize);

        writer.Write(animVisSize + 0x30 - 0x18);
        writer.Write(keys.Count * 8);

        writer.Seek(animVisSize + 0x30, SeekOrigin.Begin);
        foreach (var key in keys)
        {
            writer.Write(key.Frame);
            writer.Write(key.Value);
        }

        int stringPtr = (int)writer.Position - 0x18;

        writer.Seek(0x28, SeekOrigin.Begin);

        writer.Write(stringPtr);

        int stringSize = 0;
        foreach (var str in strings)
        {
            stringSize++;
        }

        while (stringSize % 4 != 0)
        {
            stringSize++;
        }

        writer.Write(stringSize);

        writer.Seek(stringPtr + 0x18, SeekOrigin.Begin);
        writer.WriteString(StringBinaryFormat.FixedLength, new string(strings.ToArray()), stringSize);

        dataSize = (int)writer.Position - 0x18;

        writer.Write(offsets.Count);
        foreach (var offset in offsets)
        {
            writer.Write(offset);
        }

        fileSize = (int)writer.Position;

        writer.Seek(0, SeekOrigin.Begin);
        writer.Write(fileSize);

        writer.Seek(0x8, SeekOrigin.Begin);
        writer.Write(dataSize);

        writer.Seek(0x10, SeekOrigin.Begin);
        writer.Write(dataSize + 0x18);


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

    public void Read(BinaryObjectReader reader, uint StringTableOffset, List<Keyframe> keyframes)
    {
        var pointer = reader.Read<uint>();
        var prePos = reader.Position;
        reader.Seek(pointer + 0x18, SeekOrigin.Begin);
        Name = Helpers.ReadStringTableEntry(reader, (int)StringTableOffset);
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

    public void Write(BinaryObjectWriter writer, List<char> strings, List<Keyframe> keyframes)
    {
        int stringOffset = 0;
        foreach (var str in strings)
        {
            stringOffset++;
        }
        writer.Write(stringOffset);
        foreach (var c in Name)
        {
            strings.Add(c);
        }
        strings.Add('\0');
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