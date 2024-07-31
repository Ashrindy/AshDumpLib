// See https://aka.ms/new-console-template for more information
using AshDumpLib.HedgehogEngine.Anim;
using AshDumpLib.HedgehogEngine.BINA;

Console.WriteLine("Hello, World!");
string filepath = Console.ReadLine();
SkeletonPXD anim = new(filepath);
anim.Save(filepath + "1");