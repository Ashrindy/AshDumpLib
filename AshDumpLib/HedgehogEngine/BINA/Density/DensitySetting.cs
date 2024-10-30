using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA.Density;

public class DensitySetting : IFile
{
    public struct OffsetInfo
    {
        public long offset;
        public long count;
    }

    public const string FileExtension = ".densitysetting";
    public const string BINASignature = "GSDC";

    public int Version = 11;
    public Vector2 WorldSize = new(4096, 4096);
    public int Unk0 = 128;
    public int Unk1 = 658;
    public int[] Unk2 = new int[32] { 24, 22, 20, 18, 16, 14, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
    public float[] Unk3 = new float[32] { 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.1f, 1.2f, 1.4f, 1.6f, 1.8f, 2.1f, 2.6f, 3.1f, 3.6f, 4.1f, 5.1f, 6.1f, 7.1f, 8.1f, 9.1f, 10.1f, 12.6f, 15.1f, 17.6f, 20.1f, 25.1f, 30.1f, 45.1f, 50.1f, 60.1f, 70.1f };
    public string AreaMap = "{stage-id}_area";
    public string LayerMap = "{stage-id}_layer";
    public string ColorMap = "{stage-id}_color";
    public string ScaleMap = "{stage-id}_scale";
    OffsetInfo[] Offsets = new OffsetInfo[15];
    public List<DensityModelReference> DensityModelReferences = new();
    public List<IDInfo> IDInfos = new();
    public List<LODInfo> LODInfos = new();
    public List<AreaMapInfo> AreaMapInfos = new();
    public List<IDItem> IDList = new();
    public List<string> CollisionData = new();

    public DensitySetting() { }

    public DensitySetting(string filename) => Open(filename);
    public DensitySetting(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        WorldSize = reader.Read<Vector2>();
        Unk0 = reader.Read<int>();
        Unk1 = reader.Read<int>();
        Unk2 = reader.ReadArray<int>(32);
        Unk3 = reader.ReadArray<float>(32);
        AreaMap = reader.ReadStringTableEntry();
        LayerMap = reader.ReadStringTableEntry();
        ColorMap = reader.ReadStringTableEntry();
        ScaleMap = reader.ReadStringTableEntry();
        Offsets = reader.ReadArray<OffsetInfo>(15);

        reader.Jump(Offsets[0].offset, SeekOrigin.Begin);
        for (int i = 1; i < Offsets[0].count; i++)
        {
            DensityModelReference tempDensity = new();
            tempDensity.Read(reader);
            DensityModelReferences.Add(tempDensity);
        }

        reader.Jump(Offsets[1].offset, SeekOrigin.Begin);
        for (int i = 1; i < Offsets[1].count; i++)
        {
            IDInfo tempIDInfo = new();
            tempIDInfo.Read(reader);
            IDInfos.Add(tempIDInfo);
        }

        reader.Jump(Offsets[2].offset, SeekOrigin.Begin);
        for (int i = 1; i < Offsets[2].count; i++)
        {
            LODInfo tempLodInfo = new();
            tempLodInfo.Read(reader);
            LODInfos.Add(tempLodInfo);
        }

        reader.Jump(Offsets[3].offset, SeekOrigin.Begin);
        for (int i = 1; i < Offsets[3].count; i++)
        {
            AreaMapInfo tempArea = new();
            tempArea.Read(reader);
            AreaMapInfos.Add(tempArea);
        }

        reader.Jump(Offsets[5].offset, SeekOrigin.Begin);
        for (int i = 1; i < Offsets[5].count; i++)
        {
            IDItem tempID = new();
            tempID.Read(reader);
            IDList.Add(tempID);
        }

        reader.Jump(Offsets[7].offset, SeekOrigin.Begin);
        for (int i = 1; i < Offsets[7].count; i++)
        {
            string tempCollision = reader.ReadStringTableEntry();
            CollisionData.Add(tempCollision);
        }

        reader.Dispose();
    }

    public class DensityModelReference
    {
        public string Name = "";
        public int Unk0 = 0;
        public int Unk1 = 0;

        public void Read(BINAReader reader)
        {
            Name = reader.ReadStringTableEntry();
            Unk0 = reader.ReadInt32();
            Unk1 = reader.ReadInt32();
        }
    }

    public class IDInfo
    {
        public short Unk0 = 128;
        public short Unk1 = 0;
        public short Unk2 = 4096;
        public short Unk3 = 1;
        public int Unk4 = 0;
        public int Unk5 = 3;
        public int Unk6 = 0;
        public int Unk7 = 0;
        public int Unk8 = 3;
        public int Unk9 = 0;
        public float Unk10 = 1;
        public float Unk11 = 0;
        public float Unk12 = 0;
        public float Unk13 = 0;
        public float Unk14 = 1;
        public float Unk15 = 1;
        public float Unk16 = 1;
        public short Unk17 = 24888;
        public short Unk18 = 2879;

        public void Read(BINAReader reader)
        {
            Unk0 = reader.Read<short>();
            Unk1 = reader.Read<short>();
            Unk2 = reader.Read<short>();
            Unk3 = reader.Read<short>();
            Unk4 = reader.Read<int>();
            Unk5 = reader.Read<int>();
            Unk6 = reader.Read<int>();
            Unk7 = reader.Read<int>();
            Unk8 = reader.Read<int>();
            Unk9 = reader.Read<int>();
            Unk10 = reader.Read<float>();
            Unk11 = reader.Read<float>();
            Unk12 = reader.Read<float>();
            Unk13 = reader.Read<float>();
            Unk14 = reader.Read<float>();
            Unk15 = reader.Read<float>();
            Unk16 = reader.Read<float>();
            Unk17 = reader.Read<short>();
            Unk18 = reader.Read<short>();
        }
    }

    public class LODInfo
    {
        public int ModelIndex = 0;
        public float FadeDistance = 16;
        public int Unk0 = 3;
        public int Unk1 = 0;

        public void Read(BINAReader reader)
        {
            ModelIndex = reader.Read<int>();
            FadeDistance = reader.Read<float>();
            Unk0 = reader.Read<int>();
            Unk1 = reader.Read<int>();
        }
    }

    public class AreaMapInfo
    {
        public int Unk0 = 0;
        public int ModelIndex = 0;
        public int Unk1 = 5;
        public int Unk2 = 0;

        public void Read(BINAReader reader)
        {
            Unk0 = reader.Read<int>();
            ModelIndex = reader.Read<int>();
            Unk1 = reader.Read<int>();
            Unk2 = reader.Read<int>();
        }
    }

    public class IDItem
    {
        public string Name = "";
        public int ID = 0;
        public int Unk0 = 1;
        public int Unk1 = 0;
        public int Unk2 = 0;

        public void Read(BINAReader reader)
        {
            Name = reader.ReadStringTableEntry();
            ID = reader.Read<int>();
            Unk0 = reader.Read<int>();
            Unk1 = reader.Read<int>();
            Unk2 = reader.Read<int>();
        }
    }
}