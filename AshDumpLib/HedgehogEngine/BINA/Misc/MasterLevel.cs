using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine.BINA.Misc;

public class MasterLevel : IFile
{
    public const string FileExtension = ".mlevel";
    public const string BINASignature = "LMEH";

    public List<Level> Levels = new();

    public MasterLevel() { }

    public MasterLevel(string filename) => Open(filename);
    public MasterLevel(string filename, byte[] data) => Open(filename, data);

    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public override void WriteBuffer() { MemoryStream memStream = new(); BINAWriter writer = new(memStream, Amicitia.IO.Streams.StreamOwnership.Retain, endianness); Write(writer); Data = memStream.ToArray(); }

    public void Read(BINAReader reader)
    {
        reader.ReadHeader();
        reader.ReadSignature(BINASignature);
        reader.Align(8);
        long levelCount = reader.Read<long>();
        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
        {
            for(int i = 0; i < levelCount; i++)
            {
                Level lvl = new();
                lvl.Read(reader);
                Levels.Add(lvl);
            }    
        });
        reader.Dispose();
    }

    public void Write(BINAWriter writer)
    {
        writer.WriteHeader();
        writer.WriteSignature(BINASignature);
        writer.Align(8);
        writer.Write((long)Levels.Count);
        writer.AddOffset("levelTable");
        writer.Align(16);
        writer.SetOffset("levelTable");
        foreach (var i in Levels)
            i.Write(writer);
        writer.Align(16);
        foreach (var i in Levels)
            i.FinishWrite(writer);
        foreach (var i in Levels)
            i.LastWrite(writer);
        writer.FinishWrite();
        writer.Dispose();
    }

    public class Level : IBINASerializable
    {
        public struct File
        {
            public string Filepath;
            public string Root; //not sure, since all of them are "", i'm just assuming based on the nature of these types of file formats
        }

        public string Name = "";
        public List<File> Files = new();
        public bool Unk0 = false;
        public List<string> Dependencies = new();

        public void Read(BINAReader reader)
        {
            reader.ReadAtOffset(reader.Read<long>() + 64, () =>
            {
                Name = reader.ReadStringTableEntry();
                int fileCount = reader.Read<int>();
                int depCount = reader.Read<int>();
                reader.ReadAtOffset(reader.Read<long>() + 64, () =>
                {
                    for(int i = 0; i < fileCount; i++)
                        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
                        {
                            Files.Add(new() { Filepath = reader.ReadStringTableEntry(), Root = reader.ReadStringTableEntry() });
                        });
                });
                reader.ReadAtOffset(reader.Read<long>() + 64, () =>
                {
                    for (int i = 0; i < depCount; i++)
                        reader.ReadAtOffset(reader.Read<long>() + 64, () =>
                        {
                            Dependencies.Add(reader.ReadStringTableEntry());
                        });
                });
                Unk0 = reader.Read<bool>();
            });
        }

        public void Write(BINAWriter writer)
        {
            writer.AddOffset($"{Name} {Files.Count} {Dependencies.Count}");
        }

        public void FinishWrite(BINAWriter writer)
        {
            writer.SetOffset($"{Name} {Files.Count} {Dependencies.Count}");
            writer.WriteStringTableEntry(Name);
            writer.Write(Files.Count);
            writer.Write(Dependencies.Count);
            writer.AddOffset($"{Name} files {Files.Count}");
            writer.AddOffset($"{Name} deps {Dependencies.Count}");
            writer.Write(Unk0); // thought this could be `Dependencies.Count != 0` but i guess not, odd
            writer.Write(Files.Count != 0);
            writer.Align(16);
        }

        public void LastWrite(BINAWriter writer)
        {
            writer.Align(8);
            writer.SetOffset($"{Name} deps {Dependencies.Count}");
            foreach(var i in Dependencies)
                writer.AddOffset($"{Name} {i}");
            foreach(var i in Dependencies)
            {
                writer.SetOffset($"{Name} {i}");
                writer.WriteStringTableEntry(i);
                writer.WriteNulls(8);
            }
            writer.SetOffset($"{Name} files {Files.Count}");
            foreach (var i in Files)
                writer.AddOffset($"{Name} {i.Filepath} {i.Root}");
            foreach (var i in Files)
            {
                writer.SetOffset($"{Name} {i.Filepath} {i.Root}");
                writer.WriteStringTableEntry(i.Filepath);
                writer.WriteStringTableEntry(i.Root);
                writer.WriteNulls(8);
            }
        }
    }
}
