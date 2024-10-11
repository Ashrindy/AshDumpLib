using AshDumpLib.HedgehogEngine.BINA.Animation;
using AshDumpTool;
using System.Xml;

public class Program
{
    public static void Main(string[] args)
    {
        string filepath = "";
        if (args.Length == 0)
        {
            Console.WriteLine("What's the file?");
            filepath = Console.ReadLine();
        }
        else
            filepath = args[0];

        switch(Path.GetExtension(filepath))
        {
            case ".xml":
                XmlDocument reader = new();
                reader.Load(filepath);
                switch (reader.DocumentElement.Name)
                {
                    case "ParticleLocator":
                        ParticleLocator effdbW = new();
                        effdbW.Version = int.Parse(reader.DocumentElement.Attributes[0].Value);
                        foreach(XmlNode i in reader.DocumentElement.FirstChild.ChildNodes)
                        {
                            ParticleLocator.State state = new();
                            state.StateName = i.Attributes[0].Value;
                            foreach(XmlNode x in i.ChildNodes[0].ChildNodes)
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
                            foreach(XmlNode x in i.ChildNodes[1].ChildNodes)
                                state.SoundNames.Add(x.FirstChild.InnerText);
                            effdbW.States.Add(state);
                        }
                        effdbW.SaveToFile(filepath + ".effdb");
                        break;
                }
                break;

            case ".effdb":
                ParticleLocator effdb = new(filepath);

                ExtendedXmlWriter writer = new(filepath + ".xml", "ParticleLocator", new() { new("version", effdb.Version) } );
                writer.WriteObject("States", () =>
                {
                    foreach(var i in effdb.States)
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
        }
    }
}