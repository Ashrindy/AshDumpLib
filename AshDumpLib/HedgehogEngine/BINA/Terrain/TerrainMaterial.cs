using AshDumpLib.Helpers.Archives;

namespace AshDumpLib.HedgehogEngine.BINA.Terrain;

public class TerrainMaterial : IFile
{
    public const string FileExtension = ".terrain-material";
    public const string BINASignature = "MTDN";

    public int Version = 1;
    public List<Material> Materials = new();

    public TerrainMaterial() { }

    public TerrainMaterial(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        long dataPtr = reader.Read<long>();
        long count = reader.Read<long>();
        reader.Jump(dataPtr, SeekOrigin.Begin);
        for (int i = 0; i < count; i++)
        {
            Material mat = new();
            mat.Read(reader);
            Materials.Add(mat);
        }
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.AddOffset("dataPtr");
        writer.Write(Materials.Count);
        writer.SetOffset("dataPtr");
        foreach (var i in Materials)
            i.Write(writer);
        writer.Dispose();
    }

    public class Material : IBINASerializable
    {
        public string MaterialName = "grass";
        public int[] IDs = new int[4] { 0, 2, 1, 0 };
        public string DetailDiffuseTextureName = "_terrain_detail_abd";
        public string DetailNormalTextureName = "_terrain_detail_nrm";
        public string DetailHeightTextureName = "_terrain_detail_hgt";
        public string DiffuseTextureName = "_terrain_base_abd";
        public string NormalTextureName = "_terrain_base_nrm";
        public string SpecularTextureName = "_terrain_base_prm";

        public void Read(BINAReader reader)
        {
            MaterialName = reader.ReadStringTableEntry();
            IDs = reader.ReadArray<int>(4);
            DetailDiffuseTextureName = reader.ReadStringTableEntry();
            DetailNormalTextureName = reader.ReadStringTableEntry();
            DetailHeightTextureName = reader.ReadStringTableEntry();
            DiffuseTextureName = reader.ReadStringTableEntry();
            NormalTextureName = reader.ReadStringTableEntry();
            SpecularTextureName = reader.ReadStringTableEntry();
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(MaterialName);
            writer.WriteArray(IDs);
            writer.WriteStringTableEntry(DetailDiffuseTextureName);
            writer.WriteStringTableEntry(DetailNormalTextureName);
            writer.WriteStringTableEntry(DetailHeightTextureName);
            writer.WriteStringTableEntry(DiffuseTextureName);
            writer.WriteStringTableEntry(NormalTextureName);
            writer.WriteStringTableEntry(SpecularTextureName);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }
}