using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA.Misc;

// Alot of research done by Adel!

public class PhysicalSkeleton : IFile
{
    public const string FileExtension = ".pba";
    public const string BINASignature = "PBA ";

    public int Version = 1;
    public string SkeletonName = "";
    public List<Collision> Collisions = new();
    public List<Jiggle> Jiggles = new();
    public List<Cloth> Clothes = new();

    public PhysicalSkeleton() { }

    public PhysicalSkeleton(string filename) => Open(filename);
    public PhysicalSkeleton(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        SkeletonName = reader.ReadStringTableEntry();
        int collisionCount = reader.Read<int>();
        int jiggleCount = reader.Read<int>();
        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
        {
            for (int i = 0; i < collisionCount; i++)
            {
                Collision coll = new();
                coll.Read(reader);
                Collisions.Add(coll);
            }
        });
        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
        {
            for (int i = 0; i < jiggleCount; i++)
            {
                Jiggle jigg = new();
                jigg.Read(reader);
                Jiggles.Add(jigg);
            }
        });
        int clothCount = reader.Read<int>();
        int unkCount = reader.Read<int>();
        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
        {
            for (int i = 0; i < clothCount; i++)
            {
                Cloth clo = new();
                clo.Read(reader);
                Clothes.Add(clo);
            }
        });
        long unkPtr = reader.Read<long>();
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.WriteStringTableEntry(SkeletonName);
        writer.Write(Collisions.Count);
        writer.Write(Jiggles.Count);
        if(Collisions.Count > 0)
            writer.AddOffset("collisions");
        else
            writer.Write((long)0);
        if (Jiggles.Count > 0)
            writer.AddOffset("jiggles");
        else
            writer.Write((long)0);
        writer.Write(Clothes.Count);
        writer.Write(0);
        if (Clothes.Count > 0)
            writer.AddOffset("clothes");
        else
            writer.Write((long)0);
        writer.Write((long)0);
        if(Collisions.Count > 0)
        {
            writer.SetOffset("collisions");
            foreach (var i in Collisions)
                i.Write(writer);
        }
        if(Jiggles.Count > 0)
        {
            writer.SetOffset("jiggles");
            foreach (var i in Jiggles)
                i.Write(writer);
        }
        if(Clothes.Count > 0)
        {
            writer.SetOffset("clothes");
            foreach (var i in Clothes)
                i.Write(writer);
            foreach (var i in Clothes)
            {
                writer.SetOffset(i.ClothName + i.Field28 + i.Field2A + i.Nodes.Count + "nodes");
                foreach (var x in i.Nodes)
                    x.Write(writer);
            }
            foreach (var i in Clothes)
            {
                writer.SetOffset(i.ClothName + i.Field28 + i.Field2A + i.Edges.Count + "edges");
                foreach (var x in i.Edges)
                    writer.Write(x);
            }
        }
        writer.FinishWrite();
        writer.Dispose();
    }


    public class Collision : IBINASerializable
    {
        public string BoneName = "";
        public int Field08 = 1;
        public float Field0C = 1f;
        public Matrix4x4 Matrix = Matrix4x4.Identity;

        public void Read(BINAReader reader)
        {
            BoneName = reader.ReadStringTableEntry();
            Field08 = reader.Read<int>();
            Field0C = reader.Read<float>();
            Matrix = reader.Read<Matrix4x4>();
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(BoneName);
            writer.Write(Field08);
            writer.Write(Field0C);
            writer.Write(Matrix);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class Jiggle : IBINASerializable
    {
        public string BoneName = "";
        public byte[] Flags = new byte[4] { 0x1, 0x1, 0x14, 0x0 };
        public short LocalParentBoneIndex = -1;
        public short LocalBoneIndex = -1;
        public short SKLParentBoneIndex = 0;
        public Limit[] Limits = new Limit[6];
        public Matrix4x4 Matrix = Matrix4x4.Identity;
        
        public void Read(BINAReader reader)
        {
            BoneName = reader.ReadStringTableEntry();
            Flags = reader.ReadArray<byte>(4);
            LocalParentBoneIndex = reader.Read<short>();
            LocalBoneIndex = reader.Read<short>();
            SKLParentBoneIndex = reader.Read<short>();
            reader.Align(4);
            Limits = reader.ReadArray<Limit>(6);
            reader.Align(16);
            Matrix = reader.Read<Matrix4x4>();
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(BoneName);
            writer.WriteArray(Flags);
            writer.Write(LocalParentBoneIndex); 
            writer.Write(LocalBoneIndex); 
            writer.Write(SKLParentBoneIndex);
            writer.Align(4);
            writer.WriteArray(Limits);
            writer.Align(16);
            writer.Write(Matrix);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }

        public struct Limit
        {
            public byte Field00;
            public byte Field01;
            public byte Field02;
            public byte Field03;
            public float Field04;
            public float Field08;
            public float Field0C;
            public float Field10;
        }
    }

    public class Cloth : IBINASerializable
    {
        public string ClothName = "";
        public float[] Field08 = new float[10];
        public short Field28 = 0;
        public short Field2A = 0;
        public List<Node> Nodes = new();
        public List<Edge> Edges = new();

        public void Read(BINAReader reader)
        {
            ClothName = reader.ReadStringTableEntry();
            Field08 = reader.ReadArray<float>(10);
            Field28 = reader.Read<short>();
            Field2A = reader.Read<short>();
            int nodeCount = reader.Read<int>();
            int edgeCount = reader.Read<int>();
            reader.Align(8);
            reader.ReadAtOffset(reader.Read<long>() + 64, () =>
            {
                for(int i = 0; i < nodeCount; i++)
                {
                    Node node = new();
                    node.Read(reader);
                    Nodes.Add(node);
                }
            });
            reader.ReadAtOffset(reader.Read<long>() + 64, () =>
            {
                Edges = reader.ReadArray<Edge>(edgeCount).ToList();
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(ClothName);
            writer.WriteArray(Field08);
            writer.Write(Field28);
            writer.Write(Field2A);
            writer.Write(Nodes.Count);
            writer.Write(Edges.Count);
            writer.Align(8);
            writer.AddOffset(ClothName + Field28 + Field2A + Nodes.Count + "nodes");
            writer.AddOffset(ClothName + Field28 + Field2A + Edges.Count + "edges");
        }

        public void FinishWrite(BINAWriter writer)
        {

        }

        public class Node : IBINASerializable
        {
            public string BoneName = "";
            public float GravityFactor = 1f;
            public short Field0C = -1;
            public short IsPinned = 0;
            public short ChildID = -1;
            public short ParentID = -1;
            public short Field14 = -1;
            public short Field18 = -1;
            public short SiblingLeftID = -1;
            public short SiblingRightID = -1;

            public void Read(BINAReader reader)
            {
                reader.Align(16);
                BoneName = reader.ReadStringTableEntry();
                GravityFactor = reader.Read<float>();
                Field0C = reader.Read<short>();
                IsPinned = reader.Read<short>();
                ChildID = reader.Read<short>();
                ParentID = reader.Read<short>();
                Field14 = reader.Read<short>();
                Field18 = reader.Read<short>();
                SiblingLeftID = reader.Read<short>();
                SiblingRightID = reader.Read<short>();
            }

            public void Write(BINAWriter writer)
            {
                writer.Align(16);
                writer.WriteStringTableEntry(BoneName);
                writer.Write(GravityFactor);
                writer.Write(Field0C);
                writer.Write(IsPinned);
                writer.Write(ChildID);
                writer.Write(ParentID);
                writer.Write(Field14);
                writer.Write(Field18);
                writer.Write(SiblingLeftID);
                writer.Write(SiblingRightID);
            }

            public void FinishWrite(BINAWriter writer)
            {
                throw new NotImplementedException();
            }
        }

        public struct Edge
        {
            public short ID_A;
            public short ID_B;
            public float Field04;
            public float Field08;
        }
    }
}