using AshDumpLib.Helpers.Archives;
using System.Numerics;
using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine.BINA.Density;

// Research by angryzor!

public class DensitySetting : IFile
{
    public const string FileExtension = ".densitysetting";
    public const string BINASignature = "GSDC";

    public int Version = 11;
    public Vector2 WorldSize = new(4096, 4096);
    public int Unk0 = 128;
    public int ModelCount = 0;
    public int[] LODUnk = new int[32] { 24, 22, 20, 18, 16, 14, 12, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
    public float[] LODRanges = new float[32] { 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f, 1.1f, 1.2f, 1.4f, 1.6f, 1.8f, 2.1f, 2.6f, 3.1f, 3.6f, 4.1f, 5.1f, 6.1f, 7.1f, 8.1f, 9.1f, 10.1f, 12.6f, 15.1f, 17.6f, 20.1f, 25.1f, 30.1f, 45.1f, 50.1f, 60.1f, 70.1f };
    public string AreaMap = "{stage-id}_area";
    public string LayerMap = "{stage-id}_layer";
    public string ColorMap = "{stage-id}_color";
    public string ScaleMap = "{stage-id}_scale";
    public List<Model> Models = new();
    public List<LODGroup> LODGroups = new();
    public List<LODGroupReference> LODGroupReferences = new();
    public List<Biome> Biomes = new();
    public List<BiomeReference> BiomeReferences = new();
    public List<IDItem> IDItems = new();
    public List<CollisionData> CollisionDatas = new();
    public List<string> CollisionResourceNames = new();
    public List<int> Unk0s = new();
    public List<char> Unk1s = new();
    public List<char> Unk2s = new();
    public List<char> Unk3s = new();
    public List<char> Unk4s = new();
    public List<int> SoundEffectIndices = new();
    public List<string> SoundEffects = new();

    List<T> ReadList<T>(BINAReader reader) where T : IBINASerializable, new()
    {
        reader.Align(8);
        long ptr = reader.Read<long>() + 64;
        int count = reader.Read<int>();
        List<T> list = new();
        reader.ReadAtOffset(ptr, () =>
        {
            for (int i = 0; i < count; i++)
            {
                T item = new();
                item.Read(reader);
                list.Add(item);
            }
        });
        return list;
    }

    List<T> ReadUList<T>(BINAReader reader) where T : unmanaged
    {
        reader.Align(8);
        long ptr = reader.Read<long>() + 64;
        int count = reader.Read<int>();
        List<T> list = new();
        reader.ReadAtOffset(ptr, () =>
        {
            for (int i = 0; i < count; i++)
                list.Add(reader.Read<T>());
        });
        return list;
    }

    List<string> ReadStringList(BINAReader reader)
    {
        reader.Align(8);
        long ptr = reader.Read<long>() + 64;
        int count = reader.Read<int>();
        List<string> list = new();
        reader.ReadAtOffset(ptr, () =>
        {
            for (int i = 0; i < count; i++)
                list.Add(reader.ReadStringTableEntry());
        });
        return list;
    }

    void WriteListHeader<T>(BINAWriter writer, List<T> list) where T : IBINASerializable
    {
        writer.Align(8);
        writer.AddOffset(typeof(T).Name);
        writer.Write(list.Count);
    }

    void WriteUListHeader<T>(BINAWriter writer, List<T> list, string name)
    {
        writer.Align(8);
        writer.AddOffset(name);
        writer.Write(list.Count);
    }

    void WriteList<T>(BINAWriter writer, List<T> list) where T : IBINASerializable
    {
        var fields = typeof(T).GetFields().ToList();
        if (fields.Count(x => x.FieldType == typeof(string)) > 0)
            writer.Align(8);
        writer.SetOffset(typeof(T).Name);
        foreach (var item in list)
            item.Write(writer);
    }

    void WriteUList<T>(BINAWriter writer, List<T> list, string name) where T : unmanaged
    {
        writer.SetOffset(name);
        foreach (var item in list)
            writer.Write(item);
    }

    void WriteStringList(BINAWriter writer, List<string> list, string name)
    {
        writer.Align(8);
        writer.SetOffset(name);
        foreach (var item in list)
            writer.WriteStringTableEntry(item);
    }

    public DensitySetting() { }

    public DensitySetting(string filename) => Open(filename);
    public DensitySetting(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        WorldSize = reader.Read<Vector2>();
        Unk0 = reader.Read<int>();
        ModelCount = reader.Read<int>();
        LODUnk = reader.ReadArray<int>(32);
        LODRanges = reader.ReadArray<float>(32);
        AreaMap = reader.ReadStringTableEntry();
        LayerMap = reader.ReadStringTableEntry();
        ColorMap = reader.ReadStringTableEntry();
        ScaleMap = reader.ReadStringTableEntry();

        Models = ReadList<Model>(reader);
        LODGroups = ReadList<LODGroup>(reader);
        LODGroupReferences = ReadList<LODGroupReference>(reader);
        Biomes = ReadList<Biome>(reader);
        BiomeReferences = ReadList<BiomeReference>(reader);
        IDItems = ReadList<IDItem>(reader);
        CollisionDatas = ReadList<CollisionData>(reader);
        CollisionResourceNames = ReadStringList(reader);
        Unk0s = ReadUList<int>(reader);
        Unk1s = ReadUList<char>(reader);
        Unk2s = ReadUList<char>(reader);
        Unk3s = ReadUList<char>(reader);
        Unk4s = ReadUList<char>(reader);
        SoundEffectIndices = ReadUList<int>(reader);
        SoundEffects = ReadStringList(reader);

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.Write(WorldSize);
        writer.Write(Unk0);
        writer.Write(ModelCount);
        writer.WriteArray(LODUnk);
        writer.WriteArray(LODRanges);
        writer.WriteStringTableEntry(AreaMap);
        writer.WriteStringTableEntry(LayerMap);
        writer.WriteStringTableEntry(ColorMap);
        writer.WriteStringTableEntry(ScaleMap);

        WriteListHeader(writer, Models);
        WriteListHeader(writer, LODGroups);
        WriteListHeader(writer, LODGroupReferences);
        WriteListHeader(writer, Biomes);
        WriteListHeader(writer, BiomeReferences);
        WriteListHeader(writer, IDItems);
        WriteListHeader(writer, CollisionDatas);
        WriteUListHeader(writer, CollisionResourceNames, "collisionResourceNames");
        WriteUListHeader(writer, Unk0s, "unk0s");
        WriteUListHeader(writer, Unk1s, "unk1s");
        WriteUListHeader(writer, Unk2s, "unk2s");
        WriteUListHeader(writer, Unk3s, "unk3s");
        WriteUListHeader(writer, Unk4s, "unk4s");
        WriteUListHeader(writer, SoundEffectIndices, "soundEffectIndices");
        WriteUListHeader(writer, SoundEffects, "soundEffects");

        WriteList(writer, Models);
        WriteList(writer, LODGroups);
        WriteList(writer, LODGroupReferences);
        WriteList(writer, Biomes);
        WriteList(writer, BiomeReferences);
        WriteList(writer, IDItems);
        WriteList(writer, CollisionDatas);
        WriteStringList(writer, CollisionResourceNames, "collisionResourceNames");
        WriteUList(writer, Unk0s, "unk0s");
        WriteUList(writer, Unk1s, "unk1s");
        WriteUList(writer, Unk2s, "unk2s");
        WriteUList(writer, Unk3s, "unk3s");
        WriteUList(writer, Unk4s, "unk4s");
        WriteUList(writer, SoundEffectIndices, "soundEffectIndices");
        WriteStringList(writer, SoundEffects, "soundEffects");

        writer.FinishWrite();
        writer.Dispose();
    }

    public class Model : IBINASerializable
    {
        public string Name = "";
        public int Flags = 0;

        public void Read(BINAReader reader)
        {
            reader.Align(8);
            Name = reader.ReadStringTableEntry();
            Flags = reader.Read<int>();
        }

        public void Write(BINAWriter writer)
        {
            writer.Align(8);
            writer.WriteStringTableEntry(Name);
            writer.Write(Flags);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class LODGroup : IBINASerializable
    {
        public int FootPoint = 128;
        public int Flags = 0;
        public int LODGroupReferenceOffset = 0;
        public int LODGroupReferenceCount = 0;
        public int CollisionDataIndex1 = 0;
        public int CollisionDataIndex2 = 0;
        public int ShadowLODGroupReferenceOffset = 0;
        public int ShadowLODGroupReferenceCount = 0;
        public float Angle = 1;
        public Vector3 Offset = new(0, 0, 0);
        public Vector3 Scale = new(1, 1, 1);
        public int ID = 0;

        public void Read(BINAReader reader)
        {
            FootPoint = reader.Read<int>();
            Flags = reader.Read<int>();
            LODGroupReferenceOffset = reader.Read<int>();
            LODGroupReferenceCount = reader.Read<int>();
            CollisionDataIndex1 = reader.Read<int>();
            CollisionDataIndex2 = reader.Read<int>();
            ShadowLODGroupReferenceOffset = reader.Read<int>();
            ShadowLODGroupReferenceCount = reader.Read<int>();
            Angle = reader.Read<float>();
            Offset = reader.Read<Vector3>();
            Scale = reader.Read<Vector3>();
            ID = reader.Read<int>();
        }
        public void Write(BINAWriter writer)
        {
            writer.Write(FootPoint);
            writer.Write(Flags);
            writer.Write(LODGroupReferenceOffset);
            writer.Write(LODGroupReferenceCount);
            writer.Write(CollisionDataIndex1);
            writer.Write(CollisionDataIndex2);
            writer.Write(ShadowLODGroupReferenceOffset);
            writer.Write(ShadowLODGroupReferenceCount);
            writer.Write(Angle);
            writer.Write(Offset);
            writer.Write(Scale);
            writer.Write(ID);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class LODGroupReference : IBINASerializable
    {
        public int ModelIndex = 0;
        public float Length = 16;
        public int Flags = 3;
        public int Unk1 = 0;

        public void Read(BINAReader reader)
        {
            ModelIndex = reader.Read<int>();
            Length = reader.Read<float>();
            Flags = reader.Read<int>();
            Unk1 = reader.Read<int>();
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(ModelIndex);
            writer.Write(Length);
            writer.Write(Flags);
            writer.Write(Unk1);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class Biome : IBINASerializable
    {
        public int Flags = 0;
        public int BiomeReferenceOffset = 0;
        public int BiomeReferenceCount = 0;
        public int MinBiomeReferenceOffset = 0;

        public void Read(BINAReader reader)
        {
            Flags = reader.Read<int>();
            BiomeReferenceOffset = reader.Read<int>();
            BiomeReferenceCount = reader.Read<int>();
            MinBiomeReferenceOffset = reader.Read<int>();
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(Flags);
            writer.Write(BiomeReferenceOffset);
            writer.Write(BiomeReferenceCount);
            writer.Write(MinBiomeReferenceOffset);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class BiomeReference : IBINASerializable
    {
        public int Index = 0;
        public int Flags = 0;
        public int Probability = 0;
        public int CopyFootPoint = 0;
        public Vector2 Scale = new(0, 1); // Min Max
        public int HSVPh = 0;
        public int HSVSv = 0;
        public int Upper = 0;
        public int Unk0 = 0;
        public int Unk1 = 0;
        public int Unk2 = 0;

        public void Read(BINAReader reader)
        {
            Index = reader.Read<int>();
            Flags = reader.Read<int>();
            Probability = reader.Read<int>();
            CopyFootPoint = reader.Read<int>();
            Scale = reader.Read<Vector2>();
            HSVPh = reader.Read<int>();
            HSVSv = reader.Read<int>();
            Upper = reader.Read<int>();
            Unk0 = reader.Read<int>();
            Unk1 = reader.Read<int>();
            Unk2 = reader.Read<int>();
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(Index);
            writer.Write(Flags);
            writer.Write(Probability);
            writer.Write(CopyFootPoint);
            writer.Write(Scale);
            writer.Write(HSVPh);
            writer.Write(HSVSv);
            writer.Write(Upper);
            writer.Write(Unk0);
            writer.Write(Unk1);
            writer.Write(Unk2);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class IDItem : IBINASerializable // unknown, possibly related to particles
    {
        public string Name = "";
        public int LODGroupIndex = 0;
        public int Unk0 = 0;
        public int Unk1 = 0;

        public void Read(BINAReader reader)
        {
            reader.Align(8);
            Name = reader.ReadStringTableEntry();
            LODGroupIndex = reader.Read<int>();
            Unk0 = reader.Read<int>();
            Unk1 = reader.Read<int>();
        }

        public void Write(BINAWriter writer)
        {
            writer.Align(8);
            writer.WriteStringTableEntry(Name);
            writer.Write(LODGroupIndex);
            writer.Write(Unk0);
            writer.Write(Unk1);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class CollisionData : IBINASerializable // unknown, possibly related to particles
    {
        public Vector3 Offset = new(0, 0, 0);
        public Quaternion Rotation = new(0, 0, 0, 1);
        public float Radius = 0;
        public int HitFlags = 0;
        public int Attributes = 0;
        public float CollisionReferenceOffset = 0;
        public Vector3 Params = new Vector3(0, 0, 0);
        public byte IDUpperType = 0;
        public byte IDUpperUnk0 = 0;
        public short IDUpperUnk1 = 0;
        public int IDLower = 0;
        public int SoundEffectIndex = 0;

        public void Read(BINAReader reader)
        {
            Offset = reader.Read<Vector3>();
            Rotation = reader.Read<Quaternion>();
            Radius = reader.Read<float>();
            HitFlags = reader.Read<int>();
            Attributes = reader.Read<int>();
            CollisionReferenceOffset = reader.Read<float>();
            Params = reader.Read<Vector3>();
            IDUpperType = reader.Read<byte>();
            IDUpperUnk0 = reader.Read<byte>();
            IDUpperUnk1 = reader.Read<short>();
            IDLower = reader.Read<int>();
            SoundEffectIndex = reader.Read<int>();
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(Offset);
            writer.Write(Rotation);
            writer.Write(Radius);
            writer.Write(HitFlags);
            writer.Write(Attributes);
            writer.Write(CollisionReferenceOffset);
            writer.Write(Params);
            writer.Write(IDUpperType);
            writer.Write(IDUpperUnk0);
            writer.Write(IDUpperUnk1);
            writer.Write(IDLower);
            writer.Write(SoundEffectIndex);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}