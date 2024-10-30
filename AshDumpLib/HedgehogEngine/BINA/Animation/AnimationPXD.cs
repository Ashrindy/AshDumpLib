using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;

namespace AshDumpLib.HedgehogEngine.BINA.Animation;

//Taken from Adel's Blender Addon

public class AnimationPXD : IFile
{
    public const string FileExtension = ".anm.pxd";
    public const string BINASignature = "NAXP";

    public int Version = 512;
    public bool Additive = false;
    public float PlayRate = 1;
    public float FrameRate = 30;
    public int FrameCount = 0;
    public int TrackCount = 0;
    public byte[] MainCompressed;
    public byte[] RootCompressed;

    public AnimationPXD() { }

    public AnimationPXD(string filename) => Open(filename);
    public AnimationPXD(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        Additive = reader.Read<byte>() == 1;
        byte compressed = reader.Read<byte>();
        reader.Align(8);
        long dataOffset = reader.Read<long>();
        reader.Jump(dataOffset, SeekOrigin.Begin);
        PlayRate = reader.Read<float>();
        FrameCount = reader.Read<int>();
        if (PlayRate != 0)
            FrameRate = (FrameCount - 1) / PlayRate;
        else
            FrameRate = 30;
        TrackCount = (int)reader.Read<long>();
        long mainOffset = reader.Read<long>();
        long rootOffset = reader.Read<long>();

        reader.Jump(mainOffset, SeekOrigin.Begin);
        int mainBufferLength = reader.Read<int>();
        reader.Jump(mainOffset, SeekOrigin.Begin);
        MainCompressed = reader.ReadArray<byte>(mainBufferLength);

        if (rootOffset != 0)
        {
            reader.Jump(rootOffset, SeekOrigin.Begin);
            int rootBufferLength = reader.Read<int>();
            reader.Jump(rootOffset, SeekOrigin.Begin);
            RootCompressed = reader.ReadArray<byte>(rootBufferLength);
        }

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.Write(Additive ? 1 : 0);
        writer.Write((byte)1);
        writer.Align(8);
        writer.AddOffset("dataOffset");
        writer.SetOffset("dataOffset");
        writer.Write(PlayRate);
        writer.Write(FrameCount);
        writer.Write((long)TrackCount);
        writer.AddOffset("mainOffset");
        writer.AddOffset("rootOffset");
        writer.Align(16);
        writer.SetOffset("mainOffset");
        writer.Write(MainCompressed.Length);
        writer.WriteArray(MainCompressed);
        writer.Align(8);
        writer.SetOffset("rootOffset");
        writer.Write(RootCompressed.Length);
        writer.WriteArray(RootCompressed);
        writer.Align(8);

        writer.FinishWrite();
        writer.Dispose();
    }
}