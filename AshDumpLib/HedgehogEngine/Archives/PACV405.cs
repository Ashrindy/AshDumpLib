using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using K4os.Compression.LZ4;
using static AshDumpLib.HedgehogEngine.Archives.PAC;

namespace AshDumpLib.HedgehogEngine.Archives;

public class PACV405
{
    public List<IFile> files = new();
    public List<string> parents = new();

    public static readonly PACVersion Version = new() { MajorVersion = (byte)'4', MinorVersion = (byte)'0', RevisionVersion = (byte)'5' };

    struct Chunk
    {
        public int offset;
        public int compressedSize;
        public int uncompressedSize;
    }

    struct Header
    {
        public Chunk rootChunk;
        public short flags0;
        public short flags1;
        public int parentsSize;
        public int chunkTableSize;
        public int strTableSize;
        public int offTableSize;
    }

    public void Read(ExtendedBinaryReader reader)
    {
        var header = reader.Read<Header>();
        if ((header.flags0 & 0x02) != 0)
        {
            int parentCount = reader.Read<int>();
            reader.Align(8);
            reader.Seek(reader.Read<long>(), SeekOrigin.Begin);
            for (int i = 0; i < parentCount; i++)
                parents.Add(reader.ReadStringTableEntry64());
        }
        int chunkCount = reader.Read<int>();
        List<Chunk> chunkInfos = new List<Chunk>();
        for (int i = 0; i < chunkCount; i++)
        {
            Chunk tempChunk = new();
            tempChunk.compressedSize = reader.Read<int>();
            tempChunk.uncompressedSize = reader.Read<int>();
            chunkInfos.Add(tempChunk);
        }
        reader.Align(16);
        PACV402 rootPac = new();
        reader.ReadAtOffset(header.rootChunk.offset, () =>
        {
            List<byte> uncompressedData = new();
            foreach(var i in chunkInfos)
            {
                byte[] uncompressed = new byte[i.uncompressedSize];
                LZ4Codec.Decode(reader.ReadArray<byte>(i.compressedSize), 0, i.compressedSize, uncompressed, 0, i.uncompressedSize);
                uncompressedData.AddRange(uncompressed);
            }
            //File.WriteAllBytes($"{reader.CurFilePath}.root", uncompressedData.ToArray());
            MemoryStream stream = new(uncompressedData.ToArray());
            ExtendedBinaryReader rootReader = new(stream, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Little);
            rootReader.Skip(16);
            rootPac.Read(rootReader);
            rootReader.Dispose();
            stream.Dispose();
            stream.Close();
        });
        files.AddRange(rootPac.files);
        List<PACV402> splits = new();
        foreach(var i in rootPac.Dependencies)
        {
            PACV402 split = new();
            List<byte> uncompressedData = new();
            reader.Seek(i.dataPos, SeekOrigin.Begin);
            if (i.chunks.Count() > 0 && i.chunks[0].uncompressedSize == 0)
            {
                uncompressedData.AddRange(reader.ReadArray<byte>(i.compressedSize));
            }
            else
            {
                foreach (var x in i.chunks)
                {
                    byte[] uncompressed = new byte[x.uncompressedSize];
                    LZ4Codec.Decode(reader.ReadArray<byte>(x.compressedSize), 0, x.compressedSize, uncompressed, 0, x.uncompressedSize);
                    uncompressedData.AddRange(uncompressed);
                }
            }
            //File.WriteAllBytes($"{reader.CurFilePath}.{rootPac.Dependencies.IndexOf(i)}", uncompressedData.ToArray());
            MemoryStream stream = new(uncompressedData.ToArray());
            ExtendedBinaryReader splitReader = new(stream, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Little);
            splitReader.Skip(16);
            split.Read(splitReader);
            splitReader.Dispose();
            stream.Dispose();
            stream.Close();
            splits.Add(split);
            files.AddRange(split.files);
        }
    }

    public void Write(ExtendedBinaryWriter writer) {

    }
}
