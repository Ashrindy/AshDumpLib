using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AshDumpLib.HedgehogEngine.Needle;

public class NeedleArchive : IFile
{
    public const string FileExtension = ".model";
    public const string Signature = "NEDARCV1";
    public const string ArchiveMagic = "arc";
    public const string LodInfoSignature = "NEDLDIV1";
    public const string LodInfoMagic = "lodinfo";
    public const string ModelSignature = "NEDMDLV5";
    public const string ModelMagic = "model";

    public byte[] LodInfo;
    public List<byte[]> Models;

    public NeedleArchive() { }

    public NeedleArchive(string filename) => Open(filename);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.Signature = reader.ReadString(StringBinaryFormat.FixedLength, 8);
        if (reader.Signature != Signature)
            throw new Exception("Wrong signature!");
        int filesize = reader.Read<int>();
        string archiveMagic = reader.ReadString(StringBinaryFormat.FixedLength, 4);
        string lodInfoSignature = reader.ReadString(StringBinaryFormat.FixedLength, 8);
        if (LodInfoSignature != lodInfoSignature)
            throw new Exception("Wrong signature!");
        string lodinfoMagic = reader.ReadString(StringBinaryFormat.FixedLength, 8);
        int lodInfoFileSize = reader.Read<int>();
        reader.Skip(1);
        int lodCount = reader.Read<byte>();
        reader.Skip(-2);
        LodInfo = reader.ReadArray<byte>(lodInfoFileSize);
        for(int i = 0; i < lodCount; i++)
        {
            string modelSignature = reader.ReadString(StringBinaryFormat.FixedLength, 8);
            if (ModelSignature != modelSignature)
                throw new Exception("Wrong signature!");
            string modelMagic = reader.ReadString(StringBinaryFormat.FixedLength, 8);
            int modelFileSize = reader.Read<int>();
            byte[] model = reader.ReadArray<byte>(modelFileSize);
            Models.Add(model);
        }
        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteString(StringBinaryFormat.FixedLength, Signature, 8);
        writer.AddOffset("filesize");
        writer.WriteString(StringBinaryFormat.FixedLength, ArchiveMagic, 4);
        writer.WriteString(StringBinaryFormat.FixedLength, LodInfoMagic, 8);
        writer.WriteString(StringBinaryFormat.FixedLength, LodInfoMagic, 8);
        writer.Write(LodInfo.Length);
        writer.WriteArray(LodInfo);
        foreach(var i in Models)
        {
            writer.WriteString(StringBinaryFormat.FixedLength, ModelSignature, 8);
            writer.WriteString(StringBinaryFormat.FixedLength, ModelMagic, 8);
            writer.Write(i.Length);
            writer.WriteArray(i);
        }
        writer.SetOffset("filesize");
        writer.Dispose();
    }
}
