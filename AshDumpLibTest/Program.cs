// See https://aka.ms/new-console-template for more information
using AshDumpLib.CastleSiege;
using AshDumpLib.HedgehogEngine.Archives;
using AshDumpLib.HedgehogEngine.BINA;
using AshDumpLib.HedgehogEngine.Mirage.Anim;
using AshDumpLib.HedgehogEngine.Needle;

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
//ObjectWorld gedit = new(filepath);
//Reflection rfl = new(filepath, rflName: "BossRifleConfig");
//NeedleShader needleShader = new(filepath);
Animator asm = new(filepath);
asm.SaveToFile(filepath + "1");

Console.WriteLine("test");