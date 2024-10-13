using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine.BINA.Misc;

public class AIStateMachine : IFile
{
    public const string FileExtension = ".aism";

    public List<AIState> AIStates = new();

    public AIStateMachine() { }

    public AIStateMachine(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        AIStates = reader.ReadBINAArrayStruct64<AIState>();
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteBINAArray64(AIStates, "aiStates");
        writer.FinishWrite();
        writer.Dispose();
    }


    public class AIState : IBINASerializable
    {
        public string StateName = "";
        public string ActionName = "";
        public int unk0 = -1;
        public int unk1 = -1;
        public List<State> States = new();
        public List<UnkStateNames> UnkStates = new();

        public void Read(BINAReader reader)
        {
            StateName = reader.ReadStringTableEntry();
            ActionName = reader.ReadStringTableEntry();
            unk0 = reader.Read<int>();
            unk1 = reader.Read<int>();
            States = reader.ReadBINAArrayStruct64<State>();
            UnkStates = reader.ReadBINAArrayStruct64<UnkStateNames>();
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(StateName);
            writer.WriteStringTableEntry(ActionName);
            writer.Write(unk0);
            writer.Write(unk1);
            if (States.Count > 0)
            {
                writer.AddOffset(StateName + ActionName + States.Count);
                writer.Write((long)States.Count);
            }
            else
                writer.WriteNulls(16);
            if (UnkStates.Count > 0)
            {
                writer.AddOffset(ActionName + StateName + UnkStates.Count);
                writer.Write((long)UnkStates.Count);
            }
            else
                writer.WriteNulls(16);
        }

        public void FinishWrite(BINAWriter writer)
        {
            if (States.Count > 0)
            {
                writer.SetOffset(StateName + ActionName + States.Count);
                foreach(var i in States)
                    i.Write(writer);
                foreach(var i in States)
                    i.FinishWrite(writer);
            }
            if (UnkStates.Count > 0)
            {
                writer.SetOffset(ActionName + StateName + UnkStates.Count);
                foreach(var i in UnkStates)
                    i.Write(writer);
            }
        }


        public class State : IBINASerializable
        {
            public string StateName = "";
            public long unk0 = 0;
            public List<Condition> Conditions = new();
            long ID = 0;

            public void Read(BINAReader reader)
            {
                StateName = reader.ReadStringTableEntry();
                unk0 = reader.Read<long>();
                Conditions = reader.ReadBINAArrayStruct64<Condition>();
            }

            public void Write(BINAWriter writer)
            {
                ID = Random.Shared.NextInt64();
                writer.WriteStringTableEntry(StateName);
                writer.Write(unk0);
                writer.AddOffset("conditions" + ID);
                writer.Write((long)Conditions.Count);
            }

            public void FinishWrite(BINAWriter writer)
            {
                writer.SetOffset("conditions" + ID);
                foreach(var i in Conditions)
                    i.Write(writer);
                foreach (var i in Conditions)
                    i.FinishWrite(writer);
            }


            public class Condition : IBINASerializable
            {
                public ConditionData Data = new();
                public long unk0 = 6;
                public long unk1 = 1024;
                long ID = 0;

                public void Read(BINAReader reader)
                {
                    Data.Read(reader);
                    unk0 = reader.Read<long>();
                    reader.Skip(8); // this points to the ConditionData's third value
                    unk1 = reader.Read<long>();
                }

                public void Write(BINAWriter writer)
                {
                    ID = Random.Shared.NextInt64();
                    Data.Write(writer);
                    writer.Write(unk0);
                    writer.AddOffset("condition" + ID);
                    writer.Write(unk1);
                }

                public void FinishWrite(BINAWriter writer)
                {
                    writer.Skip(8);
                    writer.SetOffset("condition" + ID);
                    writer.Skip(-8);
                    Data.FinishWrite(writer);
                }


                public class ConditionData : IBINASerializable
                {
                    public string ConditionName = "";
                    public string unk0 = "";
                    public long Data = 0;
                    long ID = 0;

                    public void Read(BINAReader reader)
                    {
                        long offset = reader.Read<long>();
                        reader.ReadAtOffset(offset + 64, () =>
                        {
                            ConditionName = reader.ReadStringTableEntry();
                            unk0 = reader.ReadStringTableEntry();
                            Data = reader.Read<long>();
                        });
                    }

                    public void Write(BINAWriter writer)
                    {
                        ID = Random.Shared.NextInt64();
                        writer.AddOffset("conditionData" + ID);
                    }

                    public void FinishWrite(BINAWriter writer)
                    {
                        writer.SetOffset("conditionData" + ID);
                        writer.WriteStringTableEntry(ConditionName);
                        writer.WriteStringTableEntry(unk0);
                        writer.Write(Data);
                    }
                }
            }
        }

        public class UnkStateNames : IBINASerializable
        {
            public string Unk0 = "";
            public string Unk1 = "";

            public void Read(BINAReader reader)
            {
                Unk0 = reader.ReadStringTableEntry();
                Unk1 = reader.ReadStringTableEntry();
            }

            public void Write(BINAWriter writer)
            {
                writer.WriteStringTableEntry(Unk0);
                writer.WriteStringTableEntry(Unk1);
            }

            public void FinishWrite(BINAWriter writer)
            {
                
            }
        }
    }
}