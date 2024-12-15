using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using System.Text;

namespace AshDumpLib.HedgehogEngine.BINA.ScalableFont;

public class OpticalKerning : IFile
{
    public const string FileExtension = ".okern";

    public int Version = 1;
    public string FontName = "";
    public List<Letter> Letters = new();

    public OpticalKerning() { }

    public OpticalKerning(string filename) => Open(filename);
    public OpticalKerning(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        Version = reader.Read<int>();
        int letterCount = reader.Read<int>();
        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
        {
            for(int i = 0; i < letterCount; i++)
            {
                Letter ltr = new();
                ltr.Read(reader);
                Letters.Add(ltr);
            }
        });
        FontName = reader.ReadStringTableEntry();
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.Write(Version);
        writer.Write(Letters.Count);
        writer.AddOffset("Letters");
        writer.WriteStringTableEntry(FontName);
        writer.Align(16);
        writer.SetOffset("Letters");
        foreach (var i in Letters)
            i.Write(writer);
        foreach (var i in Letters)
            i.FinishWrite(writer);
        writer.FinishWrite();
        writer.Dispose();
    }

    public class Letter : IBINASerializable
    {
        public struct SubLetter
        {
            public char Value;
            public ushort Spacing;
        }

        public char Value = '\0';
        public List<SubLetter> SubLetters = new();

        public void Read(BINAReader reader)
        {
            Value = Encoding.Unicode.GetString(reader.ReadArray<byte>(2))[0];
            short subletterCount = reader.Read<short>();
            reader.Align(8);
            reader.ReadAtOffset(reader.Read<long>() + 64, () =>
            {
                for(int i = 0; i < subletterCount; i++)
                    SubLetters.Add(reader.Read<SubLetter>());
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.WriteArray(Encoding.Unicode.GetBytes(Value.ToString()));
            writer.Write((short)SubLetters.Count);
            writer.Align(8);
            writer.AddOffset($"{Value} {SubLetters.Count} {SubLetters[0].Spacing}");
        }

        public void FinishWrite(BINAWriter writer)
        {
            writer.SetOffset($"{Value} {SubLetters.Count} {SubLetters[0].Spacing}");
            foreach(var i in SubLetters)
                writer.Write(i);
            writer.Align(8);
        }
    }
}
