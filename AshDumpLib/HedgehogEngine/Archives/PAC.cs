using Amicitia.IO.Binary;
using AshDumpLib.HedgehogEngine.BINA.Animation;
using AshDumpLib.HedgehogEngine.BINA.Density;
using AshDumpLib.HedgehogEngine.BINA.RFL;
using AshDumpLib.HedgehogEngine.BINA.Misc;
using AshDumpLib.HedgehogEngine.BINA.Terrain;
using AshDumpLib.HedgehogEngine.Mirage.Anim;
using AshDumpLib.Helpers.Archives;
using K4os.Compression.LZ4;

namespace AshDumpLib.HedgehogEngine.Archives;

public class PAC : Archive
{
    public interface IPACSerializable : IExtendedBinarySerializable
    {
        void FinishWriteIndices(ExtendedBinaryWriter writer);
        void FinishWriteChildIndices(ExtendedBinaryWriter writer);
        void FinishWriteNode(ExtendedBinaryWriter writer);
    }

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

    // Mostly taken from HedgeLib++
    public readonly List<ResType> ResTypesMiller = new()
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
        new() { Extension = "icu",               Type = "ResIcuData",                 Location = ResType.ELocation.Root},
        new() { Extension = "cob",               Type = "ResClipmapOcean",            Location = ResType.ELocation.Root},
    };

    ResType GetResTypeByExt(string extension) 
    {
        if(Version.MajorVersion == '4')
        {
            if(Version.MinorVersion == '0')
            {
                if(Version.RevisionVersion == '2')
                    return ResTypesRangers.Find(x => x.Extension == extension);
                else if(Version.RevisionVersion == '5')
                    return ResTypesMiller.Find(x => x.Extension == extension);
                else
                    return ResTypesRangers.Find(x => x.Extension == extension);
            }
            else
                return ResTypesRangers.Find(x => x.Extension == extension);
        }
        else
            return ResTypesRangers.Find(x => x.Extension == extension);
    }
    ResType GetResTypeByType(string type) 
    {
        if (Version.MajorVersion == '4')
        {
            if (Version.MinorVersion == '0')
            {
                if (Version.RevisionVersion == '2')
                    return ResTypesRangers.Find(x => x.Type == type);
                else if (Version.RevisionVersion == '5')
                    return ResTypesMiller.Find(x => x.Type == type);
                else
                    return ResTypesRangers.Find(x => x.Type == type);
            }
            else
                return ResTypesRangers.Find(x => x.Extension == type);
        }
        else
            return ResTypesRangers.Find(x => x.Extension == type);
    }

    struct SplitString
    {
        public string parent;
        public int start;
        public string value;
        public List<string> children;
    }

    static List<string> RemoveDuplicates(List<string> inputList)
    {
        HashSet<string> uniqueStrings = new HashSet<string>(inputList);
        return new List<string>(uniqueStrings);
    }

    static List<List<SplitString>> GetPrefix(List<string> strings)
    {
        strings.Sort();
        List<string> doneStrings = new();
        List<List<SplitString>> prefixes = new();
        foreach (string s in strings)
        {
            List<SplitString> temp = new();
            List<int> matchedLengths = new();
            foreach (var i in strings.Where(x => x != s && x[0] == s[0]))
            {
                int maxLength = Math.Min(s.Length, i.Length);
                int matchedLength = 0;
                for (int j = 0; j < maxLength; j++)
                {
                    if (i[j] == s[j])
                        matchedLength++;
                    else
                        break;
                }
                matchedLengths.Add(matchedLength);
            }
            matchedLengths.Sort();
            if (matchedLengths.Count > 0)
            {
                temp.Add(new() { parent = "", start = 0, value = s.Remove(matchedLengths.First()), children = new() { s.Substring(matchedLengths.First()) } } );
                temp.Add(new() { parent = s.Remove(matchedLengths.First()), start = matchedLengths.First(), value = s.Substring(matchedLengths.First()), children = new() });
            }
            else
                temp.Add(new() { parent = "", start = 0, value = s, children = new() });
            prefixes.Add(temp);
        }

        return prefixes;
    }

    static List<SplitString> GetSplitStrings(List<string> strings)
    {
        List<string> result = new();
        List<List<SplitString>> prefixes = GetPrefix(strings);
        List<SplitString> flattenedPrefixes = new();
        foreach(var i in prefixes)
            foreach(var l in i)
                if(!flattenedPrefixes.Contains(l))
                    flattenedPrefixes.Add(l);
        return flattenedPrefixes;
    }

    static List<byte[]> SplitByteArray(byte[] byteArray, int chunkSize)
    {
        List<byte[]> result = new List<byte[]>();
        int totalLength = byteArray.Length;
        int currentIndex = 0;

        while (currentIndex < totalLength)
        {
            int currentChunkSize = Math.Min(chunkSize, totalLength - currentIndex);
            byte[] chunk = new byte[currentChunkSize];
            Array.Copy(byteArray, currentIndex, chunk, 0, currentChunkSize);
            result.Add(chunk);
            currentIndex += currentChunkSize;
        }

        return result;
    }

    static List<List<IFile>> SplitStructsBySize(List<IFile> inputList, int maxSizeInBytes)
    {
        var result = new List<List<IFile>>();
        var currentList = new List<IFile>();
        int currentSize = 0;

        foreach (var file in inputList)
        {
            int structSize = file.Data.Length;
            if (currentSize + structSize > maxSizeInBytes)
            {
                result.Add(currentList);
                currentList = new();
                currentSize = 0;
            }
            currentList.Add(file);
            currentSize += structSize;
        }

        if (currentList.Count > 0)
            result.Add(currentList);

        return result;
    }

    public PACVersion Version;
    public uint ID = 0;
    public PacType Type;

    public List<string> ParentPaths = new();
    public List<Dependency> dependencies = new();


    public static readonly PACVersion Version402 = new() { MajorVersion = (byte)'4', MinorVersion = (byte)'0', RevisionVersion = (byte)'2' };
    public static readonly PACVersion Version403 = new() { MajorVersion = (byte)'4', MinorVersion = (byte)'0', RevisionVersion = (byte)'3' };
    public static readonly PACVersion Version405 = new() { MajorVersion = (byte)'4', MinorVersion = (byte)'0', RevisionVersion = (byte)'5' };


    public PAC() { }

    public PAC(string filepath) => Open(filepath);
    public PAC(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        //reader.ReadSignature(Signature);
        byte[] signature = reader.ReadArray<byte>(4);
        Header header = reader.Read<Header>();
        ID = header.id;
        Version = header.version;
        if (header.version.MajorVersion == '4')
        {
            if (header.version.MinorVersion == '0')
            {
                if (header.version.RevisionVersion == '2')
                {
                    reader.FileVersion = 402;
                    ReadV2(reader);
                }
                else if (header.version.RevisionVersion == '3' || Version.RevisionVersion == '5')
                {
                    if (signature[0] == 'P')
                    {
                        reader.FileVersion = 403;
                        ReadV3(reader);
                    }
                    else
                    {
                        reader.FileVersion = 405;
                        ReadV2(reader);
                    }  
                }
                    
                else
                    throw new Exception("Unimplemented Version!");
            }
            else
                throw new Exception("Unimplemented Version!");
        }
        else
            throw new Exception("Unimplemented Version!");
    }

    public static uint GenerateID()
    {
        Random rnd = new();
        byte[] ids = new byte[4];
        for (int i = 0; i < 4; i++)
            ids[i] = (byte)rnd.Next(0, 256);
        return BitConverter.ToUInt32(ids, 0);
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        if (writer.FileVersion != 4051)
            writer.WriteSignature(Signature);
        else
            writer.WriteNulls(4);
        writer.Write(Version);
        writer.Write((byte)'L');
        if (ID == 0)
            ID = GenerateID();
        writer.Write(ID);
        writer.AddOffset("fileSize", false);
        if (Version.MajorVersion == '4')
        {
            if (Version.MinorVersion == '0')
            {
                if (Version.RevisionVersion == '2')
                    WriteV2(writer);
                else if (Version.RevisionVersion == '3' || Version.RevisionVersion == '5')
                {
                    if (writer.FileVersion == 4051)
                        WriteV2(writer);
                    else
                        WriteV3(writer);
                }  
                else
                    throw new Exception("Unimplemented Version!");
            }
            else
                throw new Exception("Unimplemented Version!");
        }
        else
            throw new Exception("Unimplemented Version!");
        int fileSize = (int)writer.Position;
        writer.WriteAt(fileSize, writer.GetOffset("fileSize"));
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

                                case "asm":
                                    Animator asm = new();
                                    asm.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(asm);
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

                                case "effdb":
                                    ParticleLocator particleLocator = new();
                                    particleLocator.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(particleLocator);
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

                                case "shader-list":
                                    ShaderList shaderList = new();
                                    shaderList.Open($"{name}.{tree.nodes[i].data.nodes[x].data.extension}", data);
                                    AddFile(shaderList);
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
        if (dMetadata.dependencyCount > 0 && dMetadata.dependencyTableSize > 0)
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
        //int fileSize = reader.ReadValueAtOffset<int>(0x0C);
        //reader.Seek(fileSize - dMetadata.offTableSize, SeekOrigin.Begin);
        //int lastOffset = 0;
        //string offsets = "";
        //offsets += $"{FilePath}\n";
        //List<int> offsetsParsed = new();
        //while (true)
        //{
        //    byte firstByte = reader.Read<byte>();
        //    int offsetLengthIdentifier = firstByte >> 6; // Get the first two bits

        //    if (offsetLengthIdentifier == 0b00) // 0 bits long means end of table
        //        break;

        //    int difference;
        //    if (offsetLengthIdentifier == 0b01) // 6-bit offset
        //    {
        //        difference = (firstByte & 0x3F) << 2; // Mask out 6 bits, shift left by 2
        //    }
        //    else if (offsetLengthIdentifier == 0b10) // 14-bit offset
        //    {
        //        byte secondByte = reader.ReadByte();
        //        difference = (((firstByte & 0x3F) << 8) | secondByte) << 2; // Combine bytes, shift left by 2
        //    }
        //    else if (offsetLengthIdentifier == 0b11) // 30-bit offset
        //    {
        //        byte secondByte = reader.ReadByte();
        //        byte thirdByte = reader.ReadByte();
        //        byte fourthByte = reader.ReadByte();
        //        difference = (((firstByte & 0x3F) << 24) | (secondByte << 16) | (thirdByte << 8) | fourthByte) << 2;
        //    }
        //    else
        //    {
        //        throw new InvalidDataException("Invalid offset length identifier.");
        //    }

        //    // Calculate absolute position of the current offset
        //    int currentOffset = lastOffset + difference;
        //    offsetsParsed.Add(currentOffset);
        //    lastOffset = currentOffset;
        //}
        //foreach(var x in offsetsParsed)
        //{
        //    reader.Seek(x, SeekOrigin.Begin);
        //    int test = reader.Read<int>();
        //    string strin = "";
        //    if (test > (fileSize - (dMetadata.offTableSize + dMetadata.fileDataSize + dMetadata.strTableSize)))
        //    {
        //        reader.Skip(-4);
        //        strin = reader.ReadStringTableEntry64();
        //    } 
        //    offsets += $"{x} - {test} - {strin}\n";
        //}
        //File.WriteAllText(FilePath + ".offsets.txt", offsets);
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
        writer.Write((short)8);
        writer.Write(dependencies.Count);
        Tree<Node<Tree<Node<FileNode>>>> tree = new();
        Random rnd = new();
        tree.ID = rnd.Next();
        Files.Sort((x, y) => string.Compare(x.Extension, y.Extension));
        List<string> resTypes = new();
        foreach(var i in Files)
            resTypes.Add(GetResTypeByExt(i.Extension).Type);
        Dictionary<string, List<IFile>> filesSorted = new();
        foreach (var i in RemoveDuplicates(resTypes))
            if(i != null)
                filesSorted.Add(i, new());
        foreach(var i in Files)
            if (i != null)
                filesSorted[GetResTypeByExt(i.Extension).Type].Add(i);
        Node<Tree<Node<FileNode>>> mainNode = new();
        mainNode.name = "";
        mainNode.parentIndex = -1;
        mainNode.globalIndex = 0;
        mainNode.dataIndex = -1;
        mainNode.childIndices.Add(1);
        mainNode.bufferStartIndex = 0;
        tree.nodes.Add(mainNode);
        List<SplitString> f = new();
        if(filesSorted.Count > 1)
        {
            for (int i = 0; i < resTypes.Count; i++)
                resTypes[i] = resTypes[i].Replace("Res", "");
            var g = GetSplitStrings(RemoveDuplicates(resTypes));
            f = g.GroupBy(x => x.value).Select(group => new SplitString() { value = group.Key, children = group.SelectMany(s => s.children).Distinct().ToList(), parent = group.Select(s => s.parent).First(), start = group.Select(s => s.start).First() }).ToList();
            for (int i = 0; i < f.Count; i++)
            {
                SplitString s = f[i];
                s.start += 3;
                if (s.parent == "")
                    s.parent = "Res";
                f[i] = s;
            }
            List<string> childrenRes = new();
            foreach (var i in f)
            {
                if (i.parent == "Res" && !childrenRes.Contains(i.value))
                    childrenRes.Add(i.value);
            }
            f.Insert(0, new() { parent = "", start = 0, value = "Res", children = childrenRes });
        }
        else
        {
            f.Add(new() { parent = "", start = 0, value = resTypes[0], children = new() });
        }

        int index = 1;
        int dataIndex = 0;

        foreach(var i in f)
        {
            Node<Tree<Node<FileNode>>> node = new();
            node.ID = (int)GenerateID();
            node.name = i.value;
            if(i.parent != "")
                node.parentIndex = tree.nodes.Find(x => x.name == i.parent).globalIndex;
            else
                node.parentIndex = 0;
            node.globalIndex = index;
            node.bufferStartIndex = i.start;
            node.dataIndex = -1;
            if (i.children.Count == 0)
            {
                Node<Tree<Node<FileNode>>> dataNode = new();
                node.childIndices.Add(index + 1);
                dataNode.ID = Random.Shared.Next();
                dataNode.name = "";
                dataNode.parentIndex = index;
                dataNode.globalIndex = index + 1;
                dataNode.dataIndex = dataIndex;
                dataNode.bufferStartIndex = i.start + i.value.Length;
                dataNode.data = new();
                Node<FileNode> mainfileNode = new();
                mainfileNode.ID = Random.Shared.Next();
                mainfileNode.name = "";
                mainfileNode.parentIndex = -1;
                mainfileNode.globalIndex = 0;
                mainfileNode.dataIndex = -1;
                mainfileNode.bufferStartIndex = 0;
                dataNode.data.nodes.Add(mainfileNode);
                tree.indices.Add(index + 1);
                int filedataIndex = 1;
                int filedatadataindex = 0;
                foreach(var x in filesSorted[ResTypesRangers.Find(l => l.Type.Contains(i.parent + i.value)).Type])
                {
                    Node<FileNode> fileNode = new();
                    fileNode.ID = Random.Shared.Next();
                    fileNode.name = x.FileName.Replace($".{x.Extension}", "");
                    fileNode.parentIndex = 0;
                    fileNode.globalIndex = filedataIndex;
                    fileNode.dataIndex = -1;
                    dataNode.data.nodes[0].childIndices.Add(filedataIndex);
                    fileNode.childIndices.Add(filedataIndex + 1);
                    fileNode.bufferStartIndex = 0;
                    dataNode.data.nodes.Add(fileNode);
                    Node<FileNode> fileDataNode = new();
                    fileDataNode.data = new();
                    fileDataNode.ID = Random.Shared.Next();
                    fileDataNode.name = "";
                    fileDataNode.parentIndex = filedataIndex;
                    fileDataNode.globalIndex = filedataIndex + 1;
                    fileDataNode.dataIndex = filedatadataindex;
                    fileDataNode.bufferStartIndex = x.FileName.Replace($".{x.Extension}", "").Length;
                    fileDataNode.data.uid = (int)GenerateID();
                    fileDataNode.data.extension = x.Extension;
                    fileDataNode.data.flags = 0;
                    fileDataNode.data.data = x.Data;
                    if (GetResTypeByExt(x.Extension).Location == ResType.ELocation.Split && Type != PacType.Split)
                        fileDataNode.data.flags = 1;
                    if (x.Data[0] == 'B' && x.Data[1] == 'I' && x.Data[2] == 'N' && x.Data[3] == 'A')
                        fileDataNode.data.flags = 2;
                    dataNode.data.nodes.Add(fileDataNode);
                    dataNode.data.indices.Add(filedataIndex + 1);
                    filedataIndex++;
                    filedataIndex++;
                    filedatadataindex++;
                }
                tree.nodes.Add(node);
                tree.nodes.Add(dataNode);
                index++;
                dataIndex++;
            }
            else
                tree.nodes.Add(node);
            index++;
        }
        index = 1;
        foreach(var i in f)
        {
            if (i.children != null && i.children.Count > 0)
            {
                foreach (var y in i.children)
                    tree.nodes[index].childIndices.Add(tree.nodes.Find(x => x.name == y).globalIndex);
            }
            index++;
        }
        tree.idname = "mainTree";
        int treesSize = (int)writer.Position;
        tree.Write(writer);
        tree.FinishWrite(writer);
        foreach (var i in tree.nodes)
        {
            if (i.data != null)
            {
                writer.SetOffset(i.globalIndex + "data" + i.ID);
                i.data.Write(writer);
                i.data.FinishWrite(writer);
            }
        }
        tree.FinishWriteIndices(writer);
        tree.FinishWriteChildIndices(writer);
        writer.Align(8);
        treesSize = (int)writer.Position - treesSize;
        writer.WriteAt(treesSize, writer.GetOffset("treesSize"));
        if(dependencies.Count > 0)
        {
            int dependsSize = (int)writer.Position;
            writer.Write((long)dependencies.Count);
            writer.AddOffset("depends");
            writer.WriteNulls(4);
            writer.SetOffset("depends");
            foreach (var i in dependencies)
                i.Write(writer);
            foreach (var i in dependencies)
                i.FinishWrite(writer);
            writer.Align(8);
            dependsSize = (int)writer.Position - dependsSize;
            writer.WriteAt(dependsSize, writer.GetOffset("depTableSize"));
        }
        int dataEntriesSize = (int)writer.Position;
        foreach (var i in tree.nodes)
        {
            if (i.data != null)
            {
                foreach(var x in i.data.nodes)
                {
                    if(x.data != null)
                    {
                        writer.SetOffset(x.globalIndex + "data" + x.ID);
                        x.data.Write(writer);
                        x.data.FinishWrite(writer);
                    }
                }
            }
        }
        writer.Align(8);

        dataEntriesSize = (int)writer.Position - dataEntriesSize;
        
        writer.WriteAt(dataEntriesSize, writer.GetOffset("dataEntriesSize"));

        int stringTableOffset = (int)writer.Position;

        int stringTableSize = (int)writer.Position;

        foreach (var i in writer.StringTableOffsets)
        {
            writer.Seek(i.Key, SeekOrigin.Begin);
            writer.Write(i.Value + stringTableOffset);
        }
        writer.Seek(stringTableOffset, SeekOrigin.Begin);
        foreach (var i in writer.StringTable)
            writer.WriteChar(i);

        writer.Align(4);

        stringTableSize = (int)writer.Position - stringTableSize;

        writer.WriteAt(stringTableSize, writer.GetOffset("strTableSize"));

        int fileDataSize = (int)writer.Position;
        foreach(var i in tree.indices)
            foreach(var x in tree.nodes[i].data.indices)
            {
                writer.Align(16);
                tree.nodes[i].data.nodes[x].data.WriteData(writer);
            }

        fileDataSize = (int)writer.Position - fileDataSize;

        writer.WriteAt(fileDataSize, writer.GetOffset("fileDataSize"));

        int offsetSize = (int)writer.Position;
        long lastOffsetPos = 0;
        foreach (var i in writer.Offsets)
        {
            if (writer.OffsetsWrite[i.Key])
            {
                int difference = (int)(i.Value - lastOffsetPos) >> 2;
                if (difference <= 0x3F)
                {
                    int x = difference & 0x3F;
                    writer.Write((byte)((byte)64 | x));
                }
                else if(difference <= 0x3FFF)
                {
                    int x = difference & 0x3FFF;
                    writer.Write((byte)((byte)128 | (x >> 8)));
                    writer.Write((byte)(x & 0xFF));
                }
                else if (difference <= 0x3FFFFFFF)
                {
                    int x = difference & 0x3FFFFFFF;
                    writer.Write((byte)((byte)192 | (x >> 24)));
                    writer.Write((byte)((x >> 16) & 0xFF));
                    writer.Write((byte)((x >> 8) & 0xFF));
                    writer.Write((byte)(x & 0xFF));
                }
                lastOffsetPos = i.Value;
            }
        }
        writer.WriteNulls(1);
        writer.Align(4);
        offsetSize = (int)writer.Position - offsetSize;

        writer.WriteAt(offsetSize, writer.GetOffset("offTableSize"));
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
                    ParentPaths.Add(reader.ReadStringTableEntry64());
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
        if(rootChunk.uncompressedSize == rootChunk.compressedSize)
        {
            rootChunk.uncompressedData = reader.ReadArray<byte>(rootChunk.uncompressedSize);
        }
        else
        {
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
        }
        reader.Seek(prePos1, SeekOrigin.Begin);
        //File.WriteAllBytes(FilePath + "_og.root", rootChunk.uncompressedData);
        PAC rootPac = new();
        rootPac.Open(FileName + ".root", rootChunk.uncompressedData, parseFiles);
        foreach (var x in rootPac.Files)
            AddFile(x);

        for (int i = 0; i < rootPac.dependencies.Count; i++)
        {
            reader.Seek(rootPac.dependencies[i].dataPos, SeekOrigin.Begin);
            if (rootPac.dependencies[i].main.uncompressedSize == rootPac.dependencies[i].main.compressedSize)
                rootPac.dependencies[i].main.uncompressedData = reader.ReadArray<byte>(rootPac.dependencies[i].main.uncompressedSize);
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
            //File.WriteAllBytes(FilePath + "_og." + i, rootPac.dependencies[i].main.uncompressedData);
            PAC tempPac = new();
            tempPac.Open(rootPac.dependencies[i].name, rootPac.dependencies[i].main.uncompressedData, parseFiles);
            foreach (var x in tempPac.Files)
                AddFile(x);
        }
    }

    void WriteV3(ExtendedBinaryWriter writer)
    {
        List<PAC> depPacs = new();
        List<Dependency> deps = new();
        List<IFile> splitFiles = Files.Where(x => GetResTypeByExt(x.Extension).Location == ResType.ELocation.Split).ToList();
        splitFiles.Sort((x, y) => string.Compare(x.Extension, y.Extension, StringComparison.Ordinal));
        List<List<IFile>> chunkfiles = SplitStructsBySize(splitFiles, 31457280);
        int index = 0;
        foreach(var x in chunkfiles)
        {
            PAC depPac = new();
            depPac.ID = ID;
            depPac.Files = x;
            char revvVersion = '2';
            if (Version.RevisionVersion == '5')
                revvVersion = '5';
            depPac.Version = new() { MajorVersion = Version.MajorVersion, MinorVersion = (byte)'0', RevisionVersion = (byte)revvVersion };
            depPac.Type = PacType.Split;
            Chunk depChunk = new();
            MemoryStream dmemStream = new MemoryStream();
            var write = new ExtendedBinaryWriter(dmemStream, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Little);
            if (Version.RevisionVersion == '5')
                write.FileVersion = 4051;
            depPac.Write(write);
            write.Dispose();
            dmemStream.Dispose();
            dmemStream.Close();
            depChunk.uncompressedData = dmemStream.ToArray();
            List<byte[]> dchunks = SplitByteArray(depChunk.uncompressedData, 65536);
            List<Chunk> ddChunks = new();
            int dcompressedSize = 0;
            foreach (var i in dchunks)
            {
                byte[] unkcompressedChunk = new byte[LZ4Codec.MaximumOutputSize(i.Length)];
                var length = LZ4Codec.Encode(i, unkcompressedChunk, LZ4Level.L00_FAST);
                dcompressedSize += length;
                byte[] compressedChunk = new byte[length];
                Array.Copy(unkcompressedChunk, compressedChunk, length);
                Chunk chunk = new();
                chunk.uncompressedData = i;
                chunk.uncompressedSize = i.Length;
                chunk.compressedSize = compressedChunk.Length;
                chunk.compressedData = compressedChunk;
                ddChunks.Add(chunk);
            }
            Dependency dep = new();
            dep.main = new() { uncompressedSize = depChunk.uncompressedData.Length, compressedSize = dcompressedSize };
            dep.name = $"{FileName}.{index.ToString("D3")}";
            dep.chunks = ddChunks;
            deps.Add(dep);
            depPacs.Add(depPac);
            //File.WriteAllBytes($"{FilePath}.{index}", depChunk.uncompressedData);
            index++;
        }
        PAC rootPac = new();
        rootPac.dependencies = deps;
        rootPac.ID = ID;
        rootPac.Files = Files;
        char revVersion = '2';
        if (Version.RevisionVersion == '5')
            revVersion = '5';
        rootPac.Version = new() { MajorVersion = Version.MajorVersion, MinorVersion = (byte)'0', RevisionVersion = (byte)revVersion };
        bool split = false;
        foreach(var i in Files)
        {
            if(GetResTypeByExt(i.Extension).Location == ResType.ELocation.Split)
            {
                split = true;
                break;
            }
        }
        rootPac.Type = PacType.Root;
        if (split)
            rootPac.Type |= PacType.HasSplits;
        rootPac.Type |= PacType.unk;
        Chunk rootChunk = new();
        MemoryStream memStream = new MemoryStream();
        var writ = new ExtendedBinaryWriter(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Little);
        if(Version.RevisionVersion == '5')
            writ.FileVersion = 4051;
        rootPac.Write(writ);
        int compressedSize = 0;
        rootChunk.uncompressedData = memStream.ToArray();
        List<byte[]> chunks = SplitByteArray(rootChunk.uncompressedData, 65536);
        List<byte[]> compressedChunks = new();
        foreach (var i in chunks)
        {
            byte[] unkcompressedChunk = new byte[LZ4Codec.MaximumOutputSize(i.Length)];
            var length = LZ4Codec.Encode(i, unkcompressedChunk);
            compressedSize += length;
            byte[] compressedChunk = new byte[length];
            Array.Copy(unkcompressedChunk, compressedChunk, length);
            compressedChunks.Add(compressedChunk);
        }
        writer.AddOffset("rootPacOffset", false);
        writer.Write(compressedSize);
        writer.Write(rootChunk.uncompressedData.Length);
        FlagsV4 flags = FlagsV4.unk | FlagsV4.hasMetadata;
        flags |= FlagsV4.hasParents;
        writer.Write((short)flags);
        writer.Write((short)(FlagsV3.unk | FlagsV3.lz4));

        writer.AddOffset("parentsSize", false);
        writer.AddOffset("chunkTableSize", false);
        writer.AddOffset("strTableSize", false);
        writer.AddOffset("offTableSize", false);
        int parentsSize = (int)writer.Position;
        writer.Write((long)ParentPaths.Count);
        writer.AddOffset("parentsTable", true);
        writer.WriteNulls(4);
        writer.SetOffset("parentsTable");
        foreach (var path in ParentPaths)
        {
            writer.AddOffset(path, true);
            writer.WriteNulls(4);
        }
        parentsSize = (int)writer.Position - parentsSize;
        writer.WriteAt(parentsSize, writer.GetOffset("parentsSize"));

        int chunkSize = (int)writer.Position;

        writer.Write(compressedChunks.Count);
        long chunksPos = writer.Position;
        for(int i = 0; i < compressedChunks.Count; i++)
        {
            writer.Write(compressedChunks[i].Length);
            writer.Write(chunks[i].Length);
        }
        writer.Align(8);
        chunkSize = (int)writer.Position - chunkSize;

        writer.WriteAt(chunkSize, writer.GetOffset("chunkTableSize"));

        int strSize = (int)writer.Position;
        foreach (var i in ParentPaths)
        {
            writer.SetOffset(i);
            writer.WriteString(StringBinaryFormat.NullTerminated, i);
        }
        writer.Align(8);
        strSize = (int)writer.Position - strSize;

        writer.WriteAt(strSize, writer.GetOffset("strTableSize"));

        int offsetSize = (int)writer.Position;
        long lastOffsetPos = 0;
        int writtenOffsets = 0;
        foreach (var i in writer.Offsets)
        {
            if (writer.OffsetsWrite[i.Key])
            {
                int x = ((int)i.Value - (int)lastOffsetPos) >> 2;
                if (x <= 63)
                    writer.Write((byte)((byte)64 | x));
                lastOffsetPos = i.Value;
                writtenOffsets++;
            }
        }

        writer.Align(16);
        offsetSize = (int)writer.Position - offsetSize;

        if(writtenOffsets > 0)
            writer.WriteAt(offsetSize, writer.GetOffset("offTableSize"));

        if (deps.Count > 0)
        {
            writ.Seek(writ.GetOffset("depends"), SeekOrigin.Begin);
            writ.Skip(8);
            foreach(var i in deps)
            {
                writ.Skip(16);
                writ.Write((int)writer.Position);
                writ.Skip(12);
                foreach(var x in i.chunks)
                    writer.WriteArray(x.compressedData);
                writer.Align(16);
            }
            int oldComp = compressedSize;
            compressedSize = 0;
            rootChunk.uncompressedData = memStream.ToArray();
            chunks = SplitByteArray(rootChunk.uncompressedData, 65536);
            compressedChunks = new();
            foreach (var i in chunks)
            {
                byte[] unkcompressedChunk = new byte[LZ4Codec.MaximumOutputSize(i.Length)];
                var length = LZ4Codec.Encode(i, unkcompressedChunk, LZ4Level.L00_FAST);
                compressedSize += length;
                byte[] compressedChunk = new byte[length];
                Array.Copy(unkcompressedChunk, compressedChunk, length);
                compressedChunks.Add(compressedChunk);
            }
            if (oldComp != compressedSize)
            {
                writer.WriteAt(compressedSize, 20);
                long prePos = writer.Position;
                writer.Seek(chunksPos, SeekOrigin.Begin);
                for (int i = 0; i < compressedChunks.Count; i++)
                {
                    writer.Write(compressedChunks[i].Length);
                    writer.Write(chunks[i].Length);
                }
                writer.Seek(prePos, SeekOrigin.Begin);
            }
        }

        writer.SetOffset32("rootPacOffset");
        foreach (var i in compressedChunks)
            writer.WriteArray(i);

        writer.Align(16);

        //File.WriteAllBytes(FilePath + ".root", rootChunk.uncompressedData);
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
            if(reader.FileVersion == 405)
                reader.Skip(8);
            else
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
            if (writer.FileVersion == 4051)
                writer.WriteNulls(8);
            else
                writer.WriteStringTableEntry(name);
            writer.Write(main.compressedSize);
            writer.Write(main.uncompressedSize);
            writer.Write(dataPos);
            writer.Write(chunks.Count);
            writer.AddOffset(chunks.Count + name + "chunkOffsets");
            writer.WriteNulls(4);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            writer.SetOffset(chunks.Count + name + "chunkOffsets");
            foreach(var i in chunks)
            {
                writer.Write(i.compressedSize);
                writer.Write(i.uncompressedSize);
            }
        }
    }

    class Tree<T> : IPACSerializable where T : IPACSerializable, new()
    {
        public List<T> nodes = new();
        public List<int> indices = new();
        public int ID = (int)GenerateID();
        public string idname = "";

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
            if (idname == "")
                idname = typeof(T).Name;
            writer.AddOffset(nodes.Count.ToString() + "nodes" + idname + ID);
            writer.WriteNulls(4);
            if (indices.Count > 0)
                writer.AddOffset(indices.Count.ToString() + "indices" + idname + ID);
            else
                writer.WriteNulls(4);
            writer.WriteNulls(4);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            writer.SetOffset(nodes.Count.ToString() + "nodes" + idname + ID);
            foreach (var i in nodes)
                i.Write(writer);
            foreach (var i in nodes)
                i.FinishWrite(writer);
        }

        public void FinishWriteIndices(ExtendedBinaryWriter writer)
        {
            if(indices.Count > 0)
            {
                writer.SetOffset(indices.Count.ToString() + "indices" + idname + ID);
                writer.WriteArray(indices.ToArray());
                writer.Align(8);
            }
            foreach (var i in nodes)
                i.FinishWriteIndices(writer);
        }

        public void FinishWriteChildIndices(ExtendedBinaryWriter writer)
        {
            foreach (var i in nodes)
                i.FinishWriteChildIndices(writer);
        }

        public void FinishWriteNode(ExtendedBinaryWriter writer)
        {
            foreach (var i in nodes)
                i.FinishWriteNode(writer);
        }
    }

    class Node<T> : IPACSerializable where T : IPACSerializable, new()
    {
        public string name = "";
        public int parentIndex;
        public int globalIndex;
        public int dataIndex;
        public List<int> childIndices = new();
        public int bufferStartIndex;
        public T data;
        public int ID = (int)GenerateID();

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
                reader.ReadAtOffset(dataPtr, () => data.Read(reader));
            }
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            if (name.Length > 0)
                writer.WriteStringTableEntry(name);
            else
                writer.WriteNulls(8);
            if (data != null)
                writer.AddOffset(globalIndex + "data" + ID);
            else
                writer.WriteNulls(4);
            writer.Skip(4);
            if (childIndices.Count > 0)
                writer.AddOffset(globalIndex + "indices" + ID);
            else
                writer.WriteNulls(4);
            writer.Skip(4);
            writer.Write(parentIndex);
            writer.Write(globalIndex);
            writer.Write(dataIndex);
            writer.Write((short)childIndices.Count);
            writer.Write(data == null ? (byte)0 : (byte)1);
            writer.Write((byte)bufferStartIndex);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
            
        }

        public void FinishWriteIndices(ExtendedBinaryWriter writer)
        {
            if(data != null)
                data.FinishWriteIndices(writer);
        }

        public void FinishWriteChildIndices(ExtendedBinaryWriter writer)
        {
            if (childIndices.Count > 0)
            {
                writer.SetOffset(globalIndex + "indices" + ID);
                writer.WriteArray(childIndices.ToArray());
                writer.Align(8);
            }
            if (data != null)
                data.FinishWriteChildIndices(writer);
        }

        public void FinishWriteNode(ExtendedBinaryWriter writer)
        {
                  
        }
    }

    class FileNode : IPACSerializable
    {
        public int uid;
        public int dataSize;
        public long dataPtr;
        public long flags;
        public string extension = "";
        public byte[] data = new byte[0];

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
            if (flags != 1)
                writer.Write(uid);
            else
                writer.Skip(4);
            writer.Write(data.Length);
            writer.WriteNulls(8);
            if (flags != 1)
                writer.AddOffset(uid.ToString());
            else
                writer.WriteNulls(4);
            writer.Skip(4);
            writer.WriteNulls(8);
            writer.WriteStringTableEntry(extension);
            writer.Write(flags);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {
        }

        public void WriteData(ExtendedBinaryWriter writer)
        {
            if (flags != 1)
            {
                writer.SetOffset(uid.ToString());
                writer.WriteArray(data);
            }
        }

        public void FinishWriteIndices(ExtendedBinaryWriter writer)
        {

        }

        public void FinishWriteChildIndices(ExtendedBinaryWriter writer)
        {

        }

        public void FinishWriteNode(ExtendedBinaryWriter writer)
        {

        }
    }

    public class Chunk
    {
        public int compressedSize;
        public int uncompressedSize;
        public byte[] compressedData;
        public byte[] uncompressedData;
    }

    enum FlagsV4 : short
    {
        none = 0,
        unk = 1,
        hasParents = 2,
        hasMetadata = 0x80
    }

    enum FlagsV3 : short
    {
        unk = 8,
        deflate = 0x100,
        lz4 = 0x200
    }

    public enum PacType : byte
    {
        Root = 1,
        Split,
        HasSplits = 4,
        unk = 8
    }
}
