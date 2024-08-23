namespace AshDumpLib.Helpers.Archives;

public class Archive : IFile
{
    public List<IFile> Files = new();
    public bool parseFiles = true;

    public void Open(string filename, bool parse)
    {
        parseFiles = parse;
        Open(filename);
    }

    public void Open(string filename, byte[] data, bool parse)
    {
        parseFiles = parse;
        Open(filename, data);
    }

    public void AddFile(IFile file) => Files.Add(file);
    public void AddFile(string filename) => Files.Add(new(filename));
    public void AddFile(string filename, byte[] data) => Files.Add(new(filename, data));

    public void RemoveFile(IFile file) => Files.Remove(file);
    public void RemoveFile(string filename) => Files.Remove(Files.Find(x => x.FileName == filename));
}
