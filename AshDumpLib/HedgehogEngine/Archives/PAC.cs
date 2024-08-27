using Amicitia.IO.Binary;
using AshDumpLib.HedgehogEngine.BINA;
using AshDumpLib.HedgehogEngine.Mirage.Anim;
using AshDumpLib.Helpers.Archives;
using K4os.Compression.LZ4;

namespace AshDumpLib.HedgehogEngine.Archives;

public class PAC : Archive
{
    public const string FileExtension = ".pac";
    public const string Signature = "PACx";

    public struct ResType
    {
        public enum ELocation
        {
            Root = 0,
            Split,
            V2Merged
        }

        public string Extension;
        public string Type;
        public ELocation Location;
    }

    // Taken from HedgeLib++
    public readonly List<ResType> ResTypesRangers = new()
    {
        new() { Extension = "mlevel",            Type = "ResMasterLevel",             Location = ResType.ELocation.Root},
        new() { Extension = "level",             Type = "ResLevel",                   Location = ResType.ELocation.Root},
        new() { Extension = "anm.pxd",           Type = "ResAnimationPxd",            Location = ResType.ELocation.Root},
        new() { Extension = "skl.pxd",           Type = "ResSkeletonPxd",             Location = ResType.ELocation.Root},
        new() { Extension = "dds",               Type = "ResTexture",                 Location = ResType.ELocation.Split},
        new() { Extension = "asm",               Type = "ResAnimator",                Location = ResType.ELocation.Root},
        new() { Extension = "mat-anim",          Type = "ResAnimMaterial",            Location = ResType.ELocation.Split},
        new() { Extension = "dvscene",           Type = "ResDvScene",                 Location = ResType.ELocation.Root},
        new() { Extension = "uv-anim",           Type = "ResAnimTexSrt",              Location = ResType.ELocation.Split},
        new() { Extension = "cemt",              Type = "ResCyanEffect",              Location = ResType.ELocation.Root},
        new() { Extension = "material",          Type = "ResMirageMaterial",          Location = ResType.ELocation.Split},
        new() { Extension = "model",             Type = "ResModel",                   Location = ResType.ELocation.Split},
        new() { Extension = "cam-anim",          Type = "ResAnimCameraContainer",     Location = ResType.ELocation.Split},
        new() { Extension = "vis-anim",          Type = "ResAnimVis",                 Location = ResType.ELocation.Split},
        new() { Extension = "rfl",               Type = "ResReflection",              Location = ResType.ELocation.Root},
        new() { Extension = "cnvrs-text",        Type = "ResText",                    Location = ResType.ELocation.Root},
        new() { Extension = "btmesh",            Type = "ResBulletMesh",              Location = ResType.ELocation.Root},
        new() { Extension = "effdb",             Type = "ResParticleLocation",        Location = ResType.ELocation.Root},
        new() { Extension = "gedit",             Type = "ResObjectWorld",             Location = ResType.ELocation.Root},
        new() { Extension = "pccol",             Type = "ResPointcloudCollision",     Location = ResType.ELocation.Root},
        new() { Extension = "path",              Type = "ResSplinePath",              Location = ResType.ELocation.Root},
        new() { Extension = "lf",                Type = "ResSHLightField",            Location = ResType.ELocation.Root},
        new() { Extension = "probe",             Type = "ResProbe",                   Location = ResType.ELocation.Root},
        new() { Extension = "occ",               Type = "ResOcclusionCapsule",        Location = ResType.ELocation.Root},
        new() { Extension = "swif",              Type = "ResSurfRideProject",         Location = ResType.ELocation.Root},
        new() { Extension = "densitysetting",    Type = "ResDensitySetting",          Location = ResType.ELocation.Root},
        new() { Extension = "densitypointcloud", Type = "ResDensityPointCloud",       Location = ResType.ELocation.Root},
        new() { Extension = "lua",               Type = "ResLuaData",                 Location = ResType.ELocation.Root},
        new() { Extension = "btsmc",             Type = "ResSkinnedMeshCollider",     Location = ResType.ELocation.Root},
        new() { Extension = "terrain-model",     Type = "ResMirageTerrainModel",      Location = ResType.ELocation.Split},
        new() { Extension = "gismop",            Type = "ResGismoConfigPlan",         Location = ResType.ELocation.Root},
        new() { Extension = "fxcol",             Type = "ResFxColFile",               Location = ResType.ELocation.Root},
        new() { Extension = "gismod",            Type = "ResGismoConfigDesign",       Location = ResType.ELocation.Root},
        new() { Extension = "nmt",               Type = "ResNavMeshTile",             Location = ResType.ELocation.Root},
        new() { Extension = "pcmodel",           Type = "ResPointcloudModel",         Location = ResType.ELocation.Root},
        new() { Extension = "nmc",               Type = "ResNavMeshConfig",           Location = ResType.ELocation.Root},
        new() { Extension = "vat",               Type = "ResVertexAnimationTexture",  Location = ResType.ELocation.Root},
        new() { Extension = "heightfield",       Type = "ResHeightField",             Location = ResType.ELocation.Root},
        new() { Extension = "light",             Type = "ResMirageLight",             Location = ResType.ELocation.Root},
        new() { Extension = "pba",               Type = "ResPhysicalSkeleton",        Location = ResType.ELocation.Root},
        new() { Extension = "pcrt",              Type = "ResPointcloudLight",         Location = ResType.ELocation.Root},
        new() { Extension = "aism",              Type = "ResAIStateMachine",          Location = ResType.ELocation.Root},
        new() { Extension = "cnvrs-proj",        Type = "ResTextProject",             Location = ResType.ELocation.Root},
        new() { Extension = "terrain-material",  Type = "ResTerrainMaterial",         Location = ResType.ELocation.Root},
        new() { Extension = "pt-anim",           Type = "ResAnimTexPat",              Location = ResType.ELocation.Split},
        new() { Extension = "okern",             Type = "ResOpticalKerning",          Location = ResType.ELocation.Root},
        new() { Extension = "scfnt",             Type = "ResScalableFontSet",         Location = ResType.ELocation.Root},
        new() { Extension = "cso",               Type = "ResMirageComputeShader",     Location = ResType.ELocation.Split},
        new() { Extension = "pso",               Type = "ResMiragePixelShader",       Location = ResType.ELocation.Split},
        new() { Extension = "vib",               Type = "ResVibration",               Location = ResType.ELocation.Root},
        new() { Extension = "vso",               Type = "ResMirageVertexShader",      Location = ResType.ELocation.Split},
        new() { Extension = "pointcloud",        Type = "ResPointcloud",              Location = ResType.ELocation.Root},
        new() { Extension = "shader-list",       Type = "ResShaderList",              Location = ResType.ELocation.Root},
        new() { Extension = "cnvrs-meta",        Type = "ResTextMeta",                Location = ResType.ELocation.Root},
    };

    ResType GetResTypeByExt(string extension) { return ResTypesRangers.Find(x => x.Extension == extension); }
    ResType GetResTypeByType(string type) { return ResTypesRangers.Find(x => x.Type == type); }

    struct SplitString
    {
        public string Value;
        public int ParentIndex;
        public int StartIndex;
    }

    static string CommonPrefix(string[] input)
    {
        if (input.Length == 0)
        {
            return "";
        }
        Array.Sort(input);
        string prefix = "";
        for (int i = 0; i < input[0].Length; i++)
        {
            char c = input[0][i];
            for (int j = 1; j < input.Length; j++)
            {
                if (i >= input[j].Length || input[j][i] != c)
                {
                    return prefix;
                }
            }
            prefix += c;
        }
        return prefix;
    }

    static List<Tuple<string, string>> FindStringsWithCommonSubstrings(string[] strings, int minLength)
    {
        List<Tuple<string, string>> commonPairs = new List<Tuple<string, string>>();

        for (int i = 0; i < strings.Length; i++)
        {
            for (int j = i + 1; j < strings.Length; j++)
            {
                if (HasCommonSubstring(strings[i], strings[j], minLength))
                {
                    commonPairs.Add(new Tuple<string, string>(strings[i], strings[j]));
                }
            }
        }

        return commonPairs;
    }

    static bool HasCommonSubstring(string str1, string str2, int minLength)
    {
        for (int length = minLength; length <= Math.Min(str1.Length, str2.Length); length++)
        {
            for (int i = 0; i <= str1.Length - length; i++)
            {
                string substring = str1.Substring(i, length);
                if (str2.Contains(substring))
                {
                    return true;
                }
            }
        }
        return false;
    }

    static void ConvertToSplitStrings(List<string> strings, int parentID, int startID, ref List<SplitString> sStr)
    {
        string prefix = CommonPrefix(strings.ToArray());
        sStr.Add(new() { Value = prefix, ParentIndex = parentID, StartIndex = startID });
        if (prefix != "")
        {
            List<string> strs = new();
            foreach(var i in strings)
                strs.Add(i.Substring(prefix.Length));
            ConvertToSplitStrings(strs, parentID++, startID + prefix.Length, ref sStr);
        }
    }

    static List<SplitString> SplitStringsIntoNodes(List<string> strings)
    {
        List<SplitString> sStr = new();
        List<string> sStrings = new(new HashSet<string>(strings));
        ConvertToSplitStrings(sStrings, -1, 0, ref sStr);
        return sStr;
    }


    public PACVersion Version;
    public uint ID = 0;
    public PacType Type;

    List<string> parentPaths = new();
    public List<Dependency> dependencies = new();

    public PAC() { }

    public PAC(string filepath) => Open(filepath);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.ReadSignature(Signature);
        Header header = reader.Read<Header>();
        ID = header.id;
        Version = header.version;
        if (header.version.MajorVersion == '4')
        {
            if (header.version.MinorVersion == '0')
            {
                if (header.version.RevisionVersion == '2')
                    ReadV2(reader);
                else if (header.version.RevisionVersion == '3')
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

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteSignature(Signature);
        writer.Write(Version);
        writer.Write((byte)'L');
        if (ID == 0)
        {
            Random rnd = new();
            byte[] ids = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                ids[i] = (byte)rnd.Next(0, 256);
                writer.Write(ids[i]);
            }
            ID = BitConverter.ToUInt32(ids, 0);
        }
        else
            writer.Write(ID);
        writer.AddOffset("fileSize", false);
        if (Version.MajorVersion == '4')
        {
            if (Version.MinorVersion == '0')
            {
                if (Version.RevisionVersion == '2')
                    WriteV2(writer);
                else if (Version.RevisionVersion == '3')
                    WriteV3(writer);
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
                                case "anm.pxd":
                                    AnimationPXD animationPXD = new();
                                    animationPXD.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(animationPXD);
                                    break;

                                case "cnvrs-text":
                                    Text text = new();
                                    text.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(text);
                                    break;

                                case "densitypointcloud":
                                    DensityPointCloud densityPointCloud = new();
                                    densityPointCloud.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(densityPointCloud);
                                    break;

                                case "densitysetting":
                                    DensitySetting densitySetting = new();
                                    densitySetting.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(densitySetting);
                                    break;

                                case "gedit":
                                    ObjectWorld objWld = new();
                                    objWld.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(objWld);
                                    break;

                                case "level":
                                    Level level = new();
                                    level.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(level);
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

                                case "heightfield":
                                    HeightField hgt = new();
                                    hgt.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(hgt);
                                    break;

                                case "pcmodel" or "pcrt" or "pccol":
                                    PointCloud pointcloud = new();
                                    pointcloud.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(pointcloud);
                                    break;

                                case "probe":
                                    Probe probe = new();
                                    probe.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(probe);
                                    break;

                                case "terrain-material":
                                    TerrainMaterial tMat = new();
                                    tMat.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(tMat);
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

    void WriteV2(ExtendedBinaryWriter writer) 
    {
        writer.AddOffset("treesSize", false);
        writer.AddOffset("depTableSize", false);
        writer.AddOffset("dataEntriesSize", false);
        writer.AddOffset("strTableSize", false);
        writer.AddOffset("fileDataSize", false);
        writer.AddOffset("offTableSize", false);
        writer.Write((short)Type);
        //Just a temporary thing for now
        writer.Write((short)264);
        writer.Write(dependencies.Count);
        Tree<Node<Tree<Node<FileNode>>>> tree = new();
        Random rnd = new();
        tree.ID = rnd.Next();
        Files.Sort((x, y) => string.Compare(x.Extension, y.Extension));
        List<string> resTypes = new();
        foreach(var i in Files)
            resTypes.Add(GetResTypeByExt(i.Extension).Type);
        Node<Tree<Node<FileNode>>> mainNode = new();
        mainNode.name = "";
        mainNode.parentIndex = -1;
        mainNode.globalIndex = 0;
        mainNode.dataIndex = -1;
        mainNode.childIndices.Add(1);
        mainNode.bufferStartIndex = 0;
        tree.nodes.Add(mainNode);
        var f = SplitStringsIntoNodes(resTypes);
        //tree.nodes.AddRange();
        tree.Write(writer);
        tree.FinishWrite(writer);

        int stringTableOffset = (int)writer.Position;

        foreach (var i in writer.StringTableOffsets)
        {
            writer.Seek(i.Key, SeekOrigin.Begin);
            writer.Write(i.Value + stringTableOffset);
        }
        writer.Seek(stringTableOffset, SeekOrigin.Begin);
        foreach (var i in writer.StringTable)
            writer.WriteChar(i);

        writer.FixPadding(4);

        int stringSize = (int)writer.Position - (int)writer.GetOffsetValue("strTableSize");

        writer.WriteAt(stringSize, writer.GetOffset("strTableSize"));
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
            if (rootPac.dependencies[i].main.uncompressedSize == rootPac.dependencies[i].main.compressedSize)
            {
                rootPac.dependencies[i].main.uncompressedData = reader.ReadArray<byte>(rootPac.dependencies[i].main.uncompressedSize);
            }
            else
            {
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
            PAC tempPac = new();
            tempPac.Open(rootPac.dependencies[i].name, rootPac.dependencies[i].main.uncompressedData, parseFiles);
            foreach (var x in tempPac.Files)
                AddFile(x);
        }
    }

    void WriteV3(ExtendedBinaryWriter writer)
    {
        writer.AddOffset("root");
        writer.AddOffset("rootCSize", false);
        writer.AddOffset("rootUCSize", false);
        FlagsV4 flags = FlagsV4.unk | FlagsV4.hasMetadata;
        if(parentPaths.Count > 0)
            flags |= FlagsV4.hasParents;
        writer.Write((short)flags);
        writer.Write((short)(FlagsV3.unk | FlagsV3.lz4));

        writer.AddOffset("parentsSize", false);
        writer.AddOffset("chunkTableSize", false);
        writer.AddOffset("strTableSize", false);
        writer.AddOffset("offTableSize", false);
        writer.Write((long)parentPaths.Count);
        writer.AddOffset("parentsTable", true);
        writer.WriteNulls(4);
        writer.SetOffset("parentsTable");
        foreach(var path in parentPaths)
        {
            writer.AddOffset(path, true);
            writer.WriteNulls(4);
        }
        PAC rootPac = new();
        rootPac.ID = ID;
        rootPac.Files = Files;
        rootPac.Version = new() { MajorVersion = Version.MajorVersion, MinorVersion = (byte)'0', RevisionVersion = (byte)'2' };
        rootPac.Type = PacType.Root;
        Chunk rootChunk = new();
        int size = 0;
        foreach (var i in Files)
            size += i.Data.Length;
        rootChunk.uncompressedData = new byte[1024 + Files.Count * 32 + size];
        rootPac.Write(new(new MemoryStream(rootChunk.uncompressedData), Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Little));
        File.WriteAllBytes(FilePath + ".root", rootChunk.uncompressedData);
    }

    public struct PACVersion
    {
        public byte MajorVersion;
        public byte MinorVersion;
        public byte RevisionVersion;
    }

    struct Header
    {
        public PACVersion version;
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
            if (chunks[0].uncompressedSize == 0)
            {
                chunks.Clear();
                chunks.Add(main);
            }
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            writer.AddOffset(name);
            writer.WriteNulls(4);
            writer.Write(main.compressedSize);
            writer.Write(main.uncompressedSize);
            writer.Write(dataPos);
            writer.Write(chunks.Count);
            writer.AddOffset(name + "chunkOffsets");
            writer.WriteNulls(4);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            writer.SetOffset(name + "chunkOffsets");
            foreach(var i in chunks)
            {
                writer.Write(i.compressedSize);
                writer.Write(i.uncompressedSize);
            }
        }
    }

    class Tree<T> : IExtendedBinarySerializable where T : IExtendedBinarySerializable, new()
    {
        public List<T> nodes = new();
        public List<int> indices = new();
        public int ID = 0;

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
            writer.Write(nodes.Count);
            writer.Write(indices.Count);
            writer.AddOffset(nodes.Count.ToString() + "nodes" + typeof(T).Name + ID);
            writer.WriteNulls(4);
            writer.AddOffset(indices.Count.ToString() + "indices" + typeof(T).Name + ID);
            writer.WriteNulls(4);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            writer.SetOffset(nodes.Count.ToString() + "nodes" + typeof(T).Name + ID);
            foreach (var i in nodes)
                i.Write(writer);
            foreach (var i in nodes)
                i.FinishWrite(writer);
            writer.SetOffset(indices.Count.ToString() + "indices" + typeof(T).Name + ID);
            writer.WriteArray(indices.ToArray());
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
        public T data;
        public int ID = 0;

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
            {
                data = new();
                reader.ReadAtOffset(dataPtr + reader.genericOffset, () => data.Read(reader));
            }
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            writer.WriteStringTableEntry(name);
            writer.AddOffset(globalIndex + "data" + ID);
            writer.Skip(4);
            writer.AddOffset(globalIndex + "indices" + ID);
            writer.Skip(4);
            writer.Write(parentIndex);
            writer.Write(globalIndex);
            writer.Write(dataIndex);
            writer.Write((short)childIndices.Count);
            if (data == null)
                writer.Write((byte)0);
            else
                writer.Write((byte)1);
            writer.Write((byte)bufferStartIndex);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            if(data != null)
            {
                writer.SetOffset(globalIndex + "data" + ID);
                data.Write(writer);
            }
            writer.SetOffset(globalIndex + "indices" + ID);
            writer.WriteArray(childIndices.ToArray());
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
            writer.Write(uid);
            writer.Write(dataSize);
            writer.WriteNulls(8);
            writer.Write(dataPtr);
            writer.WriteNulls(8);
            writer.WriteStringTableEntry(extension);
            writer.WriteNulls(4);
            writer.Write(flags);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
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

    public enum PacType
    {
        Root = 1,
        Split,
        HasSplits = 4,
        unk = 8
    }
}
