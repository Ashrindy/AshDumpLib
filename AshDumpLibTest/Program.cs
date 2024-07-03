// See https://aka.ms/new-console-template for more information
using AshDumpLib.HedgehogEngine.BINA;

Console.WriteLine("Hello, World!");
string filepath = Console.ReadLine();
Probe probe = new Probe(filepath);
probe.Save(filepath + "1");