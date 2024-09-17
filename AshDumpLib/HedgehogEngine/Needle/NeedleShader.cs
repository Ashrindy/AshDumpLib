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
    public const string FileExtension = ".cso, .pso, .vso";
    public const string Signature = "HHNEEDLE";
    public const string ShaderSignature = "HNSHV002";

    public string ShaderFilePath = "";

    public List<Permutation> Permutations = new();
    public List<Shader> Shaders = new();

    public NeedleShader() { }

    public NeedleShader(string filename) => Open(filename);
    public override void ReadBuffer() => Read(new(new MemoryStream(Data), Amicitia.IO.Streams.StreamOwnership.Retain, Endianness.Big));

    public override void Read(ExtendedBinaryReader reader)
    {
        reader.Signature = reader.ReadString(StringBinaryFormat.FixedLength, 8);
        if (reader.Signature != Signature)
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
        for (int i = 0; i < permutationCount; i++)
        {
            Permutation perm = new();
            perm.Read(reader);
            Permutations.Add(perm);
        }
        reader.Skip(4);
        int variantCount = 1 << permutationCount;
        int[] variants = reader.ReadArray<int>(variantCount);
        for (int i = 0; i < variantCount; i++)
        {
            Shader shader = new();
            shader.Read(reader);
            Shaders.Add(shader);
        }
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

    public class Shader
    {
        public ShaderData MainShaderData = new();
        public ShaderData SubShaderData = new();
        public ShaderData SubVariantShaderData = new();

        public void Read(ExtendedBinaryReader reader)
        {
            int shaderSize = reader.Read<int>();
            MainShaderData.Read(reader, false);
            SubShaderData.Read(reader, false);
            SubVariantShaderData.Read(reader, true);
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            writer.Write(MainShaderData.Data.Length + SubShaderData.Data.Length + SubVariantShaderData.Data.Length);
            MainShaderData.Write(writer, false);
            SubShaderData.Write(writer, false);
            SubVariantShaderData.Write(writer, true);
        }
    }

    public class ShaderData
    {
        public byte[] Data;

        public void Read(ExtendedBinaryReader reader, bool readSize)
        {
            int shaderSize = reader.Read<int>();
            Data = reader.ReadArray<byte>(shaderSize - (readSize ? 4 : 0));
        }

        public void Write(ExtendedBinaryWriter writer, bool writeSize)
        {
            writer.Write(Data.Length + (writeSize ? 4 : 0));
            writer.WriteArray(Data);
        }
    }
}
