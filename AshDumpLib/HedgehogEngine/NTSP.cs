using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine;

// Some info taken from Skyth's NTSPMaker

public class NTSP : IFile
{
    public const string FileExtension = ".ntsp";
    public const string Signature = "PSTN";

    public int Version = 1;
    public List<Texture> Textures = new();

    public NTSP() { }

    public NTSP(string filename) => Open(filename);
    public NTSP(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.ReadSignature(Signature);
        Version = reader.Read<int>();
        int textureCount = reader.Read<int>();
        int blobCount = reader.Read<int>();
        long blobDataPtr = reader.Read<long>();
        List<tempTexture> tempTextures = new();
        for(int i = 0; i < textureCount; i++)
        {
            tempTexture tex = new();
            tex.NameHash = reader.Read<int>();
            tex.blobIndex = reader.Read<int>();
            tex.blobCount = reader.Read<int>();
            tex.width = reader.Read<short>();
            tex.height = reader.Read<short>();
            long namePtr = reader.Read<long>();
            reader.ReadAtOffset(namePtr, () => tex.Name = reader.ReadString(StringBinaryFormat.NullTerminated));
            tempTextures.Add(tex);
        }
        List<byte[]> blobs = new();
        for(int i = 0; i < blobCount; i++)
        {
            long dataPtr = reader.Read<long>();
            long dataSize = reader.Read<long>();
            byte[] data = new byte[dataSize];
            reader.ReadAtOffset(dataPtr, () => data = reader.ReadArray<byte>((int)dataSize));
            blobs.Add(data);
        }
        foreach(var i in tempTextures)
        {
            Texture texture = new();
            texture.Name = i.Name;
            texture.NameHash = i.NameHash;
            texture.Width = i.width;
            texture.Height = i.height;
            List<byte> data = new();
            for(int x = i.blobIndex; x < (i.blobIndex + i.blobCount); x++)
                data.AddRange(blobs[x]);
            texture.Data = data.ToArray();
            Textures.Add(texture);
        }
        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteSignature(Signature);
    }

    public struct Texture
    {
        public string Name;
        public int NameHash;
        public byte[] Data;
        public int Width;
        public int Height;
    }

    struct tempTexture
    {
        public int NameHash;
        public int blobIndex;
        public int blobCount;
        public short width;
        public short height;
        public string Name;
    }
}

public class NTSI : IFile
{
    public const string FileExtension = ".dds";
    public const string Signature = "NTSI";

    public int Version = 1;
    public string PackageName = "";
    public int Mip4x4Index = 0;
    public byte[] Mip4x4 = new byte[0];
    public List<byte> DDSHeader = new();

    public NTSI() { }

    public NTSI(string filename) => Open(filename);
    public NTSI(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.ReadSignature(Signature);
        Version = reader.Read<int>();
        reader.Skip(4);
        int ntspNameLength = reader.Read<int>();
        int mip4x4size = reader.Read<int>();
        Mip4x4Index = reader.Read<int>();
        PackageName = reader.ReadString(StringBinaryFormat.FixedLength, ntspNameLength);
        Mip4x4 = reader.ReadArray<byte>(mip4x4size);
        List<byte> data = new();
        while (reader.Position < Data.Length)
            DDSHeader.Add(reader.Read<byte>());
        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteSignature(Signature);
        writer.Write(Version);
        writer.WriteNulls(4);
        writer.Write(PackageName.Length + 1);
        writer.Write(Mip4x4.Length);
        writer.Write(Mip4x4Index);
        writer.WriteString(StringBinaryFormat.NullTerminated, PackageName);
        writer.WriteArray(Mip4x4);
        writer.WriteArray(DDSHeader.ToArray());
        writer.Dispose();
    }
}
