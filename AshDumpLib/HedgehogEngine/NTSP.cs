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


    public class NTSI : IFile
    {
        public const string FileExtension = ".dds";
        public const string Signature = "NTSI";

        public int Version = 1;
        public byte[] ImageData = new byte[0];

        public NTSI() { }

        public NTSI(string filename) => Open(filename);
        public NTSI(string filename, byte[] data) => Open(filename, data);

        public override void Read(ExtendedBinaryReader reader)
        {
            reader.ReadSignature(Signature);
            Version = reader.Read<int>();
            reader.Skip(4);
            int ntspNameLength = reader.Read<int>();
            int unkDataSize = reader.Read<int>();
            int unk = reader.Read<int>();
            string ntspName = reader.ReadString(StringBinaryFormat.FixedLength, ntspNameLength);
            NTSP ntsp = NTSPManager.GetNTSP(ntspName);
            reader.Skip(unkDataSize);
            List<byte> data = new();
            while(reader.Position < Data.Length)
                data.Add(reader.Read<byte>());
            byte[] ntspData = ntsp.Textures.Find(x => x.Name == FileName.Replace("." + Extension, "")).Data;
            data.AddRange(ntspData);
            ImageData = data.ToArray();
            reader.Dispose();
        }
    }


    public class NTSPManager
    {
        public static string CurrentDirectory = "";
        public static Dictionary<string, NTSP> LoadedNTSPs { get; private set; } = new();

        public static NTSP GetNTSP(string ntspName)
        {
            if(LoadedNTSPs.ContainsKey(Path.Combine(CurrentDirectory, ntspName) + ".ntsp"))
                return LoadedNTSPs[Path.Combine(CurrentDirectory, ntspName) + ".ntsp"];

            NTSP ntsp = new(Path.Combine(CurrentDirectory, ntspName) + ".ntsp");
            LoadedNTSPs.Add(Path.Combine(CurrentDirectory, ntspName) + ".ntsp", ntsp);
            return ntsp;
        }
    }
}
