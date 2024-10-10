using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Numerics;

namespace AshDumpLib.HedgehogEngine.BINA.Animation;

public class ParticleLocator : IFile
{
    public const string FileExtension = ".effdb";
    public const string BINASignature = "FENA";

    public int Version = 256;
    public List<State> States = new();

    public ParticleLocator() { }

    public ParticleLocator(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        long stateCount = reader.Read<long>();
        long stateOffset = reader.Read<long>();
        reader.ReadAtOffset(stateOffset + 64, () =>
        {
            for(int i = 0; i < stateCount; i++)
            {
                State state = new();
                state.Read(reader);
                States.Add(state);
            }
        });
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.Write((long)States.Count);
        writer.AddOffset("stateOffset");
        writer.SetOffset("stateOffset");
        foreach(var i in States)
            i.Write(writer);
        foreach (var i in States)
            i.FinishWrite(writer);
        foreach (var i in States)
            i.FinishWrite2(writer);
        writer.FinishWrite();
        writer.Dispose();
    }


    public class State : IBINASerializable
    {
        public string StateName = "";
        public List<Particle> Particles = new();
        public List<string> ParticleNames = new();

        public void Read(BINAReader reader)
        {
            StateName = reader.ReadStringTableEntry();
            long particleCount = reader.Read<long>();
            long particleOffset = reader.Read<long>();
            reader.ReadAtOffset(particleOffset + 64, () =>
            {
                for(int i = 0; i < particleCount; i++)
                {
                    Particle particle = new();
                    particle.Read(reader);
                    Particles.Add(particle);
                }
            });
            long particleNameCount = reader.Read<long>();
            long particleNameOffset = reader.Read<long>();
            reader.ReadAtOffset(particleNameOffset + 64, () =>
            {
                for (int i = 0; i < particleNameCount; i++)
                    ParticleNames.Add(reader.ReadStringTableEntry());
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(StateName);
            writer.Write((long)Particles.Count);
            if (Particles.Count > 0)
                writer.AddOffset(StateName + "particles" + Particles.Count.ToString() + ParticleNames.Count.ToString());
            else
                writer.WriteNulls(8);
            writer.Write((long)ParticleNames.Count);
            if (ParticleNames.Count > 0)
                writer.AddOffset(StateName + "names" + ParticleNames.Count.ToString() + Particles.Count.ToString());
            else
                writer.WriteNulls(8);
        }

        public void FinishWrite(BINAWriter writer)
        {
            if(Particles.Count > 0)
            {
                writer.Align(16);
                writer.SetOffset(StateName + "particles" + Particles.Count.ToString() + ParticleNames.Count.ToString());
                foreach (var i in Particles)
                    i.Write(writer);
            }
        }

        public void FinishWrite2(BINAWriter writer)
        {
            if(ParticleNames.Count > 0)
            {
                writer.Align(16);
                writer.SetOffset(StateName + "names" + ParticleNames.Count.ToString() + Particles.Count.ToString());
                foreach (var i in ParticleNames)
                    writer.WriteStringTableEntry(i);
            }
        }

        public class Particle : IBINASerializable
        {
            public byte Flag0 = 0;
            public byte Flag1 = 0;
            public byte Flag2 = 0;
            public byte Flag3 = 0;
            public int Unk0 = 0;
            public string ParticleName = "";
            public string BoneName = "";
            public int Unk1 = 0;
            public Vector3 Position = new(0, 0, 0);
            public long Unk2 = 0;
            public Quaternion Rotation = new(0, 0, 0, 1);
            public Vector3 Scale = new(1, 1, 1);
            public int Unk3 = 0;

            public void Read(BINAReader reader)
            {
                Flag0 = reader.Read<byte>();
                Flag1 = reader.Read<byte>();
                Flag2 = reader.Read<byte>();
                Flag3 = reader.Read<byte>();
                Unk0 = reader.Read<int>();
                ParticleName = reader.ReadStringTableEntry();
                BoneName = reader.ReadStringTableEntry();
                Unk1 = reader.Read<int>();
                Position = reader.Read<Vector3>();
                Unk2 = reader.Read<long>();
                Rotation = reader.Read<Quaternion>();
                Scale = reader.Read<Vector3>();
                Unk3 = reader.Read<int>();
            }

            public void Write(BINAWriter writer)
            {
                writer.Write(Flag0);
                writer.Write(Flag1);
                writer.Write(Flag2);
                writer.Write(Flag3);
                writer.Write(Unk0);
                writer.WriteStringTableEntry(ParticleName);
                writer.WriteStringTableEntry(BoneName);
                writer.Write(Unk1);
                writer.Write(Position);
                writer.Write(Unk2);
                writer.Write(Rotation);
                writer.Write(Scale);
                writer.Write(Unk3);
            }

            public void FinishWrite(BINAWriter writer)
            {
                throw new NotImplementedException();
            }
        }
    }
}