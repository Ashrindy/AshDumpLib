using Amicitia.IO.Binary;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace AshDumpLib.CastleSiege;

public class Archive : AshDumpLib.Helpers.Archives.Archive
{
    public const string FileExtension = ".rda";
    public const string Signature = "Resource File V1.1";

    public Archive() { }
    public Archive(string filename) => Open(filename);
    public Archive(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        string signature = Helpers.CastleStrikeString(reader);
        //if (signature != Signature)
        //    throw new Exception("Incorrect Signature!");
        int count = reader.Read<int>();
        for (int i = 0; i < count; i++)
        {
            File tempFile = new();
            tempFile.Read(reader);
            if (parseFiles)
            {
                switch (tempFile.FilePath.Substring(tempFile.FilePath.IndexOf('.') + 1))
                {
                    case "rdo":
                        Model model = new();
                        model.Open(tempFile.FilePath, tempFile.Data.ToArray());
                        AddFile(model);
                        break;

                    case "rdm":
                        Animation anim = new();
                        anim.Open(tempFile.FilePath, tempFile.Data.ToArray());
                        AddFile(anim);
                        break;

                    case "hlm":
                        HeightLevelMap hlm = new();
                        hlm.Open(tempFile.FilePath, tempFile.Data.ToArray());
                        AddFile(hlm);
                        break;

                    case "tlm":
                        TerrainLevelMap tlm = new();
                        tlm.Open(tempFile.FilePath, tempFile.Data.ToArray());
                        AddFile(tlm);
                        break;

                    default:
                        AddFile(tempFile.FilePath, tempFile.Data.ToArray());
                        break;
                }
            }
            else
                AddFile(tempFile.FilePath, tempFile.Data.ToArray());
        }

        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, Signature, 256);
        writer.Write(Files.Count);
        List<File> files = new();
        foreach(var i in Files)
        {
            File file = new();
            file.FilePath = i.FilePath;
            file.Data = i.Data.ToList();
            file.Write(writer);
            files.Add(file);
        }
        foreach (var i in files)
            i.FinishWrite(writer);

        writer.Dispose();
    }

    public class File : IExtendedBinarySerializable
    {
        public List<byte> Data = new();
        public string FilePath = "";

        long pointerPos;
        byte[] compressedData;

        public void Read(ExtendedBinaryReader reader)
        {
            FilePath = Helpers.CastleStrikeString(reader);
            int pointer = reader.Read<int>();
            int compressedSize = reader.Read<int>();
            int size = reader.Read<int>();
            int unk3 = reader.Read<int>();
            float unk4 = reader.Read<float>();

            long prePos = reader.Position;
            reader.Seek(pointer, SeekOrigin.Begin);
            byte[] compressedData = new byte[compressedSize];
            compressedData = reader.ReadArray<byte>(compressedSize);
            if (compressedSize != size)
            {
                byte[] uncompressedData = new byte[size];
                using (var inputStream = new MemoryStream(compressedData))
                {
                    using (var decompressionStream = new InflaterInputStream(inputStream))
                    {
                        using (var outputStream = new MemoryStream())
                        {
                            decompressionStream.CopyTo(outputStream);
                            uncompressedData = outputStream.ToArray();
                        }
                    }
                }
                Data = uncompressedData.ToList();
            }
            else
                Data = compressedData.ToList();
            reader.Seek(prePos, SeekOrigin.Begin);
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            writer.WriteString(System.Text.Encoding.Latin1, StringBinaryFormat.FixedLength, FilePath, 256);
            pointerPos = writer.Position;
            writer.Skip(4);

            using (var outputStream = new MemoryStream())
            {
                var zlibStream = new DeflaterOutputStream(outputStream, new Deflater(Deflater.BEST_COMPRESSION));
                zlibStream.Write(Data.ToArray(), 0, Data.Count);
                zlibStream.Close();
                compressedData = outputStream.ToArray();
            }

            writer.Write(compressedData.Length);
            writer.Write(Data.Count);
            writer.Write(5);
            writer.Write<short>(15047);
            writer.Write<short>(16334);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            long pointer = writer.Position;
            writer.Seek(pointerPos, SeekOrigin.Begin);
            writer.Write((int)pointer);
            writer.Seek(pointer, SeekOrigin.Begin);
            writer.WriteArray(compressedData);
        }
    }
}