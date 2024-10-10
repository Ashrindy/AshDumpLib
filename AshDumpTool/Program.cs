using AshDumpLib.HedgehogEngine.BINA.Animation;
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
            case ".effdb":
                ParticleLocator effdb = new(filepath);

                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                using(XmlWriter writer = XmlWriter.Create(filepath + ".xml", settings))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("ParticleLocator");
                    writer.WriteAttributeString("version", effdb.Version.ToString());
                    foreach (var i in effdb.States)
                    {
                        writer.WriteStartElement("State");
                        writer.WriteAttributeString("stateName", i.StateName);
                        writer.WriteStartElement("Particles");
                        foreach (var x in i.Particles)
                        {
                            writer.WriteStartElement("Particle");

                            writer.WriteAttributeString("particleName", x.ParticleName);
                            writer.WriteAttributeString("boneName", x.BoneName);
                            writer.WriteAttributeString("relativeToBone", x.AttachedToBone.ToString());
                            writer.WriteElementString("usePosition", x.UsePosition.ToString());
                            writer.WriteElementString("useRotation", x.UseRotation.ToString());
                            writer.WriteElementString("useScale", x.UseScale.ToString());
                            writer.WriteElementString("ignoreRelativeRotation", x.IgnoreRelativeRotation.ToString());

                            writer.WriteStartElement("Position");
                            writer.WriteElementString("X", x.Position.X.ToString());
                            writer.WriteElementString("Y", x.Position.Y.ToString());
                            writer.WriteElementString("Z", x.Position.Z.ToString());
                            writer.WriteEndElement();

                            writer.WriteStartElement("Rotation");
                            writer.WriteElementString("X", x.Rotation.X.ToString());
                            writer.WriteElementString("Y", x.Rotation.Y.ToString());
                            writer.WriteElementString("Z", x.Rotation.Z.ToString());
                            writer.WriteElementString("W", x.Rotation.W.ToString());
                            writer.WriteEndElement();

                            writer.WriteStartElement("Scale");
                            writer.WriteElementString("X", x.Scale.X.ToString());
                            writer.WriteElementString("Y", x.Scale.Y.ToString());
                            writer.WriteElementString("Z", x.Scale.Z.ToString());
                            writer.WriteEndElement();

                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.WriteStartElement("SoundCues");
                        foreach (var x in i.SoundNames)
                        {
                            writer.WriteStartElement("SoundCue");
                            writer.WriteAttributeString("soundCueName", x);
                            writer.WriteEndElement();
                        }
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
                break;
        }
    }
}