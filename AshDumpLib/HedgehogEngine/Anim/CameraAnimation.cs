using Amicitia.IO.Binary;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.Anim;

//Research by Kwasior!

public class CameraAnimation
{
    public const string FileExtension = ".cam-anim";

    public List<Camera> Cameras = new();


    public CameraAnimation() { }

    public CameraAnimation(string filename)
    {
        Open(filename);
    }

    public void Open(string filename)
    {
        Read(new(filename, Endianness.Big, System.Text.Encoding.UTF8));
    }

    public void Save(string filename)
    {
        Write(new(filename, Endianness.Big, System.Text.Encoding.Default));
    }

    public void Read(ExtendedBinaryReader reader)
    {
        reader.genericOffset = 0x18;
        reader.Jump(0, SeekOrigin.Begin);
        var camerasPointer = reader.Read<int>();
        var camerasSize = reader.Read<int>();
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

        reader.Jump(camerasPointer, SeekOrigin.Begin);
        var cameraCount = reader.Read<int>();
        for (int i = 0; i < cameraCount; i++)
        {
            var camera = new Camera();
            camera.Read(reader, keyframes);
            Cameras.Add(camera);
        }

        reader.Dispose();
    }

    public void Write(AnimWriter writer)
    {
        writer.AnimationType = AnimWriter.AnimType.CameraAnimation;
        writer.WriteHeader();

        List<Keyframe> keys = new List<Keyframe>();

        writer.AddOffset("cameras", false);
        long cameraSizePos = writer.Position;
        writer.WriteNulls(4);

        writer.AddOffset("keyframes", false);
        long keyframesSizePos = writer.Position;
        writer.WriteNulls(4);

        writer.AddOffset("strings", false);
        writer.WriteNulls(4);

        writer.SetOffset("cameras");
        writer.Write(Cameras.Count);

        foreach(var camera in Cameras)
            writer.AddOffset(camera.Name + "ptr");
            
        foreach(var camera in Cameras)
            camera.Write(writer, keys);

        int cameraSize = (int)(writer.Position - writer.GetOffsetValue("cameras")) - writer.GenericOffset;
        writer.WriteAt(cameraSize, cameraSizePos);

        writer.SetOffset("keyframes");

        foreach(var key in keys)
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

public class Camera
{
    public string Name = "";
    public bool RotationOrAim = true;
    public float FPS = 30;
    public float FrameStart = 0;
    public float FrameEnd = 0;
    public Vector3 Position = new(0, 0, 0);
    public Vector3 Rotation = new(0, 0, 0);
    public Vector3 AimPosition = new(0, 0, 0);
    public float Twist = 0;
    public float ZNear = 0.001f;
    public float ZFar = 1000f;
    public float FOV = 0;
    public float AspectRatio = 1.77779f;
    public List<CamFrameInfo> FrameInfos = new();

    public void Read(ExtendedBinaryReader reader, List<Keyframe> keyframes)
    {
        var pointer = reader.Read<uint>();
        var prePos = reader.Position;
        reader.Jump(pointer, SeekOrigin.Begin);

        Name = reader.ReadStringTableEntry();
        RotationOrAim = reader.Read<byte>() == 1;
        reader.Skip(3);
        FPS = reader.Read<float>();
        FrameStart = reader.Read<float>();
        FrameEnd = reader.Read<float>();
        var FrameInfoCount = reader.Read<int>();
        Vector3 posTemp = new Vector3();
        posTemp.X = reader.Read<float>();
        posTemp.Z = reader.Read<float>();
        posTemp.Y = reader.Read<float>();
        Position = posTemp;
        Vector3 rotTemp = new Vector3();
        rotTemp.X = reader.Read<float>();
        rotTemp.Z = reader.Read<float>();
        rotTemp.Y = reader.Read<float>();
        Rotation = rotTemp;
        Vector3 aimPosTemp = new Vector3();
        aimPosTemp.X = reader.Read<float>();
        aimPosTemp.Z = reader.Read<float>();
        aimPosTemp.Y = reader.Read<float>();
        AimPosition = aimPosTemp;
        Twist = reader.Read<float>();
        ZNear = reader.Read<float>();
        ZFar = reader.Read<float>();
        FOV = reader.Read<float>();
        AspectRatio = reader.Read<float>();
        //Thanks ik-01 for allowing me to use this formula!
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
                    //Thanks ik-01 for allowing me to use this formula!
                    keyframe.Value = (float)(2 * Math.Atan(Math.Tan(keyframe.Value / 2) * AspectRatio));
                }
                frameInfo.KeyFrames.Add(keyframe);
            }
            FrameInfos.Add(frameInfo);
        }

        reader.Seek(prePos, SeekOrigin.Begin);
    }

    public void Write(AnimWriter writer, List<Keyframe> keyframes)
    {
        writer.SetOffset(Name + "ptr");
        writer.WriteStringTableEntry(Name);
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
    public CamFrameType Type;
    public List<Keyframe> KeyFrames = new List<Keyframe>();
}

public enum CamFrameType
{
    PositionX = 0, PositionY = 1, PositionZ = 2, RotationX = 3, RotationY = 4, RotationZ = 5, AimPositionX = 6, AimPositionY = 7, AimPositionZ = 8, Twist = 9, ZNear = 10, ZFar = 11, FOV = 12, AspectRatio = 13
}
