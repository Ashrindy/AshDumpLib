using Amicitia.IO.Binary;
using AshDumpLib.Helpers.Archives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AshDumpLib.HedgehogEngine.Needle;

public class NeedleShader : IFile
{
    public const string FileExtension = ".cso, .pso";
    public const string Signature = "HHNEEDLE";
    public const string ShaderSignature = "HHSHV002";

    public string ShaderFilePath = "";

    public List<Permutation> Permutations = new();
    public List<SubShader> Shaders;

    public NeedleShader() { }

    public NeedleShader(string filename) => Open(filename);

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.Signature = reader.ReadString(StringBinaryFormat.FixedLength, 8);
        if(reader.Signature != Signature)
            throw new Exception("Wrong signature!");
        int filesize = reader.Read<int>();
        ShaderFilePath = reader.ReadString(StringBinaryFormat.NullTerminated);
        reader.Align(4);
        string shaderSignature = reader.ReadString(StringBinaryFormat.FixedLength, 8);
        if (shaderSignature != ShaderSignature)
            throw new Exception("Wrong signature!");
        int dataSize = reader.Read<int>();
        reader.Skip(4);
        int permutationCount = reader.Read<int>();
        for(int i = 0; i < permutationCount; i++)
        {
            Permutation perm = new();
            perm.Read(reader);
            Permutations.Add(perm);
        }
        reader.Skip(4);
        int variantCount = reader.Read<int>();
        int[] variants = reader.ReadArray<int>(variantCount);
        int mainShaderFullSize = reader.Read<int>();
        SubShader shader = new();
        shader.Read(reader);
        //MainShader = shader.Data;
        reader.Dispose();
    }

    public override void Write(ExtendedBinaryWriter writer)
    {
        writer.WriteString(StringBinaryFormat.FixedLength, Signature, 8);

        writer.Dispose();
    }

    public class Permutation
    {
        public string Name = "";
        public int unk = 0;

        public void Read(ExtendedBinaryReader reader)
        {
            unk = reader.Read<int>();
            Name = reader.ReadString(StringBinaryFormat.NullTerminated);
            reader.Align(4);
        }
    }

    public class SubShader
    {
        public byte[] MainData;
        public byte[] SubData;

        public void Read(ExtendedBinaryReader reader)
        {
            int shaderSize = reader.Read<int>();
            MainData = reader.ReadArray<byte>(shaderSize);
        }
    }
}
