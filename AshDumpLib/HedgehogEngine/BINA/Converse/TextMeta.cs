using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using static AshDumpLib.Helpers.MathA;

namespace AshDumpLib.HedgehogEngine.BINA.Converse;

public class TextMeta : IFile
{
    public const string FileExtension = ".cnvrs-meta";

    public long Version = 2;
    public List<TypeFace> TypeFaces = new();
    public IconData Images = new();

    public TextMeta() { }

    public TextMeta(string filename) => Open(filename);
    public TextMeta(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();

        Version = reader.Read<long>();
        long typeFacesPtr = reader.Read<long>();
        long imagesPtr = reader.Read<long>();
        reader.ReadAtOffset(typeFacesPtr + 64, () => 
        {
            TypeFaces = reader.ReadBINAArrayStruct64<TypeFace>(false);
        });
        reader.ReadAtOffset(imagesPtr + 64, () => { Images.Read(reader); });

        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();

        writer.Write(Version);
        writer.AddOffset("typeFaces");
        writer.AddOffset("images");
        writer.WriteNulls(8);
        writer.SetOffset("typeFaces");
        writer.Write((long)TypeFaces.Count);
        writer.AddOffset("typeFacesList");
        writer.WriteNulls(8);
        writer.SetOffset("typeFacesList");
        foreach(var i in TypeFaces)
            i.Write(writer);
        foreach (var i in TypeFaces)
            i.FinishWrite(writer);
        writer.WriteNulls(8);
        writer.SetOffset("images");
        Images.Write(writer);

        writer.FinishWrite();
        writer.Dispose();
    }

    public class TypeFace : IBINASerializable
    {
        long id = Random.Shared.NextInt64();

        public string Name0 = "";
        public string Name1 = "";
        public float Unk0 = 0.0f;
        public float Unk1 = 0.0f;
        public float Unk2 = 0.0f;
        public float Unk3 = 0.0f;
        public List<string> Parents = new();

        public void Read(BINAReader reader)
        {
            long ptr = reader.Read<long>();
            reader.ReadAtOffset(ptr + 64, () =>
            {
                Parents = reader.ReadBINAStringArray64();
                Name0 = reader.ReadStringTableEntry();
                Name1 = reader.ReadStringTableEntry();
                Unk0 = reader.Read<float>();
                Unk1 = reader.Read<float>();
                Unk2 = reader.Read<float>();
                Unk3 = reader.Read<float>();
                //after that it's just space for two pointers that the game then takes use of
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.AddOffset(Name0 + Name1 + id);
        }

        public void FinishWrite(BINAWriter writer)
        {
            writer.WriteNulls(8);
            writer.SetOffset(Name0 + Name1 + id);
            writer.Write((long)Parents.Count);
            writer.AddOffset(Name0 + Name1 + id + "parents" + Parents.Count);
            writer.WriteStringTableEntry(Name0);
            writer.WriteStringTableEntry(Name1);
            writer.Write(Unk0);
            writer.Write(Unk1);
            writer.Write(Unk2);
            writer.Write(Unk3);
            writer.WriteNulls(8);
            writer.SetOffset(Name0 + Name1 + id + "parents" + Parents.Count);
            foreach (var i in Parents)
                writer.WriteStringTableEntry(i);
        }
    }

    public class IconData : IBINASerializable
    {
        public class Icon : IBINASerializable
        {
            public string IconName = "";
            public string ResourceName = "";
            public float Unk0 = 2;
            public float Unk1 = 2;
            public float Unk2 = 0;
            public float Unk3 = 0;
            public Crop ImageCrop = new() { Bottom = 0, Left = 0, Top = 1, Right = 1 };

            public void Read(BINAReader reader)
            {
                long ptr = reader.Read<long>();
                reader.ReadAtOffset(ptr + 64, () =>
                {
                    IconName = reader.ReadStringTableEntry();
                    ResourceName = reader.ReadStringTableEntry();
                    Unk0 = reader.Read<float>();
                    Unk1 = reader.Read<float>();
                    Unk2 = reader.Read<float>();
                    Unk3 = reader.Read<float>();
                    ImageCrop = reader.Read<Crop>();
                    //after that it's just space for two pointers that the game then takes use of
                });
            }

            public void Write(BINAWriter writer)
            {
                writer.AddOffset(IconName + ResourceName);
            }

            public void FinishWrite(BINAWriter writer)
            {
                writer.SetOffset(IconName + ResourceName);
                writer.WriteStringTableEntry(IconName);
                writer.WriteStringTableEntry(ResourceName);
                writer.Write(Unk0);
                writer.Write(Unk1);
                writer.Write(Unk2);
                writer.Write(Unk3);
                writer.Write(ImageCrop);
                writer.WriteNulls(16);
            }
        }

        public List<Icon> Icons = new();
        public List<string> Resources = new();

        public void Read(BINAReader reader)
        {
            Icons = reader.ReadBINAArrayStruct64<Icon>(false);
            Resources = reader.ReadBINAStringArray64();
        }

        public void Write(BINAWriter writer)
        {
            writer.Write((long)Icons.Count);
            writer.AddOffset("icons");
            writer.Write((long)Resources.Count);
            writer.AddOffset("resources");
            writer.WriteNulls(8);
            writer.SetOffset("icons");
            foreach (var i in Icons)
                i.Write(writer);
            writer.WriteNulls(8);
            foreach (var i in Icons)
                i.FinishWrite(writer);
            writer.SetOffset("resources");
            foreach (var i in Resources)
                writer.WriteStringTableEntry(i);
            writer.WriteNulls(16);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}