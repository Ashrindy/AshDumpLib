// See https://aka.ms/new-console-template for more information
//using AshDumpLib.CastleSiege;
//using AshDumpLib.HedgehogEngine.Archives;
using AshDumpLib.HedgehogEngine.Mirage.Anim;
using AshDumpLib.HedgehogEngine.Needle;
using AshDumpLib.HedgehogEngine.BINA.RFL;
using AshDumpLib.HedgehogEngine.BINA.Animation;
using AshDumpLib.HedgehogEngine.BINA.Terrain;
using AshDumpLib.HedgehogEngine.BINA.Misc;
using AshDumpLib.HedgehogEngine.Archives;
using AshDumpLib.Helpers.Archives;
using AshDumpLibTest;
using System;
using AshDumpLib.HedgehogEngine.BINA.Converse;
using AshDumpLib.HedgehogEngine.BINA.ScalableFont;
using AshDumpLib.HedgehogEngine.Mirage;
using AshDumpLib.HedgehogEngine.BINA.Density;
using AshDumpLib.HedgehogEngine.BINA;
using Amicitia.IO.Binary;

Console.WriteLine("Hello, World!");
string filepath = Console.ReadLine();
//MaterialAnimation anim = new();
//anim.MaterialName = "chr_supersonic2_fur";
//Material mat = new();
//mat.Name = "default";
//mat.FPS = 30;
//mat.FrameStart = 0;
//mat.FrameEnd = 150;
//mat.FrameInfos = new()
//{
//    new() {InputID = 0, KeyFrames = new()
//    {
//        new(){Frame = 0, Value = 0},
//        new(){Frame = 150, Value = 1},
//    } },
//    new() {InputID = 1, KeyFrames = new()
//    {
//        new(){Frame = 0, Value = 1},
//        new(){Frame = 150, Value = 0},
//    } },
//    new() {InputID = 2, KeyFrames = new()
//    {
//        new(){Frame = 0, Value = 0.5f},
//        new(){Frame = 150, Value = 0},
//    } },
//    new() {InputID = 3, KeyFrames = new()
//    {
//        new(){Frame = 0, Value = 0},
//        new(){Frame = 150, Value = 0.5f},
//    } }
//};
//anim.Materials.Add(mat);
//anim.SaveToFile(filepath);
//ObjectWorld gedit = new(filepath, "frontiers.template.hson.json");
//var i = gedit.ToHson();
//i.Save(filepath.Replace(".gedit", ".hson"));
//var x = ObjectWorld.ToGedit(i);
//x.SaveToFile(filepath + "edited.gedit");
//Reflection rfl = new(filepath, templateFilePath: "frontiers.template.rfl.json", rflName: "MasterTrialParameters");
//rfl.SaveToFile(filepath + "_test.rfl");
//NeedleShader needleShader = new(filepath);
//Animator asm = new(filepath);
//asm.SaveToFile(filepath + "1");
/*foreach(var i in Directory.GetFiles(filepath))
{
    PAC pAC = new();
    pAC.parseFiles = false;
    pAC.Open(i);
    if(pAC.Files.Where(x => x.Extension == "cam-anim").Count() > 0)
        foreach(var l in pAC.Files.Where(x => x.Extension == "cam-anim"))
        {
            CameraAnimation camanim = new();
            camanim.Open(l.FileName, l.Data);
            foreach(var m in camanim.Cameras)
                if(m.AspectRatio > 1.79 || m.AspectRatio < 1.76)
                    Console.WriteLine($"{i}\\{l.FileName}: {m.Name} - bad aspect ratio {m.AspectRatio}");
        }
}*/

//Text ogText = new(filepath);
//string filepath2 = Console.ReadLine();
//Text newText = new(filepath2);
//for(int i = 0; i < ogText.Entries.Count; i++)
//{
//    ogText.Entries[i].Text = newText.Entries[i].Text;
//}

//ogText.SaveToFile(filepath2);

//pAC.parseFiles = false;
//pAC.SaveToFile(filepath + "test.pac");
//PointCloud pcmodel = new(filepath);
//pcmodel.SaveToFile(filepath + "1");
//ParticleLocator effdb = new(filepath);
//effdb.SaveToFile(filepath + ".effdb");
//Probe probe = new(filepath);
//probe.SaveToFile(filepath + "1");

PAC pac = new(filepath);
//pac.SaveToFile(filepath + ".pac");
//PAC pac2 = new(filepath + ".pac");

//List<IFile> missingFiles = new();

//var comparer = new IFileComparer();
//missingFiles = pac.Files.Except(pac2.Files, comparer).ToList();

/*TextMeta meta = new(filepath);
meta.SaveToFile(filepath + ".cnvrs-meta");*/

/*TextProject proj = new(filepath);
proj.SaveToFile(filepath + ".cnvrs-proj");*/

//Text txt = new(filepath);
//txt.SaveToFile(filepath + ".cnvrs-text");

/*OpticalKerning okern = new(filepath);
okern.SaveToFile(filepath + ".okern");*/

/*ScalableFontSet scfnt = new(filepath);
scfnt.SaveToFile(filepath + ".scfnt");*/

/*MasterLevel mlevel = new(filepath);
mlevel.SaveToFile(filepath + ".mlevel");*/

//CameraAnimation camAnim = new(filepath);
//camAnim.SaveToFile(filepath + ".cam-anim");

//PhysicalSkeleton pba = new(filepath);
//pba.SaveToFile(filepath + ".pba");

//PAC pac = new(filepath);
//pac.SaveToFile(filepath + ".pac");

/*Console.WriteLine("Hello, World!");
string filepath1 = Console.ReadLine();

MasterLevel level = new(filepath);
MasterLevel level1 = new(filepath1);
MasterLevel revLevel = new();

revLevel.Levels = level1.Levels.Except(level.Levels).ToList();

revLevel.SaveToFile(filepath1 + "_revisited");*/

/*public partial class Program
{
    public static void Main(string[] args)
    {
        string filepath;
        if (args.Count() > 0) filepath = args[0];
        else
        {
            Console.WriteLine("Filepath");
            filepath = Console.ReadLine();
        }

        DensitySetting setting = new(filepath);
        string result = "";
        foreach(var i in setting.LODGroups)
        {
            if (i.LODGroupReferenceCount == 0) continue;

            result += $"{setting.LODGroups.IndexOf(i)}: {setting.Models[setting.LODGroupReferences[i.LODGroupReferenceOffset].ModelIndex].Name} - {setting.CollisionResourceNames[(int)setting.CollisionDatas[i.CollisionDataIndex1].CollisionReferenceOffset]}\n";
        }

        string arearesult = "";
        foreach (var i in setting.Biomes)
        {
            if (i.BiomeReferenceCount == 0) continue;
            var biomeReference = setting.BiomeReferences[i.BiomeReferenceOffset];
            var lodGroup = setting.LODGroups[biomeReference.Index];

            arearesult += $"{setting.Biomes.IndexOf(i)}: {setting.Models[setting.LODGroupReferences[lodGroup.LODGroupReferenceOffset].ModelIndex].Name} - scaleMin: {biomeReference.Scale.X}; scaleMax: {biomeReference.Scale.Y}; probability: {biomeReference.Probability}\n";
        }

        File.WriteAllText(filepath + ".txt", result);
        File.WriteAllText(filepath + "_area.txt", arearesult);
    }
}*/

/*TerrainInstanceInfo trrinstinfo = new(filepath);
trrinstinfo.SaveToFile(filepath + ".terrain-instanceinfo");*/

/*DensitySetting dSetting = new(filepath);
dSetting.SaveToFile(filepath + ".densitysetting");*/

//DensityPointCloud dpc0 = new(filepath);
/*DensityPointCloud dpc1 = new(filepath + "d");
/*foreach (var i in dpc0.FoliagePoints)
{
    var z = dpc1.FoliagePoints.Find(x => x == i);
    if (z != null)
        z.Flags = i.Flags;
}

dpc1.SaveToFile(filepath + "d");

Console.WriteLine("test");*/

/*BINAWriter file = new("titlemovie.rfl", Amicitia.IO.Binary.Endianness.Little, System.Text.Encoding.UTF8);
file.WriteHeader();
file.Write(false);
file.Align(8);
file.WriteNulls(16 * 8);
file.FinishWrite();
file.Dispose();*/
