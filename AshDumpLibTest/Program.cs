// See https://aka.ms/new-console-template for more information
//using AshDumpLib.CastleSiege;
//using AshDumpLib.HedgehogEngine.Archives;
using AshDumpLib.HedgehogEngine.Mirage.Anim;
using AshDumpLib.HedgehogEngine.Needle;
using AshDumpLib.HedgehogEngine.BINA.RFL;
using AshDumpLib.HedgehogEngine.BINA.Animation;
using AshDumpLib.HedgehogEngine.BINA.Terrain;
using AshDumpLib.HedgehogEngine.BINA.Misc;

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
//foreach(var i in Directory.GetFiles(filepath))
//{
//    PAC pAC = new();
//    pAC.parseFiles = false;
//    pAC.Open(i);
//    if(pAC.Files.Where(x => x.Extension == "cam-anim").Count() > 0)
//        foreach(var l in pAC.Files.Where(x => x.Extension == "cam-anim"))
//        {
//            CameraAnimation camanim = new();
//            camanim.Open(l.FileName, l.Data);
//            foreach(var m in camanim.Cameras)
//                if(m.AspectRatio > 1.79 || m.AspectRatio < 1.76)
//                    Console.WriteLine($"{i}\\{l.FileName}: {m.Name} - bad aspect ratio");
//        }
//}

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

Console.WriteLine("test");