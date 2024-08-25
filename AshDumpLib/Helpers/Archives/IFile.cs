using Amicitia.IO.Binary;

namespace AshDumpLib.Helpers.Archives;

public class IFile
{
    public string FilePath;
    public string FileName;
    public string Extension;
    public byte[] Data;
    public const Endianness endianness = Endianness.Little;

    public IFile() { }

    public IFile(string filePath)
    {
        FileName = Path.GetFileName(filePath);
        FilePath = filePath;
        Extension = FileName.Substring(FileName.IndexOf('.') + 1);
        Data = File.ReadAllBytes(FilePath);
    }

    public IFile(string filePath, byte[] data)
    {
        FileName = Path.GetFileName(filePath);
        FilePath = filePath;
        Extension = FileName.Substring(FileName.IndexOf('.') + 1);
        Data = data;
    }

    public virtual void Open(string filename)
    {
        FileName = Path.GetFileName(filename);
        FilePath = filename;
        Extension = FileName.Substring(FileName.IndexOf('.') + 1);
        Data = File.ReadAllBytes(FilePath);
        ReadBuffer();
    }

    public virtual void Open(string filename, byte[] data)
    {
        FileName = Path.GetFileName(filename);
        FilePath = filename;
        Extension = FileName.Substring(FileName.IndexOf('.') + 1);
        Data = data;
        ReadBuffer();
    }

    public virtual void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness));
    public virtual void Read(ExtendedBinaryReader reader) => throw new NotImplementedException();

    public virtual void Save() => WriteBuffer();
    public void SaveToFile(string filename) { WriteBuffer(); File.WriteAllBytes(filename, Data); }

    public virtual void Write(ExtendedBinaryWriter writer) => throw new NotImplementedException();
    public virtual void FinishWrite(ExtendedBinaryWriter writer) { }
    public virtual void WriteBuffer() { ExtendedBinaryWriter writer = new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, endianness);  Write(writer); FinishWrite(writer); }
}
