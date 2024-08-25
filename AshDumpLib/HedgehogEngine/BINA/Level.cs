using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine.BINA;

//This was made for a joke

public class Level : IFile
{
    public const string FileExtension = ".level";
    public const string BINASignature = "VLEH";

    public Level() { }

    public Level(string filename) => Open(filename);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() => Write(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        reader.Skip(12);
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.WriteNulls(12);
        writer.FinishWrite();
        writer.Dispose();
    }
}