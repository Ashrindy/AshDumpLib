using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using AshDumpLib.Helpers;

namespace AshDumpLib.HedgehogEngine.Needle;

public class NeedleArchive : Archive
{
    public const string FileExtension = ".model";

    public const string ArchiveSignature = "NEDARCV1";
    public const string ArchiveFileExtension = "arc";

    public const string LodInfoSignature = "NEDLDIV1";
    public const string LodInfoFileExtension = "lodinfo";

    public const string ModelSignature = "NEDMDLV5";
    public const string ModelFileExtension = "model";

    public NeedleArchive() { }

    public NeedleArchive(string filename) => Open(filename);
    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big));
    public override void WriteBuffer() { MemoryStream memStream = new MemoryStream(); Write(new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big)); Data = memStream.ToArray(); }

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.Signature = reader.ReadString(StringBinaryFormat.FixedLength, 8);
        if (reader.Signature != ArchiveSignature)
            throw new Exception("Wrong signature!");
        int filesize = reader.Read<int>();
        string archiveFileExtension = reader.ReadString(StringBinaryFormat.FixedLength, 4);
        while(reader.Position < filesize)
        {
            NedArcFile arcFile = new();
            arcFile.Signature = reader.ReadString(StringBinaryFormat.FixedLength, 8);
            arcFile.Extension = reader.ReadString(StringBinaryFormat.NullTerminated);
            reader.Align(4);
            int arcfilesize = reader.Read<int>();
            arcFile.Data = reader.ReadArray<byte>(arcfilesize);
            switch (arcFile.Signature)
            {
                case ModelSignature:
                    MemoryStream data = new(arcFile.Data);
                    ExtendedBinaryReader dReader = new(data, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big);
                    dReader.Skip(8);
                    int offsetsPtr = dReader.Read<int>();
                    int offsetsCount = dReader.Read<int>();
                    Dictionary<int, uint> offsets = new();
                    dReader.Seek(offsetsPtr, SeekOrigin.Begin);
                    for (int x = 0; x < offsetsCount; x++)
                    {
                        int offsetPtr = dReader.Read<int>();
                        dReader.Seek(offsetPtr + 0x10, SeekOrigin.Begin);
                        uint offset = dReader.Read<uint>();
                        offsets.Add(offsetPtr, MathA.SwapBytes(offset));
                        dReader.Seek(offsetsPtr + (x + 1) * 4, SeekOrigin.Begin);
                    }
                    dReader.Dispose();

                    ExtendedBinaryWriter writer = new(data, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big);
                    foreach (var x in offsets)
                    {
                        writer.Seek(x.Key + 0x10, SeekOrigin.Begin);
                        writer.Write((int)(x.Value + x.Key));
                    }
                    writer.Dispose();

                    arcFile.Data = data.ToArray();
                    data.Dispose();
                    data.Close();
                    break;
            }
            if(Files.Where(x => x.Extension.Contains(arcFile.Extension)).Count() > 0)
                arcFile.Extension = $"{arcFile.Extension}.{Files.Where(x => x.Extension.Contains(arcFile.Extension)).Count()}";
            AddFile(new(FilePath.Replace(Extension, $"{arcFile.Extension}")), arcFile.Data);
        }
        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteString(StringBinaryFormat.FixedLength, ArchiveSignature, 8);
        writer.WriteNulls(4);
        writer.WriteString(StringBinaryFormat.FixedLength, ArchiveFileExtension, 4);
        List<NedArcFile> nedArcFiles = new();
        foreach(var i in Files)
        {
            string ext = i.Extension;
            if (ext.Contains("."))
                ext = ext.Replace(ext.Substring(ext.IndexOf('.')), "");
            switch (ext)
            {
                case LodInfoFileExtension:
                    nedArcFiles.Add(new() { Signature = LodInfoSignature, Extension = LodInfoFileExtension, Data = i.Data });
                    break;

                case ModelFileExtension:
                    NedArcFile mdl = new() { Signature = ModelSignature, Extension = ModelFileExtension };
                    MemoryStream data = new(i.Data);
                    ExtendedBinaryReader reader = new(data, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big);
                    reader.Skip(8);
                    int offsetsPtr = reader.Read<int>();
                    int offsetsCount = reader.Read<int>();
                    Dictionary<int, uint> offsets = new();

                    reader.Seek(offsetsPtr, SeekOrigin.Begin);
                    for(int x = 0; x < offsetsCount; x++) 
                    {
                        int offsetPtr = reader.Read<int>();
                        reader.Seek(offsetPtr + 0x10, SeekOrigin.Begin);
                        uint offset = reader.Read<uint>();
                        offsets.Add(offsetPtr, offset);
                        reader.Seek(offsetsPtr + (x + 1) * 4, SeekOrigin.Begin);
                    }
                    reader.Dispose();

                    ExtendedBinaryWriter dWriter = new(data, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Little);
                    foreach(var x in offsets)
                    {
                        dWriter.Seek(x.Key + 0x10, SeekOrigin.Begin);
                        dWriter.Write((uint)(x.Value - x.Key));
                    }
                    dWriter.Dispose();

                    mdl.Data = data.ToArray();
                    nedArcFiles.Add(mdl);
                    data.Dispose();
                    data.Close();
                    break;
            }
        }
        foreach(var i in nedArcFiles)
        {
            writer.WriteString(StringBinaryFormat.FixedLength, i.Signature, 8);
            writer.WriteString(StringBinaryFormat.NullTerminated, i.Extension);
            writer.Align(4);
            writer.Write(i.Data.Length);
            writer.WriteArray(i.Data);
        }
        int filesize = (int)writer.Position - 8;
        writer.WriteAt(filesize, 0x08);
        writer.Dispose();
    }

    public struct NedArcFile
    {
        public string Signature;
        public string Extension;
        public byte[] Data;
    }
}
