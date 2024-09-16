using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Reflection.PortableExecutable;

namespace AshDumpLib.HedgehogEngine.BINA;
public class ShaderList : IFile
{
    public const string FileExtension = ".shader-list";
    public const string BINASignature = "NDSL";

    public int Version = 2;

    public ShaderListInput MainInput = new();
    public List<ShaderListInput> Inputs = new();

    public ShaderList() { }

    public ShaderList(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.Read<int>();
        long tableOffset = reader.Read<long>();
        long tableLength = reader.Read<long>();
        MainInput.Read(reader);
        reader.ReadAtOffset(tableOffset + 64, () =>
        {
            for (int i = 0; i < tableLength; i++)
            {
                ShaderListInput input = new();
                input.Read(reader);
                Inputs.Add(input);
            }
        });
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Write(Version);
        writer.AddOffset("tableOffset");
        writer.Write((long)Inputs.Count);
        MainInput.Write(writer);
        writer.SetOffset("tableOffset");
        foreach (var i in Inputs)
            i.Write(writer);
        writer.FinishWrite();
        writer.Dispose();
    }

    public class ShaderListInput
    {
        public string Name = "";
        public int ID = 0;

        public void Read(BINAReader reader)
        {
            Name = reader.ReadStringTableEntry();
            ID = reader.ReadInt32();
            reader.Align(8);
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteStringTableEntry(Name);
            writer.Write(ID);
            writer.Align(8);
        }
    }
}