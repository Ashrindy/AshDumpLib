﻿// See https://aka.ms/new-console-template for more information
using AshDumpLib.CastleSiege;
using AshDumpLib.HedgehogEngine.Anim;
using AshDumpLib.HedgehogEngine.Archives;
using AshDumpLib.HedgehogEngine.BINA;
Console.WriteLine("Hello, World!");
string filepath = Console.ReadLine();
//PAC pac = new(filepath);
//ObjectWorld gedit = new(filepath, "forces.json");
//gedit.SaveToFile(filepath + "t");
//Text txt = new(filepath);
//txt.SaveToFile(filepath + "t");
CameraAnimation camAnim = new(filepath);
camAnim.SaveToFile(filepath + "t");

Console.WriteLine("test");