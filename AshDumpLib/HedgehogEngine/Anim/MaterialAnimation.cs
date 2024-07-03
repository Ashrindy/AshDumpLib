using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine.Anim;

public class MaterialAnimation : IBinarySerializable
{
    public const string FileExtension = ".mat-anim";

    public string MaterialName = "";
    public List<Material> Materials = new();


    public MaterialAnimation() { }

    public MaterialAnimation(string filename)
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
        var matsPointer = reader.Read<uint>();
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

        reader.Seek(0x18 + matsPointer, SeekOrigin.Begin);
        MaterialName = Helpers.ReadStringTableEntry(reader, (int)StringTableOffset);
        var matsCount = reader.Read<int>();
        for (int i = 0; i < matsCount; i++)
        {
            var mat = new Material();
            mat.Read(reader, StringTableOffset, keyframes);
            Materials.Add(mat);
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
        writer.Write(2);
        writer.Write(dataSize);
        writer.Write(24);
        writer.Write(offsetPointer);
        writer.Write(0);

        writer.Write(24);
        int animMatSize;
        writer.Seek(0x30, SeekOrigin.Begin);
        int stringOffset = 0;
        foreach (var str in strings)
        {
            stringOffset++;
        }
        writer.Write(stringOffset);
        foreach (var c in MaterialName)
        {
            strings.Add(c);
        }
        strings.Add('\0');
        writer.Write(Materials.Count);
        writer.Skip(4 * Materials.Count);
        List<long> matPtrs = new List<long>();

        foreach (var mat in Materials)
        {
            matPtrs.Add(writer.Position - 0x18);
            mat.Write(writer, strings, keys);
        }

        animMatSize = (int)writer.Position - 0x30;

        writer.Seek(0x38, SeekOrigin.Begin);

        foreach (var ptr in matPtrs)
        {
            offsets.Add((int)writer.Position - 0x18);
            writer.Write((int)ptr);
        }

        writer.Seek(0x1c, SeekOrigin.Begin);
        writer.Write(animMatSize);

        writer.Write(animMatSize + 0x30 - 0x18);
        writer.Write(keys.Count * 8);

        writer.Seek(animMatSize + 0x30, SeekOrigin.Begin);
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

public class Material
{
    public string Name = "";
    public float FPS = 30;
    public float FrameStart = 0;
    public float FrameEnd = 0;
    public List<MaterialFrameInfo> FrameInfos = new();

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
