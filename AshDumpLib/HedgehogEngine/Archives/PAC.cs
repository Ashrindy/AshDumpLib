using Amicitia.IO.Binary;
using AshDumpLib.HedgehogEngine.Anim;
using AshDumpLib.HedgehogEngine.BINA;
using AshDumpLib.Helpers.Archives;
using K4os.Compression.LZ4;
using System.Text;

namespace AshDumpLib.HedgehogEngine.Archives;

public class PAC : Archive
{
    public const string FileExtension = ".pac";
    public const string Signature = "PACx";

    List<string> parentPaths = new();
    public List<Dependency> dependencies = new();

    public PAC() { }

    public PAC(string filepath) => Open(filepath);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.ReadSignature(Signature);
        Header header = reader.Read<Header>();
        if (header.majorVer == '4')
        {
            if (header.minorVer == '0')
            {
                if (header.revVer == '2')
                    ReadV2(reader);
                else if (header.revVer == '3')
                    ReadV3(reader);
                else
                    throw new Exception("Unimplemented Version!");
            }
            else
                throw new Exception("Unimplemented Version!");
        }
        else
            throw new Exception("Unimplemented Version!");
    }

    static void LoopThroughNodesForName(Tree<Node<FileNode>> tree, Node<FileNode> curNode, ref string name)
    {
        if (curNode.bufferStartIndex != 0)
        {
            name = tree.nodes[curNode.parentIndex].name + name;
            LoopThroughNodesForName(tree, tree.nodes[curNode.parentIndex], ref name);
        }
    }

    static void LoopThroughMainNodesForName(Tree<Node<Tree<Node<FileNode>>>> tree, Node<Tree<Node<FileNode>>> curNode, ref string name)
    {
        if (curNode.bufferStartIndex != 0)
        {
            name = tree.nodes[curNode.parentIndex].name + name;
            LoopThroughMainNodesForName(tree, tree.nodes[curNode.parentIndex], ref name);
        }
    }

    void ReadV2(ExtendedBinaryReader reader)
    {
        MetadataV3 dMetadata = reader.Read<MetadataV3>();
        Tree<Node<Tree<Node<FileNode>>>> tree = new();
        tree.Read(reader);
        foreach (var i in tree.indices)
        {
            if (tree.nodes[i].data != null)
            {
                string type = tree.nodes[tree.nodes[i].parentIndex].name;
                if (tree.nodes[i].bufferStartIndex != 0)
                    LoopThroughMainNodesForName(tree, tree.nodes[tree.nodes[i].parentIndex], ref type);
                foreach (var x in tree.nodes[i].data.indices)
                {
                    if (tree.nodes[i].data.nodes[x].data.dataPtr != 0)
                    {
                        byte[] data = new byte[tree.nodes[i].data.nodes[x].data.dataSize];
                        reader.ReadAtOffset(tree.nodes[i].data.nodes[x].data.dataPtr, () => data = reader.ReadArray<byte>(tree.nodes[i].data.nodes[x].data.dataSize));
                        string name = tree.nodes[i].data.nodes[tree.nodes[i].data.nodes[x].parentIndex].name;
                        if (tree.nodes[i].data.nodes[tree.nodes[i].data.nodes[x].parentIndex].bufferStartIndex != 0)
                            LoopThroughNodesForName(tree.nodes[i].data, tree.nodes[i].data.nodes[tree.nodes[i].data.nodes[x].parentIndex], ref name);
                        if (parseFiles)
                        {
                            switch (tree.nodes[i].data.nodes[x].data.extension)
                            {
                                case "densitysetting":
                                    DensitySetting densitySetting = new();
                                    densitySetting.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(densitySetting);
                                    break;

                                case "nmc":
                                    NavMeshConfig nmc = new();
                                    nmc.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(nmc);
                                    break;

                                case "nmt":
                                    NavMeshTile nmt = new();
                                    nmt.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(nmt);
                                    break;

                                case "probe":
                                    Probe probe = new();
                                    probe.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(probe);
                                    break;

                                case "skl.pxd":
                                    SkeletonPXD skeletonPXD = new();
                                    skeletonPXD.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(skeletonPXD);
                                    break;

                                case "cam-anim":
                                    CameraAnimation camAnim = new();
                                    camAnim.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(camAnim);
                                    break;

                                case "mat-anim":
                                    MaterialAnimation matAnim = new();
                                    matAnim.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(matAnim);
                                    break;

                                case "uv-anim":
                                    UVAnimation uvAnim = new();
                                    uvAnim.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(uvAnim);
                                    break;

                                case "vis-anim":
                                    VisibilityAnimation visAnim = new();
                                    visAnim.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(visAnim);
                                    break;

                                default:
                                    AddFile($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    break;
                            }
                        }
                        else
                            AddFile($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                    }
                }
            }
        }
        if (dMetadata.dependencyCount > 0)
        {
            reader.Seek(dMetadata.treesSize + 0x30, SeekOrigin.Begin);
            long depCount = reader.Read<long>();
            long ptr = reader.Read<long>();
            reader.Seek(ptr, SeekOrigin.Begin);
            for (int i = 0; i < depCount; i++)
            {
                Dependency depen = new();
                depen.Read(reader);
                dependencies.Add(depen);
            }
        }
    }

    bool HasMetadata(HeaderV4 m)
    {
        return (m.flagV4 & (ushort)FlagsV4.hasMetadata) != 0;
    }

    bool HasParents(HeaderV4 m)
    {
        return (m.flagV4 & (ushort)FlagsV4.hasParents) != 0;
    }

    void ReadV3(ExtendedBinaryReader reader)
    {
        HeaderV4 header2 = reader.Read<HeaderV4>();
        MetadataV4 metadata = new();
        if (HasMetadata(header2))
        {
            metadata = reader.Read<MetadataV4>();
            if (HasParents(header2))
            {
                long parentCount = reader.Read<long>();
                long parentPtr = reader.Read<long>();
                reader.Seek(parentPtr, SeekOrigin.Begin);
                for (int i = 0; i < parentCount; i++)
                    parentPaths.Add(reader.ReadStringTableEntry64());
            }
        }

        int chunkCount = reader.Read<int>();
        List<Chunk> chunkInfos = new();
        for (int i = 0; i < chunkCount; i++)
        {
            Chunk tempChunk = new();
            tempChunk.compressedSize = reader.Read<int>();
            tempChunk.uncompressedSize = reader.Read<int>();
            chunkInfos.Add(tempChunk);
        }

        Chunk rootChunk = new();
        rootChunk.compressedSize = (int)header2.rootCompressedSize;
        rootChunk.uncompressedSize = (int)header2.rootUncompressedSize;
        long prePos1 = reader.Position;
        reader.Seek(header2.rootOffset, SeekOrigin.Begin);
        rootChunk.uncompressedData = new byte[rootChunk.uncompressedSize];
        List<byte> uncompressedRootData = new();
        for (int i = 0; i < chunkCount; i++)
        {
            chunkInfos[i].compressedData = reader.ReadArray<byte>(chunkInfos[i].compressedSize);
            chunkInfos[i].uncompressedData = new byte[chunkInfos[i].uncompressedSize];
            _ = LZ4Codec.Decode(chunkInfos[i].compressedData, 0, chunkInfos[i].compressedSize, chunkInfos[i].uncompressedData, 0, chunkInfos[i].uncompressedSize);
            uncompressedRootData.AddRange(chunkInfos[i].uncompressedData);
        }
        rootChunk.uncompressedData = uncompressedRootData.ToArray();
        reader.Seek(prePos1, SeekOrigin.Begin);
        PAC rootPac = new();
        rootPac.Open(FileName + ".root", rootChunk.uncompressedData, parseFiles);
        foreach (var x in rootPac.Files)
            AddFile(x);

        for (int i = 0; i < rootPac.dependencies.Count; i++)
        {
            reader.Seek(rootPac.dependencies[i].dataPos, SeekOrigin.Begin);
            rootPac.dependencies[i].main.uncompressedData = new byte[rootPac.dependencies[i].main.uncompressedSize];
            List<byte> uncompressedData = new();
            for (int x = 0; x < rootPac.dependencies[i].chunks.Count; x++)
            {
                rootPac.dependencies[i].chunks[x].compressedData = reader.ReadArray<byte>(rootPac.dependencies[i].chunks[x].compressedSize);
                rootPac.dependencies[i].chunks[x].uncompressedData = new byte[rootPac.dependencies[i].chunks[x].uncompressedSize];
                _ = LZ4Codec.Decode(rootPac.dependencies[i].chunks[x].compressedData, 0, rootPac.dependencies[i].chunks[x].compressedSize, rootPac.dependencies[i].chunks[x].uncompressedData, 0, rootPac.dependencies[i].chunks[x].uncompressedSize);
                uncompressedData.AddRange(rootPac.dependencies[i].chunks[x].uncompressedData);
            }
            rootPac.dependencies[i].main.uncompressedData = uncompressedData.ToArray();
        }
        foreach (var i in rootPac.dependencies)
        {
            PAC tempPac = new();
            tempPac.Open(i.name, i.main.uncompressedData, parseFiles);
            foreach (var x in tempPac.Files)
                AddFile(x);
        }
    }

    struct Header
    {
        public byte majorVer;
        public byte minorVer;
        public byte revVer;
        public byte endianess;
        public uint id;
        public uint fileSize;
    }

    struct HeaderV4
    {
        public int rootOffset;
        public uint rootCompressedSize;
        public uint rootUncompressedSize;
        public ushort flagV4;
        public ushort flagV3;
    }

    struct MetadataV4
    {
        public int parentsSize;
        public int chunkTableSize;
        public int strTableSize;
        public int offTableSize;
    }

    struct MetadataV3
    {
        public int treesSize;
        public int dependencyTableSize;
        public int dataEntriesSize;
        public int strTableSize;
        public int fileDataSize;
        public int offTableSize;
        public short type;
        public short flags;
        public int dependencyCount;
    }

    public class Dependency : IExtendedBinarySerializable
    {
        public string name = "";
        public Chunk main = new();
        public int dataPos;
        public List<Chunk> chunks = new();

        public void Read(ExtendedBinaryReader reader)
        {
            name = reader.ReadStringTableEntry64();
            main.compressedSize = reader.Read<int>();
            main.uncompressedSize = reader.Read<int>();
            dataPos = reader.Read<int>();
            var chunkCount = reader.Read<int>();
            var offset = reader.Read<long>();
            reader.ReadAtOffset(offset, () =>
            {
                for (int i = 0; i < chunkCount; i++)
                {
                    Chunk chunk = new Chunk();
                    chunk.compressedSize = reader.Read<int>();
                    chunk.uncompressedSize = reader.Read<int>();
                    chunks.Add(chunk);
                }
            });
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    class Tree<T> : IExtendedBinarySerializable where T : IExtendedBinarySerializable, new()
    {
        public List<T> nodes = new();
        public List<int> indices = new();

        public void Read(ExtendedBinaryReader reader)
        {
            int nodeCount = reader.Read<int>();
            int dataNodeCount = reader.Read<int>();
            long nodesPtr = reader.Read<long>();
            long indicesPtr = reader.Read<long>();
            reader.Jump(indicesPtr, SeekOrigin.Begin);
            indices.AddRange(reader.ReadArray<int>(dataNodeCount));
            reader.Seek(nodesPtr, SeekOrigin.Begin);
            for (int i = 0; i < nodeCount; i++)
            {
                T x = new T();
                x.Read(reader);
                nodes.Add(x);
            }
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    class Node<T> : IExtendedBinarySerializable where T : IExtendedBinarySerializable, new()
    {
        public string name = "";
        public int parentIndex;
        public int globalIndex;
        public int dataIndex;
        public List<int> childIndices = new();
        public int bufferStartIndex;
        public T data = new();

        public void Read(ExtendedBinaryReader reader)
        {
            name = reader.ReadStringTableEntry64();
            long dataPtr = reader.Read<long>();
            long childIndicesPtr = reader.Read<long>();
            parentIndex = reader.Read<int>();
            globalIndex = reader.Read<int>();
            dataIndex = reader.Read<int>();
            short childCount = reader.Read<short>();
            byte hasData = reader.Read<byte>();
            bufferStartIndex = reader.Read<byte>();
            if (childIndicesPtr != 0)
                childIndices.AddRange(reader.ReadArrayAtOffset<int>(childIndicesPtr, childCount));
            if (hasData == 1)
                reader.ReadAtOffset(dataPtr + reader.genericOffset, () => data.Read(reader));
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    class FileNode : IExtendedBinarySerializable
    {
        public int uid;
        public int dataSize;
        public long dataPtr;
        public long flags;
        public string extension = "";

        public FileNode() { }

        public void Read(ExtendedBinaryReader reader)
        {
            uid = reader.Read<int>();
            dataSize = reader.Read<int>();
            long unk0 = reader.Read<long>();
            dataPtr = reader.Read<long>();
            long unk1 = reader.Read<long>();
            extension = reader.ReadStringTableEntry64();
            flags = reader.Read<long>();
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            throw new NotImplementedException();
        }
    }

    public class Chunk
    {
        public int compressedSize;
        public int uncompressedSize;
        public byte[] compressedData;
        public byte[] uncompressedData;
    }

    enum FlagsV4
    {
        none = 0,
        unk = 1,
        hasParents = 2,
        hasMetadata = 0x80
    }

    enum FlagsV3
    {
        unk = 8,
        deflate = 0x100,
        lz4 = 0x200
    }
}
