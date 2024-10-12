using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Drawing;
using System.Numerics;
using System;

namespace AshDumpLib.HedgehogEngine.BINA.Animation;

// Original research by ik-01, finished by angryzor!

public class Animator : IFile
{
    public const string FileExtension = ".asm";
    public const string BINASignature = "FMSA";

    public int Version = 259;
    public List<Clip> Clips = new();
    public List<State> States = new();
    public List<BlendNode> BlendNodes = new();
    public List<Event> Events = new();
    public List<TransitionArray> TransitionArrays = new();
    public List<Transition> Transitions = new();
    public Transition NullTransition = new();
    public List<short> FlagIndices = new();
    public List<string> Flags = new();
    public List<string> Variables = new();
    public List<Layer> Layers = new();
    public List<BlendMask> BlendMasks = new();
    public List<string> MaskBones = new();
    public List<Trigger> Triggers = new();
    public List<string> TriggerTypes = new();
    public List<string> Colliders = new();
    public short BlendTreeRootNodeID = 0;
    public List<BlendSpace> BlendSpaces = new();
    public List<short> ChildClipIndices = new();

    public Animator() { }

    public Animator(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        Clips = reader.ReadBINAArrayStruct<Clip>();
        States = reader.ReadBINAArrayStruct<State>();
        BlendNodes = reader.ReadBINAArrayStruct<BlendNode>();
        Events = reader.ReadBINAArrayStruct<Event>();
        TransitionArrays = reader.ReadBINAArrayStruct<TransitionArray>();
        Transitions = reader.ReadBINAArrayStruct<Transition>();
        NullTransition.Read(reader);
        FlagIndices = reader.ReadBINAArray<short>();
        Flags = reader.ReadBINAStringArray();
        Variables = reader.ReadBINAStringArray();
        Layers = reader.ReadBINAArrayStruct<Layer>();
        BlendMasks = reader.ReadBINAArrayStruct<BlendMask>();
        MaskBones = reader.ReadBINAStringArray();
        Triggers = reader.ReadBINAArrayStruct<Trigger>();
        TriggerTypes = reader.ReadBINAStringArray();
        Colliders = reader.ReadBINAStringArray();
        BlendTreeRootNodeID = reader.Read<short>();
        reader.Align(4);
        BlendSpaces = reader.ReadBINAArrayStruct<BlendSpace>();
        ChildClipIndices = reader.ReadBINAArray<short>();
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.WriteBINAArray(Clips, "clipsArray");
        writer.WriteBINAArray(States, "statesArray");
        writer.WriteBINAArray(BlendNodes, "blendNodesArray");
        writer.WriteBINAArray(Events, "eventsArray");
        writer.WriteBINAArray(TransitionArrays, "transitionArraysArray");
        writer.WriteBINAArray(Transitions, "transitionsArray");
        Console.WriteLine(writer.Position);
        NullTransition.Write(writer);
        Console.WriteLine(writer.Position);
        writer.WriteBINAArray(FlagIndices, "flagIndicesArray");
        writer.WriteBINAArray(Flags, "flagsArray");
        writer.WriteBINAArray(Variables, "variablesArray");
        writer.WriteBINAArray(Layers, "layersArray");
        writer.WriteBINAArray(BlendMasks, "blendMasksArray");
        writer.WriteBINAArray(MaskBones, "maskBonesArray");
        writer.WriteBINAArray(Triggers, "triggersArray");
        writer.WriteBINAArray(TriggerTypes, "triggerTypesArray");
        writer.WriteBINAArray(Colliders, "collidersArray");
        writer.Write(BlendTreeRootNodeID);
        writer.Align(4);
        writer.WriteBINAArray(BlendSpaces, "blendSpacesArray");
        writer.WriteBINAArray(ChildClipIndices, "childClipIndices");
        writer.FinishWrite();
        writer.Dispose();
    }

    public class Clip : IBINASerializable
    {
        public string Name = "";
        public AnimationSettings Settings = new();
        public ushort TriggerCount = 0;
        public short TriggerOffset = 0;
        public short BlendMaskIndex;
        public ushort ChildClipIndexCount;
        public ushort ChildClipIndexOffset;

        public Clip() { }

        public void Read(BINAReader reader)
        {
            Name = reader.ReadStringTableEntry();
            Settings.ResourceName = reader.ReadStringTableEntry();
            Settings.Start = reader.Read<float>();
            Settings.End = reader.Read<float>();
            Settings.Speed = reader.Read<float>();
            Settings.Flags = reader.Read<byte>();
            Settings.Loop = reader.Read<byte>() == 1;
            reader.Align(4);
            TriggerCount = reader.Read<ushort>();
            TriggerOffset = reader.Read<short>();
            BlendMaskIndex = reader.Read<short>();
            ChildClipIndexCount = reader.Read<ushort>();
            ChildClipIndexOffset = reader.Read<ushort>();
            reader.Align(8);
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(Name);
            writer.WriteStringTableEntry(Settings.ResourceName);
            writer.Write(Settings.Start);
            writer.Write(Settings.End);
            writer.Write(Settings.Speed);
            writer.Write(Settings.Flags);
            writer.Write(Settings.Loop ? (byte)1 : (byte)0);
            writer.Align(4);
            writer.Write(TriggerCount);
            writer.Write(TriggerOffset);
            writer.Write(BlendMaskIndex);
            writer.Write(ChildClipIndexCount);
            writer.Write(ChildClipIndexOffset);
            writer.Align(8);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }

        public struct AnimationSettings
        {
            public enum Flag : byte
            {
                Mirror,
                PlayUntilAnimationEnd,
                NoAnimationResolution
            }

            public string ResourceName = "";
            public float Start = 0;
            public float End = 0;
            public float Speed = 1;
            public byte Flags = 0;
            public bool Loop = false;

            public AnimationSettings()
            {
            }
        }
    }

    public class State : IBINASerializable
    {
        public enum StateType : byte
        {
            NullState = 255,
            Clip = 0,
            BlendTree,
            None,
        }

        public enum Flag : byte
        {
            Loops,
            Unk1
        }

        public string Name = "";
        public StateType Type = StateType.NullState;
        public bool TransitImmediately = false;
        public byte Flags = 0;
        public byte DefaultLayerIndex = 0;
        public short RootBlendNodeOrClipIndex = 0;
        public short MaxCycles = 0;
        public float Speed = 1;
        public short SpeedVariableIndex = 0;
        public short EventCount = 0;
        public short EventOffset = 0;
        public short TransitionArrayIndex = 0;
        public Transition TransitionData = new();
        public short FlagIndexCount = 0;
        public short FlagIndexOffset = 0;

        public State() { }

        public void Read(BINAReader reader)
        {
            Name = reader.ReadStringTableEntry();
            Type = reader.Read<StateType>();
            TransitImmediately = reader.Read<byte>() == 1;
            Flags = reader.Read<byte>();
            DefaultLayerIndex = reader.Read<byte>();
            RootBlendNodeOrClipIndex = reader.Read<short>();
            MaxCycles = reader.Read<short>();
            Speed = reader.Read<float>();
            SpeedVariableIndex = reader.Read<short>();
            EventCount = reader.Read<short>();
            EventOffset = reader.Read<short>();
            TransitionArrayIndex = reader.Read<short>();
            TransitionData.Read(reader);
            FlagIndexCount = reader.Read<short>();
            FlagIndexOffset = reader.Read<short>();
            reader.Align(8);
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(Name);
            writer.Write(Type);
            writer.Write(TransitImmediately ? (byte)1 : (byte)0);
            writer.Write(Flags);
            writer.Write(DefaultLayerIndex);
            writer.Write(RootBlendNodeOrClipIndex);
            writer.Write(MaxCycles);
            writer.Write(Speed);
            writer.Write(SpeedVariableIndex);
            writer.Write(EventCount);
            writer.Write(EventOffset);
            writer.Write(TransitionArrayIndex);
            TransitionData.Write(writer);
            writer.Write(FlagIndexCount);
            writer.Write(FlagIndexOffset);
            writer.Align(8);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }
    }

    public class BlendNode : IBINASerializable
    {
        public enum BlendNodeType : byte
        {
            Lerp,
            Additive,
            Clip,
            Override,
            Layer,
            Multiply,
            BlendSpace,
            TwoPointLerp
        }

        public BlendNodeType Type = BlendNodeType.Lerp;
        public short BlendSpaceIndex = 0;
        public short VariableIndex = 0;
        public float BlendFactor = 0;
        public short ChildNodeArraySize = 0;
        public short ChildNodeArrayOffset = 0;

        public BlendNode() { }

        public void Read(BINAReader reader)
        {
            Type = reader.Read<BlendNodeType>();
            reader.Align(2);
            BlendSpaceIndex = reader.Read<short>();
            VariableIndex = reader.Read<short>();
            reader.Align(4);
            BlendFactor = reader.Read<float>();
            ChildNodeArraySize = reader.Read<short>();
            ChildNodeArrayOffset = reader.Read<short>();
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(Type);
            writer.Align(2);
            writer.Write(BlendSpaceIndex);
            writer.Write(VariableIndex);
            writer.Align(4);
            writer.Write(BlendFactor);
            writer.Write(ChildNodeArraySize);
            writer.Write(ChildNodeArrayOffset);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }
    }

    public class Event : IBINASerializable
    {
        public string Name = "";
        public Transition Transition = new();

        public Event() { }

        public void Read(BINAReader reader)
        {
            Name = reader.ReadStringTableEntry();
            Transition.Read(reader);
            reader.Align(8);
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(Name);
            Transition.Write(writer);
            writer.Align(8);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }
    }

    public class TransitionArray : IBINASerializable
    {
        public int Offset = 0;
        public int Size = 0;

        public TransitionArray() { }

        public void Read(BINAReader reader)
        {
            Size = reader.Read<int>();
            Offset = reader.Read<int>();
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(Size);
            writer.Write(Offset);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }
    }

    public class Layer : IBINASerializable
    {
        public string Name = "";
        public ushort LayerID = 0;
        public short BlendMaskIndex = 0;

        public Layer() { }

        public void Read(BINAReader reader)
        {
            Name = reader.ReadStringTableEntry();
            LayerID = reader.Read<ushort>();
            BlendMaskIndex = reader.Read<short>();
            reader.Align(8);
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(Name);
            writer.Write(LayerID);
            writer.Write(BlendMaskIndex);
            writer.Align(8);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }
    }

    public class BlendMask : IBINASerializable
    {
        public string Name = "";
        public short MaskBoneCount = 0;
        public short MaskBoneOffset = 0;

        public BlendMask() { }

        public void Read(BINAReader reader)
        {
            Name = reader.ReadStringTableEntry();
            MaskBoneCount = reader.Read<short>();
            MaskBoneOffset = reader.Read<short>();
            reader.Align(8);
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(Name);
            writer.Write(MaskBoneCount);
            writer.Write(MaskBoneOffset);
            writer.Align(8);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }
    }

    public class Trigger : IBINASerializable
    {
        public enum TriggerType : byte
        {
            Hit,
            EnterLeave
        }

        public TriggerType Type = TriggerType.Hit;
        public float Unk0 = 0;
        public float Unk1 = 0;
        public ushort TriggerTypeIndex = 0;
        public short ColliderIndex = 0;
        public string Name = "";

        public Trigger() { }

        public void Read(BINAReader reader)
        {
            Type = reader.Read<TriggerType>();
            reader.Align(4);
            Unk0 = reader.Read<float>();
            Unk1 = reader.Read<float>();
            TriggerTypeIndex = reader.Read<ushort>();
            ColliderIndex = reader.Read<short>();
            Name = reader.ReadStringTableEntry();
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(Type);
            writer.Align(4);
            writer.Write(Unk0);
            writer.Write(Unk1);
            writer.Write(TriggerTypeIndex);
            writer.Write(ColliderIndex);
            writer.WriteStringTableEntry(Name);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }
    }

    public class BlendSpace : IBINASerializable
    {
        public class BlendSpaceTriangle : IBINASerializable
        {
            public short[] NodeIndices = new short[3] { 0, 0, 0 };
            public short unk = 0;

            public BlendSpaceTriangle()
            {
            }

            public void Read(BINAReader reader)
            {
                NodeIndices = reader.ReadArray<short>(3);
                unk = reader.Read<short>();
            }

            public void Write(BINAWriter writer)
            {
                writer.WriteArray(NodeIndices);
                writer.Write(unk);
            }

            public void FinishWrite(BINAWriter writer)
            {
                
            }
        }

        public Point VariableIndex = new();
        public Vector2 Min = new();
        public Vector2 Max = new();
        public List<Vector2> Nodes = new();
        public List<short> ClipIndices = new();
        public List<BlendSpaceTriangle> Triangles = new();
        long id = 0;

        public BlendSpace() { }

        public void Read(BINAReader reader)
        {
            VariableIndex.X = reader.Read<short>();
            VariableIndex.Y = reader.Read<short>();
            Min.X = reader.Read<float>();
            Max.X = reader.Read<float>();
            Min.Y = reader.Read<float>();
            Max.Y = reader.Read<float>();
            ushort nodeCount = reader.Read<ushort>();
            ushort triangleCount = reader.Read<ushort>();
            long nodeOffset = reader.Read<long>();
            long clipIndiceOffset = reader.Read<long>();
            long triangleOffset = reader.Read<long>();
            Nodes.AddRange(reader.ReadArrayAtOffset<Vector2>(nodeOffset + 64, nodeCount));
            ClipIndices.AddRange(reader.ReadArrayAtOffset<short>(clipIndiceOffset + 64, nodeCount));
            reader.ReadAtOffset(triangleOffset + 64, () =>
            {
                for (int i = 0; i < triangleCount; i++)
                {
                    BlendSpaceTriangle tri = new();
                    tri.Read(reader);
                    Triangles.Add(tri);
                }
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.Write((short)VariableIndex.X);
            writer.Write((short)VariableIndex.Y);
            writer.Write(Min.X);
            writer.Write(Max.X);
            writer.Write(Min.Y);
            writer.Write(Max.Y);
            writer.Write((ushort)Nodes.Count);
            writer.Write((ushort)Triangles.Count);
            id = Random.Shared.NextInt64();
            writer.AddOffset("nodes" + id);
            writer.AddOffset("clipIndices" + id);
            writer.AddOffset("triangles" + id);
        }

        public void FinishWrite(BINAWriter writer)
        {
            writer.SetOffset("nodes" + id);
            foreach (var i in Nodes)
                writer.Write(i);
            writer.SetOffset("clipIndices" + id);
            foreach (var i in ClipIndices)
                writer.Write(i);
            writer.SetOffset("triangles" + id);
            foreach (var i in Triangles)
                i.Write(writer);
        }
    }

    public class Transition : IBINASerializable
    {
        public TransitionInfo Info = new();
        public short TransitionTimeVariableIndex = 0;


        public void Read(BINAReader reader)
        {
            Info.Type = reader.Read<TransitionInfo.TransitionType>();
            Info.Easing = reader.Read<TransitionInfo.TransitionEasingType>();
            Info.TargetStateIndex = reader.Read<short>();
            Info.TransitionTime = reader.Read<float>();
            TransitionTimeVariableIndex = reader.Read<short>();
            reader.Align(4);
        }

        public void Write(BINAWriter writer)
        {
            writer.Write(Info.Type);
            writer.Write(Info.Easing);
            writer.Write(Info.TargetStateIndex);
            writer.Write(Info.TransitionTime);
            writer.Write(TransitionTimeVariableIndex);
            writer.Align(4);
        }

        public void FinishWrite(BINAWriter writer)
        {
            
        }

        public struct TransitionInfo
        {
            public enum TransitionType : byte
            {
                Immediate,
                Frozen,
                Smooth,
                Synchronize,
                HoldTo,
                HoldBoth,
                WaitFrom,
                WaitFromHoldTo,
                User0,
                User1,
                User2
            }

            public enum TransitionEasingType : byte
            {
                Linear,
                Cubic
            }

            public TransitionType Type = TransitionType.Immediate;
            public TransitionEasingType Easing = TransitionEasingType.Linear;
            public short TargetStateIndex = 0;
            public float TransitionTime = 0;

            public TransitionInfo()
            {
            }
        }
    }
}