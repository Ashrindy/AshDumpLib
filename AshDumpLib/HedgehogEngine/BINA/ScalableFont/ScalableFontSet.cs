using AshDumpLib.Helpers.Archives;
using System.Text;
using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine.BINA.ScalableFont;

public class ScalableFontSet : IFile
{
    public const string FileExtension = ".scfnt";
    public const string BINASignature = "KFCS";

    public string Version = "1000";
    public string FontName = "";
    public byte[] TTFData = new byte[0];
    public List<Letter> Letters = new();

    public ScalableFontSet() { }

    public ScalableFontSet(string filename) => Open(filename);
    public ScalableFontSet(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        Version = reader.ReadString(StringBinaryFormat.FixedLength, 4);
        FontName = reader.ReadStringTableEntry();
        long letterPtr = reader.Read<long>();
        long letterCount = reader.Read<int>();
        reader.Align(8);
        long ttfDataSize = reader.Read<long>();
        TTFData = reader.ReadArray<byte>((int)ttfDataSize);
        reader.ReadAtOffset(letterPtr + 64, () =>
        {
            for(int i = 0; i < letterCount; i++)
            {
                Letter ltr = new();
                ltr.Read(reader);
                Letters.Add(ltr);
            }
        });
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.WriteString(StringBinaryFormat.FixedLength, Version, 4);
        writer.WriteStringTableEntry(FontName);
        writer.AddOffset("letters");
        writer.Write(Letters.Count);
        writer.Align(8);
        writer.Write((long)TTFData.Length);
        writer.WriteArray(TTFData);
        writer.Align(8);
        writer.SetOffset("letters");
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
            private int padding;
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
                for (int i = 0; i < subletterCount; i++)
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
            foreach (var i in SubLetters)
                writer.Write(i);
            writer.Align(8);
        }
    }
}
