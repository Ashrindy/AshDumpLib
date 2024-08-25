// See https://aka.ms/new-console-template for more information
using AshDumpLib.CastleSiege;
using AshDumpLib.HedgehogEngine.Anim;
using AshDumpLib.HedgehogEngine.Archives;
using AshDumpLib.HedgehogEngine.BINA;

Console.WriteLine("Hello, World!");
string filepath = Console.ReadLine();
//PAC pac = new(filepath);
Archive rda = new();
rda.Open(filepath, false);
rda.SaveToFile(filepath + "1");
//foreach(var i in rda.Files)
//{
//    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine("R:\\CastlesStrike", i.FilePath)));
//    File.WriteAllBytes(Path.Combine("R:\\CastlesStrike", i.FilePath), i.Data.ToArray());
//}
//Model rdo = new(filepath);
Console.WriteLine("test");