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
    public List<RigidBody> RigidBodies = new();
    public List<Constraint> Constraints = new();
    public List<SoftBody> SoftBodies = new();

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
        int rigidBodyCount = reader.Read<int>();
        int constraintCount = reader.Read<int>();
        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
        {
            for (int i = 0; i < rigidBodyCount; i++)
            {
                RigidBody rb = new();
                rb.Read(reader);
                RigidBodies.Add(rb);
            }
        });
        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
        {
            for (int i = 0; i < constraintCount; i++)
            {
                Constraint co = new();
                co.Read(reader);
                Constraints.Add(co);
            }
        });
        int softBodyCount = reader.Read<int>();
        reader.Align(8);
        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
        {
            for (int i = 0; i < softBodyCount; i++)
            {
                SoftBody sb = new();
                sb.Read(reader);
                SoftBodies.Add(sb);
            }
        });
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.WriteStringTableEntry(SkeletonName);
        writer.Write(RigidBodies.Count);
        writer.Write(Constraints.Count);
        if(RigidBodies.Count > 0)
            writer.AddOffset("rigidbodies");
        else
            writer.Write((long)0);
        if (Constraints.Count > 0)
            writer.AddOffset("constraints");
        else
            writer.Write((long)0);
        writer.Write(SoftBodies.Count);
        writer.Align(8);
        if (SoftBodies.Count > 0)
            writer.AddOffset("softbodies");
        else
            writer.Write((long)0);
        writer.Align(16);
        if(RigidBodies.Count > 0)
        {
            writer.SetOffset("rigidbodies");
            foreach (var i in RigidBodies)
                i.Write(writer);
        }
        if(Constraints.Count > 0)
        {
            writer.SetOffset("constraints");
            foreach (var i in Constraints)
                i.Write(writer);
        }
        if(SoftBodies.Count > 0)
        {
            writer.SetOffset("softbodies");
            foreach (var i in SoftBodies)
                i.Write(writer);
            foreach (var i in SoftBodies)
            {
                writer.SetOffset(i.Name + i.Scale + i.DragCoeff + i.Nodes.Count + "nodes");
                foreach (var x in i.Nodes)
                    x.Write(writer);
            }
            foreach (var i in SoftBodies)
            {
                writer.SetOffset(i.Name + i.Scale + i.DragCoeff + i.Links.Count + "links");
                foreach (var x in i.Links)
                    writer.Write(x);
            }
        }
        writer.FinishWrite();
        writer.Dispose();
    }


    public class RigidBody : IBINASerializable
    {
        public string BoneName = "";
        public bool IsStaticObject = true;
        public bool IsBox = false;
        public float ShapeRadius = 0.0f;
        public float ShapeHeight = 0.0f;
        public float ShapeDepth = 0.0f;
        public float Mass = 0.0f;
        public float Friction = 0.0f;
        public float Resitution = 0.0f;
        public float LinearDamping = 0.0f;
        public float AngularDamping = 0.0f;
        public Vector3 OffsetPosition = new();
        public Vector3 OffsetRotation = new();

        public void Read(BINAReader reader)
        {
            reader.Align(16);
            BoneName = reader.ReadStringTableEntry();
            IsStaticObject = reader.Read<bool>();
            IsBox = reader.Read<bool>();
            reader.Align(4);
            ShapeRadius = reader.Read<float>();
            ShapeHeight = reader.Read<float>();
            ShapeDepth = reader.Read<float>();
            Mass = reader.Read<float>();
            Friction = reader.Read<float>();
            Resitution = reader.Read<float>();
            LinearDamping = reader.Read<float>();
            AngularDamping = reader.Read<float>();
            reader.Align(16);
            OffsetPosition = reader.Read<Vector3>();
            reader.Align(16);
            OffsetRotation = Helpers.ToEulerAngles(reader.Read<Quaternion>());
            reader.Align(16);
        }

        public void Write(BINAWriter writer)
        {
            writer.Align(16);
            writer.WriteStringTableEntry(BoneName);
            writer.Write(IsStaticObject);
            writer.Write(IsBox);
            writer.Align(4);
            writer.Write(ShapeRadius);
            writer.Write(ShapeHeight);
            writer.Write(ShapeDepth);
            writer.Write(Mass);
            writer.Write(Friction);
            writer.Write(Resitution);
            writer.Write(LinearDamping);
            writer.Write(AngularDamping);
            writer.Align(16);
            writer.Write(OffsetPosition);
            writer.Align(16);
            writer.Write(Helpers.ToQuaternion(OffsetRotation));
            writer.Align(16);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class Constraint : IBINASerializable
    {
        public string BoneName = "";
        public byte Unk0 = 1;
        public byte Unk1 = 1;
        public short IterationCount = 20;
        public short LocalParentBoneIndex = -1;
        public short LocalBoneIndex = -1;
        public short SKLParentBoneIndex = 0;
        public Limit[] AngularLimits = new Limit[3];
        public Limit[] LinearLimits = new Limit[3];
        public Vector3 OffsetPositionA = new();
        public Vector3 OffsetRotationA = new();
        public Vector3 OffsetPositionB = new();
        public Vector3 OffsetRotationB = new();
        
        public void Read(BINAReader reader)
        {
            reader.Align(16);
            BoneName = reader.ReadStringTableEntry();
            Unk0 = reader.Read<byte>();
            Unk1 = reader.Read<byte>();
            IterationCount = reader.Read<short>();
            LocalParentBoneIndex = reader.Read<short>();
            LocalBoneIndex = reader.Read<short>();
            SKLParentBoneIndex = reader.Read<short>();
            reader.Align(4);
            AngularLimits = reader.ReadArray<Limit>(3);
            LinearLimits = reader.ReadArray<Limit>(3);
            reader.Align(16);
            OffsetPositionA = reader.Read<Vector3>();
            reader.Align(16);
            OffsetRotationA = Helpers.ToEulerAngles(reader.Read<Quaternion>());
            reader.Align(16);
            OffsetPositionB = reader.Read<Vector3>();
            reader.Align(16);
            OffsetRotationB = Helpers.ToEulerAngles(reader.Read<Quaternion>());
            reader.Align(16);
        }

        public void Write(BINAWriter writer)
        {
            writer.Align(16);
            writer.WriteStringTableEntry(BoneName);
            writer.Write(Unk0);
            writer.Write(Unk1);
            writer.Write(IterationCount);
            writer.Write(LocalParentBoneIndex); 
            writer.Write(LocalBoneIndex); 
            writer.Write(SKLParentBoneIndex);
            writer.Align(4);
            writer.WriteArray(AngularLimits);
            writer.WriteArray(LinearLimits);
            writer.Align(16);
            writer.Write(OffsetPositionA);
            writer.Align(16);
            writer.Write(Helpers.ToQuaternion(OffsetRotationA));
            writer.Align(16);
            writer.Write(OffsetPositionB);
            writer.Align(16);
            writer.Write(Helpers.ToQuaternion(OffsetRotationB));
            writer.Align(16);
        }

        public void FinishWrite(BINAWriter writer)
        {
            throw new NotImplementedException();
        }

        public struct Limit
        {
            public enum LimitMode : byte
            {
                None,
                Enabled,
                Disabled
            }

            public LimitMode Mode;
            public bool Enabled;
            private short align;
            public float LowLimit;
            public float HighLimit;
            public float Stiffness;
            public float Damping;
        }
    }

    public class SoftBody : IBINASerializable
    {
        public string Name = "";
        public float Scale = 1.0f;
        public float DampingCoeff = 0.0f;
        public float DragCoeff = 0.0f;
        public float LiftCoeff = 0.0f;
        public float DynamicFrictionCoeff = 0.0f;
        public float PoseMatchingCoeff = 0.0f;
        public float RigidContactCoeff = 0.0f;
        public float KineticContactsHardness = 0.0f;
        public float SoftContactsHardness = 0.0f;
        public float AnchorsHardness = 0.0f;
        public byte PositionIterationCount = 0;
        public byte Unk0 = 0;
        public short Unk1 = 0;
        public List<Node> Nodes = new();
        public List<Link> Links = new();

        public void Read(BINAReader reader)
        {
            reader.Align(8);
            Name = reader.ReadStringTableEntry();
            Scale = reader.Read<float>();
            DampingCoeff = reader.Read<float>();
            DragCoeff = reader.Read<float>();
            LiftCoeff = reader.Read<float>();
            DynamicFrictionCoeff = reader.Read<float>();
            PoseMatchingCoeff = reader.Read<float>();
            RigidContactCoeff = reader.Read<float>();
            KineticContactsHardness = reader.Read<float>();
            SoftContactsHardness = reader.Read<float>();
            AnchorsHardness = reader.Read<float>();
            PositionIterationCount = reader.Read<byte>();
            Unk0 = reader.Read<byte>();
            Unk1 = reader.Read<short>();
            reader.Align(4);
            int nodeCount = reader.Read<int>();
            int linkCount = reader.Read<int>();
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
                Links = reader.ReadArray<Link>(linkCount).ToList();
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.Align(8);
            writer.WriteStringTableEntry(Name);
            writer.Write(Scale);
            writer.Write(DampingCoeff);
            writer.Write(DragCoeff);
            writer.Write(LiftCoeff);
            writer.Write(DynamicFrictionCoeff);
            writer.Write(PoseMatchingCoeff);
            writer.Write(RigidContactCoeff);
            writer.Write(KineticContactsHardness);
            writer.Write(SoftContactsHardness);
            writer.Write(AnchorsHardness);
            writer.Write(PositionIterationCount);
            writer.Write(Unk0);
            writer.Write(Unk1);
            writer.Align(4);
            writer.Write(Nodes.Count);
            writer.Write(Links.Count);
            writer.Align(8);
            writer.AddOffset(Name + Scale + DragCoeff + Nodes.Count + "nodes");
            writer.AddOffset(Name + Scale + DragCoeff + Links.Count + "links");
        }

        public void FinishWrite(BINAWriter writer)
        {

        }

        public class Node : IBINASerializable
        {
            public string BoneName = "";
            public float Mass = 1f;
            public short Unk0 = -1;
            public bool IsPinned = false;
            public short ChildID = -1;
            public short ParentID = -1;
            public short Field14 = -1;
            public short Field18 = -1;
            public short SiblingLeftID = -1;
            public short SiblingRightID = -1;

            public void Read(BINAReader reader)
            {
                reader.Align(8);
                BoneName = reader.ReadStringTableEntry();
                Mass = reader.Read<float>();
                Unk0 = reader.Read<short>();
                IsPinned = reader.Read<bool>();
                reader.Align(2);
                ChildID = reader.Read<short>();
                ParentID = reader.Read<short>();
                Field14 = reader.Read<short>();
                Field18 = reader.Read<short>();
                SiblingLeftID = reader.Read<short>();
                SiblingRightID = reader.Read<short>();
            }

            public void Write(BINAWriter writer)
            {
                writer.Align(8);
                writer.WriteStringTableEntry(BoneName);
                writer.Write(Mass);
                writer.Write(Unk0);
                writer.Write(IsPinned);
                writer.Align(2);
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

        public struct Link
        {
            public short VertA;
            public short VertB;
            public float RestLength;
            public float Stiffness;
        }
    }
}