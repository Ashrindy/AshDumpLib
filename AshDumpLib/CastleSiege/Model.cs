using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using System.Numerics;

namespace AshDumpLib.CastleSiege;

public class Model : IFile
{
    public const string FileExtension = ".rdo";

    public int Version = 2011;
    public List<Bone> Bones = new();
    public List<Mesh> Meshes = new();
    public List<Material> Materials = new();

    public Model() { }

    public Model(string filename) => Open(filename);
    public Model(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        int amount;
        int boneAmount;
        int numMaterials;

        Version = reader.Read<int>();
        FileName = Helpers.CastleStrikeString(reader) + ".rdo";
        FilePath = Helpers.CastleStrikeString(reader);
        amount = reader.Read<int>();
        boneAmount = reader.Read<int>();
        numMaterials = reader.Read<int>();
        reader.Skip(128);
        string maxFilepath = Helpers.CastleStrikeString(reader);
        for (int i = 0; i < amount; i++)
        {
            Mesh tempMesh = new Mesh();
            tempMesh.Read(reader);
            Meshes.Add(tempMesh);
        }

        reader.Skip(-4);

        foreach (var i in Meshes)
            i.ReadData(reader);

        for (int i = 0; i < boneAmount; i++)
        {
            Bone tempBone = new Bone();
            tempBone.Read(reader);
            Bones.Add(tempBone);
        }

        foreach (var i in Bones)
            i.ReadData(reader);

        for (int i = 0; i < numMaterials; i++)
        {
            Material tempMaterial = new Material();
            tempMaterial.Read(reader);
            Materials.Add(tempMaterial);
        }

        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.Write(Version);
        writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, FileName, 256);
        writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, FilePath, 256);
        writer.Write(Meshes.Count);
        writer.Write(Bones.Count);
        writer.Write(Materials.Count);
        writer.Skip(128 + 256);
        foreach (var i in Meshes)
            i.Write(writer);

        writer.Skip(-4);

        foreach (var i in Meshes)
            i.FinishWrite(writer);

        foreach (var i in Bones)
            i.Write(writer);

        foreach (var i in Bones)
            i.FinishWrite(writer);

        foreach (var i in Materials)
            i.Write(writer);

        writer.Dispose();
    }


    public class Mesh : IExtendedBinarySerializable
    {
        public int MeshVersion = 3001;

        int numVerts = 0;
        int numVertsNormals = 0;
        int numFaces = 0;
        int numFacesNormals = 0;
        int numTVerts = 0;
        int numTFaces = 0;

        public int MaterialID = 0;

        public string MeshName = "";

        public Vector3[] Vertices = new Vector3[0];
        public Face[] Faces = new Face[0];
        public Vector2[] TVertices = new Vector2[0];
        public TFace[] TFaces = new TFace[0];
        public Vector3[] FaceNormals = new Vector3[0];
        public Vector3[] VerticesNormals = new Vector3[0];

        public Vector3 BoundingBoxCenter = new(0, 0, 0);
        public Vector3[] BoundingBoxCorners = new Vector3[8];

        public Vector3 Pivot = new(0, 0, 0);
        public Vector3 Position = new(0, 0, 0);
        public Quaternion Rotation = new(0, 0, 0, 1);
        public Vector3 Scale = new(0, 0, 0);

        public void Read(ExtendedBinaryReader reader)
        {
            MeshVersion = reader.Read<int>();
            MeshName = Helpers.CastleStrikeString(reader);
            Pivot = reader.Read<Vector3>();
            Position = reader.Read<Vector3>();
            Rotation = reader.Read<Quaternion>();
            Scale = reader.Read<Vector3>();

            numVerts = reader.Read<int>();
            numVertsNormals = numVerts;
            numFaces = reader.Read<int>();
            numFacesNormals = numFaces;
            numTVerts = reader.Read<int>();
            numTFaces = reader.Read<int>();

            BoundingBoxCenter = reader.Read<Vector3>();
            BoundingBoxCorners = reader.ReadArray<Vector3>(8);

            reader.Skip(16);

            MaterialID = reader.Read<int>();

            reader.Skip(8);
        }

        public void ReadData(ExtendedBinaryReader reader)
        {
            Vertices = new Vector3[numVerts];
            Faces = new Face[numFaces];
            TVertices = new Vector2[numTVerts];
            TFaces = new TFace[numTFaces];
            FaceNormals = new Vector3[numFacesNormals];
            VerticesNormals = new Vector3[numVertsNormals];

            for (int j = 0; j < numVerts; j++)
                Vertices[j] = reader.Read<Vector3>();
            for (int j = 0; j < numFaces; j++)
                Faces[j] = reader.Read<Face>();
            for (int j = 0; j < numTVerts; j++)
                TVertices[j] = reader.Read<Vector2>();
            for (int j = 0; j < numTFaces; j++)
            {
                TFaces[j] = reader.Read<TFace>();
                reader.Skip(4);
            }
            for (int j = 0; j < numFacesNormals; j++)
                FaceNormals[j] = reader.Read<Vector3>();
            for (int j = 0; j < numVertsNormals; j++)
                VerticesNormals[j] = reader.Read<Vector3>();
        }


        public void Write(ExtendedBinaryWriter writer)
        {
            writer.Write(MeshVersion);
            writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, MeshName, 256);
            writer.Write(Pivot);
            writer.Write(Position);
            writer.Write(Rotation);
            writer.Write(Scale);
            writer.Write(Vertices.Length);
            writer.Write(Faces.Length);
            writer.Write(TVertices.Length);
            writer.Write(TFaces.Length);
            writer.Write(BoundingBoxCenter);
            writer.WriteArray(BoundingBoxCorners);
            writer.Skip(16);
            writer.Write(MaterialID);
            writer.Skip(8);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            writer.WriteArray(Vertices);
            writer.WriteArray(Faces);
            writer.WriteArray(TVertices);
            foreach (var i in TFaces)
            {
                writer.Write(i);
                writer.Write(-1);
            }
            writer.WriteArray(FaceNormals);
            writer.WriteArray(VerticesNormals);
        }
    }

    public class Bone : IExtendedBinarySerializable
    {
        public string Name = "";
        public Vector3 Position = new(0, 0, 0);
        int NumChilds = 0;
        public int[] IndexOfChild = new int[0];
        int NumObjects = 0;
        public BoneWeight[] BoneWeights = new BoneWeight[0];

        public void Read(ExtendedBinaryReader reader)
        {
            Name = Helpers.CastleStrikeString(reader);
            Position = reader.Read<Vector3>();
            NumChilds = reader.Read<int>();
            NumObjects = reader.Read<int>();
            reader.Skip(4);
        }

        public void ReadData(ExtendedBinaryReader reader)
        {
            IndexOfChild = new int[NumChilds];
            for (int j = 0; j < NumChilds; j++)
                IndexOfChild[j] = reader.Read<int>();
            if (NumObjects > 0)
            {
                reader.Skip(8);
                int amountOfWeights = reader.Read<int>();
                reader.Skip(12);
                BoneWeights = new BoneWeight[amountOfWeights];
                for (int j = 0; j < amountOfWeights; j++)
                {
                    BoneWeight tempWeight = new();
                    tempWeight.ID = reader.Read<int>();
                    BoneWeights[j] = tempWeight;
                }
                for (int j = 0; j < amountOfWeights; j++)
                {
                    BoneWeight tempWeight = BoneWeights[j];
                    tempWeight.Weight = reader.Read<float>();
                    BoneWeights[j] = tempWeight;
                }
            }
        }


        public void Write(ExtendedBinaryWriter writer)
        {
            writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, Name, 256);
            writer.Write(Position);
            writer.Write(IndexOfChild.Length);
            if (BoneWeights.Length > 0)
            {
                writer.Write(1);
                writer.Write(1);
            }
            else
            {
                writer.Write(0);
                writer.Write(0);
            }

        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            writer.WriteArray(IndexOfChild);

            if (BoneWeights.Length > 0)
            {
                writer.Skip(8);
                writer.Write(BoneWeights.Length);
                writer.Skip(12);
                foreach (var i in BoneWeights)
                    writer.Write(i.ID);
                foreach (var i in BoneWeights)
                    writer.Write(i.Weight);
            }
        }
    }

    public class Material : IExtendedBinarySerializable
    {
        public string Name = "";
        public ColorStruct Ambient;
        public ColorStruct Diffuse;
        public ColorStruct Specular;
        public float Transparency = 0;
        public Vector2 UVTilling = new(0, 0);
        public Vector2 UVOffset = new(0, 0);
        public string TextureName = "";

        public void Read(ExtendedBinaryReader reader)
        {
            Name = Helpers.CastleStrikeString(reader);
            Ambient = reader.Read<ColorStruct>();
            Diffuse = reader.Read<ColorStruct>();
            Specular = reader.Read<ColorStruct>();
            Transparency = reader.Read<float>();
            UVTilling = reader.Read<Vector2>();
            UVOffset = reader.Read<Vector2>();
            TextureName = Helpers.CastleStrikeString(reader);
            reader.Skip(4);
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, Name, 256);
            writer.Write(Ambient);
            writer.Write(Diffuse);
            writer.Write(Specular);
            writer.Write(Transparency);
            writer.Write(UVTilling);
            writer.Write(UVOffset);
            writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, TextureName, 256);
            writer.Write(0);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }


    public struct Face
    {
        public int a { get; set; }
        public int b { get; set; }
        public int c { get; set; }
        public int MATID { get; set; }
    }

    public struct ColorStruct
    {
        public float R { get; set; }
        public float G { get; set; }
        public float B { get; set; }
    }

    public struct TFace
    {
        public int a { get; set; }
        public int b { get; set; }
        public int c { get; set; }
    }

    public struct BoneWeight
    {
        public int ID { get; set; }
        public float Weight { get; set; }
    }
}