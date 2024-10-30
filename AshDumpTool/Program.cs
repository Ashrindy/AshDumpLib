using AshDumpLib.HedgehogEngine.Archives;
using AshDumpLib.HedgehogEngine.BINA.Animation;
using AshDumpLib.HedgehogEngine.BINA.Density;
using AshDumpLib.HedgehogEngine.BINA.RFL;
using AshDumpLib.HedgehogEngine.BINA.Terrain;
using AshDumpLib.HedgehogEngine.Needle;
using AshDumpLib.Helpers.Archives;
using Spectre.Console;
using System.Xml;

namespace AshDumpTool;

public class Program
{
    public static void Main(string[] args)
    {
        List<string> filepaths = new();
        if (args.Length == 0)
        {
            var hetable = new Table();
            hetable.AddColumn("Hedgehog Engine 2");
            hetable.AddRow("ParticleLocator (.effdb)");
            hetable.AddRow("PAC (.pac)");
            hetable.AddRow("PointCloudModel (.pcmodel)");
            hetable.AddRow("PointCloudCollision (.pccol)");
            hetable.AddRow("PointCloudLight (.pcrt)");
            hetable.AddRow("DensityPointCloud (.densitypointcloud)");
            //hetable.AddRow("Reflection (.rfl)");
            hetable.AddRow("ObjectWorld (.gedit)");
            AnsiConsole.Write(hetable);

            var castleSiegetable = new Table();
            castleSiegetable.AddColumn("Castle Siege (Castle Strike)");
            castleSiegetable.AddRow("Archive (.rda)");
            AnsiConsole.Write(castleSiegetable);

            Console.WriteLine("What's the file?");
            filepaths.Add(Console.ReadLine());
        }
        else
            filepaths = args.ToList();

        foreach(var filepath in filepaths)
        {
            if (Directory.Exists(filepath))
            {
                var pactable = new Table();
                Console.Clear();
                Console.WriteLine("What .pac type?");
                pactable.AddColumn("Game");
                pactable.AddColumn("Keyword");
                pactable.AddRow("Sonic Frontiers", "rangers");
                pactable.AddRow("Sonic X Shadow Generations", "miller");
                AnsiConsole.Write(pactable);
                string pactype = Console.ReadLine();
                switch (pactype)
                {
                    case "rangers":
                        PAC rPac = new();
                        rPac.parseFiles = false;
                        rPac.FilePath = filepath;
                        rPac.FileName = Path.GetFileName(filepath) + ".pac";
                        rPac.Version = PAC.Version403;
                        foreach (var i in Directory.GetFiles(filepath))
                        {
                            IFile x = new(i, File.ReadAllBytes(i));
                            if (x.Extension == "txt")
                            {
                                using (StreamReader reader = new(i))
                                {
                                    string line;
                                    while ((line = reader.ReadLine()) != null)
                                        rPac.ParentPaths.Add(line);
                                }
                            }
                            else
                                rPac.AddFile(x);
                        }
                        rPac.SaveToFile(filepath + ".pac");
                        return;
                        break;

                    case "miller":
                        PAC mPac = new();
                        mPac.Version = PAC.Version405;
                        foreach (var i in Directory.GetFiles(filepath))
                        {
                            IFile x = new(i, File.ReadAllBytes(i));
                            if (x.Extension == "txt")
                            {
                                using (StreamReader reader = new(i))
                                {
                                    string line;
                                    while ((line = reader.ReadLine()) != null)
                                        mPac.ParentPaths.Add(line);
                                }
                            }
                            else
                                mPac.AddFile(x);
                        }
                        mPac.SaveToFile(filepath + ".pac");
                        return;
                        break;

                    default:
                        Console.WriteLine("Unsupported/wrong keyword!");
                        return;
                        break;
                }
            }

            switch (Path.GetExtension(filepath))
            {
                case ".xml":
                    XmlDocument reader = new();
                    reader.Load(filepath);
                    switch (reader.DocumentElement.Name)
                    {
                        case "ParticleLocator":
                            ParticleLocator effdbW = new();
                            effdbW.Version = int.Parse(reader.DocumentElement.Attributes[0].Value);
                            foreach (XmlNode i in reader.DocumentElement.FirstChild.ChildNodes)
                            {
                                ParticleLocator.State state = new();
                                state.StateName = i.Attributes[0].Value;
                                foreach (XmlNode x in i.ChildNodes[0].ChildNodes)
                                {
                                    ParticleLocator.State.Particle particle = new();
                                    particle.ParticleName = x.Attributes[0].Value;
                                    particle.BoneName = x.Attributes[1].Value;
                                    particle.UsePosition = bool.Parse(x.ChildNodes[0].InnerText);
                                    particle.UseRotation = bool.Parse(x.ChildNodes[1].InnerText);
                                    particle.UseScale = bool.Parse(x.ChildNodes[2].InnerText);
                                    particle.Unk0 = int.Parse(x.ChildNodes[3].InnerText);
                                    particle.IgnoreRelativeRotation = bool.Parse(x.ChildNodes[4].InnerText);
                                    XmlNode position = x.ChildNodes[5];
                                    particle.Position = new(float.Parse(position.ChildNodes[0].InnerText), float.Parse(position.ChildNodes[1].InnerText), float.Parse(position.ChildNodes[2].InnerText));
                                    position = x.ChildNodes[6];
                                    particle.Rotation = new(float.Parse(position.ChildNodes[0].InnerText), float.Parse(position.ChildNodes[1].InnerText), float.Parse(position.ChildNodes[2].InnerText), float.Parse(position.ChildNodes[3].InnerText));
                                    position = x.ChildNodes[7];
                                    particle.Scale = new(float.Parse(position.ChildNodes[0].InnerText), float.Parse(position.ChildNodes[1].InnerText), float.Parse(position.ChildNodes[2].InnerText));
                                    state.Particles.Add(particle);
                                }
                                foreach (XmlNode x in i.ChildNodes[1].ChildNodes)
                                    state.SoundNames.Add(x.FirstChild.InnerText);
                                effdbW.States.Add(state);
                            }
                            effdbW.SaveToFile(filepath.Replace(".xml", ".effdb"));
                            break;

                        case "PointCloudModel" or "PointCloudCollision" or "PointCloudLight":
                            PointCloud pcmodelW = new();
                            pcmodelW.Version = int.Parse(reader.DocumentElement.Attributes[0].Value);
                            foreach (XmlNode i in reader.DocumentElement.FirstChild.ChildNodes)
                            {
                                PointCloud.Point point = new();
                                point.InstanceName = i.Attributes[0].Value;
                                point.ResourceName = i.Attributes[1].Value;
                                XmlNode position = i.ChildNodes[0];
                                point.Position = new(float.Parse(position.ChildNodes[0].InnerText), float.Parse(position.ChildNodes[1].InnerText), float.Parse(position.ChildNodes[2].InnerText));
                                position = i.ChildNodes[1];
                                point.Rotation = new(float.Parse(position.ChildNodes[0].InnerText), float.Parse(position.ChildNodes[1].InnerText), float.Parse(position.ChildNodes[2].InnerText));
                                position = i.ChildNodes[2];
                                point.Scale = new(float.Parse(position.ChildNodes[0].InnerText), float.Parse(position.ChildNodes[1].InnerText), float.Parse(position.ChildNodes[2].InnerText));
                                pcmodelW.Points.Add(point);
                            }
                            string pcmodelExtension = ".pcmodel";
                            switch (reader.DocumentElement.Name)
                            {
                                case "PointCloudCollision":
                                    pcmodelExtension = ".pccol";
                                    break;

                                case "PointCloudLight":
                                    pcmodelExtension = ".pcrt";
                                    break;
                            }
                            pcmodelW.SaveToFile(filepath.Replace(".xml", pcmodelExtension));
                            break;

                        case "DensityPointCloud":
                            DensityPointCloud pcdensityW = new();
                            pcdensityW.Version = int.Parse(reader.DocumentElement.Attributes[0].Value);
                            foreach (XmlNode i in reader.DocumentElement.FirstChild.ChildNodes)
                            {
                                DensityPointCloud.FoliagePoint point = new();
                                point.ID = int.Parse(i.Attributes[0].Value);
                                point.Unk = int.Parse(i.Attributes[1].Value);
                                XmlNode position = i.ChildNodes[0];
                                point.Position = new(float.Parse(position.ChildNodes[0].InnerText), float.Parse(position.ChildNodes[1].InnerText), float.Parse(position.ChildNodes[2].InnerText));
                                position = i.ChildNodes[1];
                                point.Rotation = new(float.Parse(position.ChildNodes[0].InnerText), float.Parse(position.ChildNodes[1].InnerText), float.Parse(position.ChildNodes[2].InnerText), float.Parse(position.ChildNodes[3].InnerText));
                                position = i.ChildNodes[2];
                                point.Scale = new(float.Parse(position.ChildNodes[0].InnerText), float.Parse(position.ChildNodes[1].InnerText), float.Parse(position.ChildNodes[2].InnerText));
                                pcdensityW.FoliagePoints.Add(point);
                            }
                            pcdensityW.SaveToFile(filepath.Replace(".xml", ".densitypointcloud"));
                            break;
                    }
                    break;

                //Hedgehog Engine 2

                case ".effdb":
                    ParticleLocator effdb = new(filepath);

                    ExtendedXmlWriter writer = new(filepath.Replace(".effdb", ".xml"), "ParticleLocator", new() { new("version", effdb.Version) });
                    writer.WriteObject("States", () =>
                    {
                        foreach (var i in effdb.States)
                        {
                            writer.WriteObject("State", new() { new("StateName", i.StateName) }, () =>
                            {
                                writer.WriteObject("Particles", () =>
                                {
                                    foreach (var x in i.Particles)
                                    {
                                        writer.WriteObject("Particle", new() { new("ParticleName", x.ParticleName), new("BoneName", x.BoneName) }, () =>
                                        {
                                            writer.Write("UsePosition", x.UsePosition);
                                            writer.Write("UseRotation", x.UseRotation);
                                            writer.Write("UseScale", x.UseScale);
                                            writer.Write("Unk0", x.Unk0);
                                            writer.Write("IgnoreRelativeRotation", x.IgnoreRelativeRotation);
                                            writer.Write("Position", x.Position);
                                            writer.Write("Rotation", x.Rotation);
                                            writer.Write("Scale", x.Scale);
                                        });
                                    }
                                });
                                writer.WriteObject("SoundCues", () =>
                                {
                                    foreach (var x in i.SoundNames)
                                    {
                                        writer.Write("SoundCue", x);
                                    }
                                });
                            });
                        }
                    });
                    writer.Close();
                    break;

                case ".pac" or ".levels":
                    PAC pAC = new();
                    pAC.parseFiles = false;
                    pAC.Open(filepath);
                    Directory.CreateDirectory(filepath.Replace(Path.GetExtension(filepath), ""));
                    foreach (var i in pAC.Files)
                        File.WriteAllBytes($"{filepath.Replace(Path.GetExtension(filepath), "")}/{i.FileName}", i.Data);
                    string dependencies = "";
                    foreach (var i in pAC.ParentPaths)
                        dependencies += $"{i}\0";
                    if (pAC.ParentPaths.Count > 0)
                        File.WriteAllText($"{filepath.Replace(Path.GetExtension(filepath), "")}/!DEPENDENCIES.txt", dependencies);
                    break;

                case ".pcmodel" or ".pccol" or ".pcrt":
                    PointCloud pcmodel = new(filepath);

                    string mainName = "PointCloudModel";
                    switch (Path.GetExtension(filepath))
                    {
                        case ".pccol":
                            mainName = "PointCloudCollision";
                            break;

                        case ".pcrt":
                            mainName = "PointCloudLight";
                            break;
                    }
                    ExtendedXmlWriter pcmodelwriter = new(filepath.Replace(Path.GetExtension(filepath), ".xml"), mainName, new() { new("version", pcmodel.Version) });
                    pcmodelwriter.WriteObject("Points", () =>
                    {
                        foreach (var i in pcmodel.Points)
                        {
                            pcmodelwriter.WriteObject("Point", new() { new("InstanceName", i.InstanceName), new("ResourceName", i.ResourceName) }, () =>
                            {
                                pcmodelwriter.Write("Position", i.Position);
                                pcmodelwriter.Write("Rotation", i.Rotation);
                                pcmodelwriter.Write("Scale", i.Scale);
                            });
                        }
                    });
                    pcmodelwriter.Close();
                    break;

                case ".densitypointcloud":
                    DensityPointCloud pcdensity = new(filepath);

                    ExtendedXmlWriter pcdensitywriter = new(filepath.Replace(".densitypointcloud", ".xml"), "DensityPointCloud", new() { new("version", pcdensity.Version) });
                    pcdensitywriter.WriteObject("Points", () =>
                    {
                        foreach (var i in pcdensity.FoliagePoints)
                        {
                            pcdensitywriter.WriteObject("FoliagePoint", new() { new("ID", i.ID), new("Unk", i.Unk) }, () =>
                            {
                                pcdensitywriter.Write("Position", i.Position);
                                pcdensitywriter.Write("Rotation", i.Rotation);
                                pcdensitywriter.Write("Scale", i.Scale);
                            });
                        }
                    });
                    pcdensitywriter.Close();
                    break;

                //case ".rfl":
                //    Console.Clear();
                //    Console.WriteLine("What is the template?");
                //    string template = Console.ReadLine();
                //    Console.WriteLine("What is the RFLClass name?");
                //    string rflClassName = Console.ReadLine();
                //    Reflection rfl = new(filepath, template, rflClassName);
                //    ExtendedXmlWriter xmlRflWriter = new(filepath.Replace(".rfl", ".xml"));
                //    ReflectionWriter rflWriter = new(xmlRflWriter, rfl.Parameters);
                //    rflWriter.Write();
                //    xmlRflWriter.Close();
                //    break;

                case ".gedit":
                    Console.Clear();
                    Console.WriteLine("What is the template?");
                    string gTemplate = Console.ReadLine();
                    ObjectWorld gedit = new(filepath, gTemplate);
                    gedit.ToHson().Save(filepath.Replace(".gedit", ".hson"), jsonOptions: new() { Indented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, SkipValidation = true });
                    break;

                case ".hson":
                    Console.Clear();
                    Console.WriteLine("What is the template?");
                    string hTemplate = Console.ReadLine();
                    ObjectWorld.TemplateData = ReflectionData.Template.GetTemplateFromFilePath(hTemplate);
                    libHSON.Project hson = new();
                    hson.Load(filepath);
                    ObjectWorld.ToGedit(hson).SaveToFile(filepath.Replace(".hson", ".gedit"));
                    break;

                //Castle Siege

                case ".rda" or ".RDA":
                    AshDumpLib.CastleSiege.Archive rda = new();
                    rda.parseFiles = false;
                    rda.Open(filepath);
                    Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(filepath), "data"));
                    foreach (var i in rda.Files)
                    {
                        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(filepath), Path.GetDirectoryName(i.FilePath)));
                        File.WriteAllBytes($"{Path.Combine(Path.GetDirectoryName(filepath), Path.GetDirectoryName(i.FilePath))}/{i.FileName}", i.Data);
                    }
                    break;

                default:
                    Console.WriteLine("Unsupported file format!");
                    break;
            }
        }
    }
}