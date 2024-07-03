using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AshDumpLib.HedgehogEngine.Anim;
public class CameraAnimation : IBinarySerializable
{
    public List<Camera> Cameras { get; set; } = new();

    public CameraAnimation()
    {
    }

    public CameraAnimation(string filename)
    {
        Read(new(filename, Endianness.Big, System.Text.Encoding.UTF8));
    }

    public void Save(string filename)
    {
        Write(new(filename, Endianness.Big, System.Text.Encoding.Default));
    }

    public void Read(BinaryObjectReader reader)
    {
        uint StringTableOffset = 0;
        reader.Seek(0x18, SeekOrigin.Begin);
        var camerasPointer = reader.Read<uint>();
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

        reader.Seek(0x18 + camerasPointer, SeekOrigin.Begin);
        var cameraCount = reader.Read<int>();
        for (int i = 0; i < cameraCount; i++)
        {
            var camera = new Camera();
            camera.Read(reader, StringTableOffset, keyframes);
            Cameras.Add(camera);
        }
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
        int animCameraSize;
        writer.Seek(0x30, SeekOrigin.Begin);
        writer.Write(Cameras.Count);
        writer.Skip(4 * Cameras.Count);
        List<long> camPtrs = new List<long>();

        foreach (var camera in Cameras)
        {
            camPtrs.Add(writer.Position - 0x18);
            camera.Write(writer, strings, keys);
        }

        animCameraSize = (int)writer.Position - 0x30;

        writer.Seek(0x34, SeekOrigin.Begin);

        foreach (var ptr in camPtrs)
        {
            offsets.Add((int)writer.Position - 0x18 - 4);
            writer.Write((int)ptr);
        }

        writer.Seek(0x1c, SeekOrigin.Begin);
        writer.Write(animCameraSize);

        writer.Write(animCameraSize + 0x30 - 0x18);
        writer.Write(keys.Count * 8);

        writer.Seek(animCameraSize + 0x30, SeekOrigin.Begin);
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

public class Vector3Json
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    public Vector3Json()
    {
    }

    public Vector3Json(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

public class Camera
{
    public string Name { get; set; } = "";
    public bool RotationOrAim { get; set; } = true;
    public float FPS { get; set; } = 30;
    public float FrameStart { get; set; } = 0;
    public float FrameEnd { get; set; } = 0;
    public Vector3Json Position { get; set; } = new(0, 0, 0);
    public Vector3Json Rotation { get; set; } = new(0, 0, 0);
    public Vector3Json AimPosition { get; set; } = new(0, 0, 0);
    public float Twist { get; set; } = 0;
    public float ZNear { get; set; } = 0.001f;
    public float ZFar { get; set; } = 1000f;
    public float FOV { get; set; } = 0;
    public float AspectRatio { get; set; } = 1.77779f;
    public List<CamFrameInfo> FrameInfos { get; set; } = new();

    public void Read(BinaryObjectReader reader, uint StringTableOffset, List<Keyframe> keyframes)
    {
        var pointer = reader.Read<uint>();
        var prePos = reader.Position;
        reader.Seek(pointer + 0x18, SeekOrigin.Begin);
        Name = Helpers.ReadNullTerminatedStringTableEntry(reader, extraOffset: StringTableOffset);
        RotationOrAim = reader.Read<byte>() == 1;
        reader.Skip(3);
        FPS = reader.Read<float>();
        FrameStart = reader.Read<float>();
        FrameEnd = reader.Read<float>();
        var FrameInfoCount = reader.Read<int>();
        Vector3Json posTemp = new Vector3Json();
        posTemp.X = reader.Read<float>();
        posTemp.Z = reader.Read<float>();
        posTemp.Y = reader.Read<float>();
        Position = posTemp;
        Vector3Json rotTemp = new Vector3Json();
        rotTemp.X = reader.Read<float>();
        rotTemp.Z = reader.Read<float>();
        rotTemp.Y = reader.Read<float>();
        Rotation = rotTemp;
        Vector3Json aimPosTemp = new Vector3Json();
        aimPosTemp.X = reader.Read<float>();
        aimPosTemp.Z = reader.Read<float>();
        aimPosTemp.Y = reader.Read<float>();
        AimPosition = aimPosTemp;
        Twist = reader.Read<float>();
        ZNear = reader.Read<float>();
        ZFar = reader.Read<float>();
        FOV = reader.Read<float>();
        AspectRatio = reader.Read<float>();
        FOV = (float)(2 * Math.Atan(Math.Tan(FOV / 2) * AspectRatio));
        for (int i = 0; i < FrameInfoCount; i++)
        {
            var frameInfo = new CamFrameInfo();
            CamFrameType type = (CamFrameType)reader.Read<byte>();
            frameInfo.Type = type;
            reader.Skip(3);
            frameInfo.KeyFrames = new List<Keyframe>();
            var length = reader.Read<int>();
            var indexStart = reader.Read<int>();
            for (int j = indexStart; j < length + indexStart; j++)
            {
                var keyframe = keyframes[j];
                if (type == CamFrameType.FOV)
                {
                    keyframe.Value = (float)(2 * Math.Atan(Math.Tan(keyframe.Value / 2) * AspectRatio));
                }
                frameInfo.KeyFrames.Add(keyframe);
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
        if (RotationOrAim) { writer.Write<byte>(1); } else { writer.Write<byte>(0); }
        writer.Skip(3);
        writer.Write(FPS);
        writer.Write(FrameStart);
        writer.Write(FrameEnd);
        writer.Write(FrameInfos.Count);
        writer.Write(Position.X);
        writer.Write(Position.Z);
        writer.Write(Position.Y);
        writer.Write(Rotation.X);
        writer.Write(Rotation.Y);
        writer.Write(Rotation.Z);
        writer.Write(AimPosition.X);
        writer.Write(AimPosition.Z);
        writer.Write(AimPosition.Y);
        writer.Write(Twist);
        writer.Write(ZNear);
        writer.Write(ZFar);
        writer.Write((float)(2 * Math.Atan(Math.Tan(FOV / 2) / AspectRatio)));
        writer.Write(AspectRatio);

        foreach (var frameInfo in FrameInfos)
        {
            writer.Write((byte)frameInfo.Type);
            writer.Skip(3);
            writer.Write(frameInfo.KeyFrames.Count);
            writer.Write(keyframes.Count);

            foreach (var keyFrame in frameInfo.KeyFrames)
            {
                var keyframe = keyFrame;
                if (frameInfo.Type == CamFrameType.FOV)
                {
                    keyframe.Value = (float)(2 * Math.Atan(Math.Tan(keyframe.Value / 2) / AspectRatio));
                }
                keyframes.Add(keyframe);
            }
        }
    }
}

public class CamFrameInfo
{
    public CamFrameType Type { get; set; }
    public List<Keyframe> KeyFrames { get; set; } = new List<Keyframe>();
}

public struct Keyframe
{
    public float Frame { get; set; }
    public float Value { get; set; }
}

public enum CamFrameType
{
    PositionX = 0, PositionY = 1, PositionZ = 2, RotationX = 3, RotationY = 4, RotationZ = 5, AimPositionX = 6, AimPositionY = 7, AimPositionZ = 8, Twist = 9, ZNear = 10, ZFar = 11, FOV = 12, AspectRatio = 13
}
