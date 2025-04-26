using Amicitia.IO.Binary;
using AshDumpLib.HedgehogEngine.BINA.Animation;
using AshDumpLib.HedgehogEngine.BINA.Density;
using AshDumpLib.HedgehogEngine.BINA.RFL;
using AshDumpLib.HedgehogEngine.BINA.Misc;
using AshDumpLib.HedgehogEngine.BINA.Terrain;
using AshDumpLib.HedgehogEngine.Mirage.Anim;
using AshDumpLib.Helpers.Archives;
using K4os.Compression.LZ4;
using System.Diagnostics.CodeAnalysis;
using AshDumpLib.HedgehogEngine.BINA.Converse;

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

        public override string ToString()
        {
            return $"{Extension} - {Type}";
        }
    }

    // Taken from HedgeLib++
    public static readonly List<ResType> ResTypesWars = new()
    {
        new() { Extension = "dds",                  Type = "ResTexture",                   Location = ResType.ELocation.Split },
        new() { Extension = "model",                Type = "ResModel",                     Location = ResType.ELocation.Split },
        new() { Extension = "terrain-model",        Type = "ResMirageTerrainModel",        Location = ResType.ELocation.Split },
        new() { Extension = "material",             Type = "ResMirageMaterial",            Location = ResType.ELocation.Split },
        new() { Extension = "swif",                 Type = "ResSurfRideProject",           Location = ResType.ELocation.Root },
        new() { Extension = "terrain-instanceinfo", Type = "ResMirageTerrainInstanceInfo", Location = ResType.ELocation.Root },
        new() { Extension = "uv-anim",              Type = "ResAnimTexSrt",                Location = ResType.ELocation.Split },
        new() { Extension = "cemt",                 Type = "ResCyanEffect",                Location = ResType.ELocation.Root },
        new() { Extension = "rfl",                  Type = "ResReflection",                Location = ResType.ELocation.Root },
        new() { Extension = "skl.hkx",              Type = "ResSkeleton",                  Location = ResType.ELocation.Root },
        new() { Extension = "anm.hkx",              Type = "ResAnimSkeleton",              Location = ResType.ELocation.Root },
        new() { Extension = "mat-anim",             Type = "ResAnimMaterial",              Location = ResType.ELocation.Split },
        new() { Extension = "codetbl",              Type = "ResCodeTable",                 Location = ResType.ELocation.Root },
        new() { Extension = "cnvrs-text",           Type = "ResText",                      Location = ResType.ELocation.Root },
        new() { Extension = "light",                Type = "ResMirageLight",               Location = ResType.ELocation.Root },
        new() { Extension = "asm",                  Type = "ResAnimator",                  Location = ResType.ELocation.V2Merged },
        new() { Extension = "model-instanceinfo",   Type = "ResModelInstanceInfo",         Location = ResType.ELocation.V2Merged },
        new() { Extension = "cam-anim",             Type = "ResAnimCameraContainer",       Location = ResType.ELocation.Split },
        new() { Extension = "gedit",                Type = "ResObjectWorld",               Location = ResType.ELocation.Root },
        new() { Extension = "phy.hkx",              Type = "ResHavokMesh",                 Location = ResType.ELocation.Root },
        new() { Extension = "vis-anim",             Type = "ResAnimVis",                   Location = ResType.ELocation.Split },
        new() { Extension = "grass.bin",            Type = "ResTerrainGrassInfo",          Location = ResType.ELocation.Root },
        new() { Extension = "scene",                Type = "ResScene",                     Location = ResType.ELocation.Root },
        new() { Extension = "effdb",                Type = "ResParticleLocation",          Location = ResType.ELocation.V2Merged },
        new() { Extension = "shlf",                 Type = "ResSHLightField",              Location = ResType.ELocation.Root },
        new() { Extension = "gism",                 Type = "ResGismoConfig",               Location = ResType.ELocation.Root },
        new() { Extension = "probe",                Type = "ResProbe",                     Location = ResType.ELocation.V2Merged },
        new() { Extension = "svcol.bin",            Type = "ResSvCol",                     Location = ResType.ELocation.V2Merged },
        new() { Extension = "fxcol.bin",            Type = "ResFxColFile",                 Location = ResType.ELocation.V2Merged },
        new() { Extension = "path",                 Type = "ResSplinePath",                Location = ResType.ELocation.V2Merged },
        new() { Extension = "pt-anim",              Type = "ResAnimTexPat",                Location = ResType.ELocation.Split },
        new() { Extension = "lit-anim",             Type = "ResAnimLightContainer",        Location = ResType.ELocation.Split },
        new() { Extension = "cnvrs-proj",           Type = "ResTextProject",               Location = ResType.ELocation.Root },
        new() { Extension = "cnvrs-meta",           Type = "ResTextMeta",                  Location = ResType.ELocation.Root },
        new() { Extension = "scfnt",                Type = "ResScalableFontSet",           Location = ResType.ELocation.Root },
        new() { Extension = "pso",                  Type = "ResMiragePixelShader",         Location = ResType.ELocation.Split },
        new() { Extension = "vso",                  Type = "ResMirageVertexShader",        Location = ResType.ELocation.Split },
        new() { Extension = "shader-list",          Type = "ResShaderList",                Location = ResType.ELocation.Root },
        new() { Extension = "vib",                  Type = "ResVibration",                 Location = ResType.ELocation.Root },
        new() { Extension = "bfnt",                 Type = "ResBitmapFont",                Location = ResType.ELocation.V2Merged },
    };


    // Taken from HedgeLib++
    public static readonly List<ResType> ResTypesRangers = new()
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
    public static readonly List<ResType> ResTypesMiller = new()
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

    public static ResType GetResTypeByExt(PACVersion Version, string extension) 
    {
        if (Version == PACV3.Version)
            return ResTypesWars.Find(x => x.Extension == extension);
        else if (Version == PACV402.Version || Version == PACV403.Version)
            return ResTypesRangers.Find(x => x.Extension == extension);
        else if (Version == PACV405.Version || Version == PACV405Split.Version)
            return ResTypesMiller.Find(x => x.Extension == extension);
        else
            return new();
    }
    public static ResType GetResTypeByType(PACVersion Version, string type) 
    {
        if (Version == PACV3.Version)
            return ResTypesWars.Find(x => x.Type == type);
        else if (Version == PACV402.Version || Version == PACV403.Version)
            return ResTypesRangers.Find(x => x.Type == type);
        else if (Version == PACV405.Version || Version == PACV405Split.Version)
            return ResTypesMiller.Find(x => x.Type == type);
        else
            return new();
    }

    public struct PACVersion
    {
        public byte MajorVersion;
        public byte MinorVersion;
        public byte RevisionVersion;

        public static bool operator ==(PACVersion a, PACVersion b)
        {
            if (ReferenceEquals(a, b)) return true;
            return a.MajorVersion == b.MajorVersion && a.MinorVersion == b.MinorVersion && a.RevisionVersion == b.RevisionVersion;
        }

        public static bool operator !=(PACVersion a, PACVersion b)
        {
            return !(a == b);
        }
    }

    public PACVersion Version;
    public uint ID = 0;

    public List<string> ParentPaths = new();

    public PAC() { }

    public PAC(string filepath) => Open(filepath);
    public PAC(string filename, byte[] data) => Open(filename, data);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.CurFilePath = this.FilePath;
        string signature = reader.ReadString(StringBinaryFormat.FixedLength, 4);
        Version = reader.Read<PACVersion>();
        byte endianess = reader.Read<byte>();
        ID = reader.Read<uint>();
        uint filesize = reader.Read<uint>();
        if (Version == PACV3.Version)
        {
            PACV3 pacV3 = new();
            pacV3.Read(reader);
            Files.AddRange(pacV3.files);
        }
        else if (Version == PACV403.Version)
        {
            PACV403 pacV403 = new();
            pacV403.Read(reader);
            Files.AddRange(pacV403.files);
            ParentPaths.AddRange(pacV403.parents);
        }
        else if (Version == PACV402.Version)
        {
            PACV402 pacV402 = new();
            pacV402.Read(reader);
            Files.AddRange(pacV402.files);
            /*ExtendedBinaryWriter writer = new("test.pac", Endianness.Little, System.Text.Encoding.UTF8);
            writer.WriteSignature(signature);
            writer.Write(version);
            writer.Write(endianess);
            writer.Write(ID);
            writer.Write(filesize);
            pacV402.Write(writer);
            writer.Dispose();*/
        }
        else if (Version == PACV405Split.Version && signature == "")
        {
            PACV405Split pacV405 = new();
            pacV405.Read(reader);
            Files.AddRange(pacV405.files);
        }
        else if (Version == PACV405.Version)
        {
            PACV405 pacV405 = new();
            pacV405.Read(reader);
            Files.AddRange(pacV405.files);
            ParentPaths.AddRange(pacV405.parents);
        }
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
        throw new Exception("Writing is not finished yet!");
        writer.CurFilePath = this.FilePath;
        if (Version != PACV405Split.Version)
            writer.WriteString(StringBinaryFormat.FixedLength, Signature, 4);
        else
            writer.WriteNulls(4);
        writer.Write(Version);
        writer.Write('L');
        writer.Write(ID);
        writer.Write(0);
        if (Version == PACV3.Version)
        {
            PACV3 pacV3 = new();
            pacV3.files = Files;
            pacV3.Write(writer);
        }
        else if (Version == PACV403.Version)
        {
            PACV403 pacV403 = new();
            pacV403.Write(writer);
        }
        else if (Version == PACV402.Version)
        {
            PACV402 pacV402 = new();
            pacV402.Write(writer);
        }
    }
}