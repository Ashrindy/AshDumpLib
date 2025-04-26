using AshDumpLib.Helpers.Archives;
using Amicitia.IO.Binary;
using static AshDumpLib.HedgehogEngine.Archives.PAC;

namespace AshDumpLib.HedgehogEngine.Archives;

public class PACV3
{
    public List<IFile> files = new();
    public List<string> Dependencies = new();
    public bool IsSplit = false;

    public static readonly PACVersion Version = new() { MajorVersion = (byte)'3', MinorVersion = (byte)'0', RevisionVersion = (byte)'1' };

    struct Header
    {
        public int treeSize;
        public int depTableSize;
        public int dataEntriesSize;
        public int strTableSize;
        public int fileDataSize;
        public int offTableSize;
        public short type;
        public short flags;
        public int depCount;
    }

    static void LoopThroughNodesForName(Tree<Node<FileNode>> tree, Node<FileNode> curNode, ref string name)
    {
        if (curNode.bufStartIndex != 0)
        {
            name = tree.nodes[curNode.parentidx].name + name;
            LoopThroughNodesForName(tree, tree.nodes[curNode.parentidx], ref name);
        }
    }

    public void Read(ExtendedBinaryReader reader)
    {
        var header = reader.Read<Header>();
        Tree<Node<Tree<Node<FileNode>>>> tree = new();
        tree.Read(reader);
        List<PAC> depends = new();
        if (header.depTableSize > 0)
        {
            reader.Seek(header.treeSize + 48, SeekOrigin.Begin);
            int depCount = reader.Read<int>();
            reader.Align(8);
            reader.ReadAtOffset(reader.Read<long>(), () =>
            {
                for (int i = 0; i < depCount; i++)
                {
                    var filename = reader.ReadStringTableEntry64();
                    depends.Add(new(Path.Combine(Path.GetDirectoryName(reader.CurFilePath), filename)));
                    Dependencies.Add(filename);
                }
            });
        }
        foreach(var i in tree.indices)
        {
            var x = tree.nodes[i].data;
            foreach(var y in x.indices)
            {
                var l = x.nodes[y];
                var filename = x.nodes[l.parentidx].name;
                if (x.nodes[l.parentidx].bufStartIndex != 0)
                    LoopThroughNodesForName(x, x.nodes[l.parentidx], ref filename);
                files.Add(new($"{filename}.{l.data.Extension}", l.data.Data));
            }
        }
        foreach (var i in depends)
            files.AddRange(i.Files);
    }

    public void Write(ExtendedBinaryWriter writer)
    {
        var header = new Header();
        if (IsSplit) header.type = 2;
        else header.type = 5;
        header.flags = 264;
        writer.Write(header);

        Tree<Node<Tree<Node<FileNode>>>> tree = BuildRadixTree(files); 

        header.treeSize = (int)writer.Position;
        tree.Write(writer);
        writer.SetOffset($"nodes{tree.GetHashCode()}");
        foreach (var i in tree.nodes)
            i.Write(writer);
        foreach (var i in tree.indices)
        {
            var x = tree.nodes[i];
            writer.SetOffset($"data{x.GetHashCode()}");
            x.data.Write(writer);
            writer.SetOffset($"nodes{x.data.GetHashCode()}");
            foreach(var r in x.data.nodes)
                r.Write(writer);
        }
        writer.SetOffset($"indices{tree.GetHashCode()}");
        foreach (var i in tree.indices)
            writer.Write(i);
        writer.Align(8);
        foreach (var i in tree.indices)
        {
            var x = tree.nodes[i];
            writer.SetOffset($"indices{x.data.GetHashCode()}");
            foreach (var r in x.data.indices)
                writer.Write(r);
            writer.Align(8);
        }
        foreach (var i in tree.nodes)
        {
            if(i.childIndices.Count > 0)
            {
                writer.SetOffset($"childIndices{i.GetHashCode()}");
                foreach (var x in i.childIndices)
                    writer.Write(x);
                writer.Align(8);
            }  
        }
        foreach (var i in tree.indices)
        {
            var x = tree.nodes[i];
            foreach (var r in x.data.nodes)
            {
                if(r.childIndices.Count > 0)
                {
                    writer.SetOffset($"childIndices{r.GetHashCode()}");
                    foreach (var y in r.childIndices)
                        writer.Write(y);
                    writer.Align(8);
                } 
            }
        }
        header.treeSize = (int)writer.Position - header.treeSize;

        header.depTableSize = (int)writer.Position;
        if (Dependencies.Count > 0)
        {
            header.depCount = Dependencies.Count;
            writer.Write(Dependencies.Count);
            writer.Align(8);
            writer.AddOffset("dependencies");
            writer.SetOffset("dependencies");
            foreach (var i in Dependencies)
                writer.WriteStringTableEntry(i);
        }
        header.depTableSize = (int)writer.Position - header.depTableSize;

        header.dataEntriesSize = (int)writer.Position;
        foreach (var i in tree.indices)
        {
            var x = tree.nodes[i];
            foreach (var r in x.data.indices)
            {
                var l = x.data.nodes[r];
                writer.SetOffset($"data{l.GetHashCode()}");
                l.data.Write(writer);
            }
        }
        header.dataEntriesSize = (int)writer.Position - header.dataEntriesSize;

        int stringTableOffset = (int)writer.Position;

        foreach (var i in writer.StringTableOffsets)
        {
            writer.Seek(i.Key, SeekOrigin.Begin);
            writer.Write(i.Value + stringTableOffset);
        }
        writer.Seek(stringTableOffset, SeekOrigin.Begin);

        header.strTableSize = (int)writer.Position;
        writer.WriteString(StringBinaryFormat.FixedLength, writer.StringTable, writer.StringTable.Length);
        writer.Align(8);
        header.strTableSize = (int)writer.Position - header.strTableSize;

        header.fileDataSize = (int)writer.Position;
        foreach (var i in tree.indices)
        {
            var x = tree.nodes[i];
            foreach (var r in x.data.indices)
            {
                var l = x.data.nodes[r].data;
                if((l.flags & 1) == 0)
                {
                    writer.Align(16);
                    writer.SetOffset($"data{l.GetHashCode()}");
                    writer.WriteArray(l.Data);
                }
            }
        }
        header.fileDataSize = (int)writer.Position - header.fileDataSize;

        header.offTableSize = (int)writer.Position;
        long lastOffsetPos = 0;
        foreach (var i in writer.Offsets)
        {
            int difference = (int)(i.Value - lastOffsetPos) >> 2;
            if (difference <= 0x3F)
            {
                int x = difference & 0x3F;
                writer.Write((byte)((byte)64 | x));
            }
            else if (difference <= 0x3FFF)
            {
                int x = difference & 0x3FFF;
                writer.Write((byte)((byte)128 | (x >> 8)));
                writer.Write((byte)(x & 0xFF));
            }
            else if (difference <= 0x3FFFFFFF)
            {
                int x = difference & 0x3FFFFFFF;
                writer.Write((byte)((byte)192 | (x >> 24)));
                writer.Write((byte)((x >> 16) & 0xFF));
                writer.Write((byte)((x >> 8) & 0xFF));
                writer.Write((byte)(x & 0xFF));
            }
            lastOffsetPos = i.Value;
        }
        writer.Align(8);
        header.offTableSize = (int)writer.Position - header.offTableSize;

        writer.Seek(0x10, SeekOrigin.Begin);
        writer.Write(header);
    }

    Tree<Node<Tree<Node<FileNode>>>> BuildRadixTree(List<IFile> files)
    {
        var tree = new Tree<Node<Tree<Node<FileNode>>>>();
        tree.nodes.Add(new());
        Dictionary<ResType, List<IFile>> filesSorted = new();
        foreach(var i in files)
        {
            var resType = GetResTypeByExt(Version, i.Extension);
            if (filesSorted.ContainsKey(resType))
                filesSorted[resType].Add(i);
            else
                filesSorted.Add(resType, new() { i });
        }

        return tree;
    }

    public class Tree<T> : IExtendedBinarySerializable where T : IExtendedBinarySerializable, new()
    {
        public List<T> nodes = new();
        public List<int> indices = new();

        public Tree() { }

        public void Read(ExtendedBinaryReader reader)
        {
            int nodeCount = reader.Read<int>();
            int indicesCount = reader.Read<int>();
            long nodePtr = reader.Read<long>();
            long indicesPtr = reader.Read<long>();
            reader.ReadAtOffset(nodePtr, () =>
            {
                for(int i = 0; i < nodeCount; i++)
                {
                    T node = new();
                    node.Read(reader);
                    nodes.Add(node);
                }
            });
            reader.ReadAtOffset(indicesPtr, () =>
            {
                for (int i = 0; i < indicesCount; i++)
                    indices.Add(reader.Read<int>());
            });
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            writer.Write(nodes.Count);
            writer.Write(indices.Count);
            writer.AddOffset($"nodes{GetHashCode()}");
            writer.AddOffset($"indices{GetHashCode()}");
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {

        }
    }

    public class Node<T> : IExtendedBinarySerializable where T : IExtendedBinarySerializable, new()
    {
        public string name = "";
        public int parentidx = -1;
        public int globalidx = 0;
        public int dataidx = -1;
        public List<int> childIndices = new();
        public T? data;
        public byte bufStartIndex = 0;

        public Node() { }

        public void Read(ExtendedBinaryReader reader)
        {
            name = reader.ReadStringTableEntry64();
            long dataPtr = reader.Read<long>();
            long childIndicesPtr = reader.Read<long>();
            parentidx = reader.Read<int>();
            globalidx = reader.Read<int>();
            dataidx = reader.Read<int>();
            short childCount = reader.Read<short>();
            bool hasData = reader.Read<bool>();
            bufStartIndex = reader.Read<byte>();
            if (hasData)
                reader.ReadAtOffset(dataPtr, () =>
                {
                    data = new();
                    data.Read(reader);
                });
            reader.ReadAtOffset(childIndicesPtr, () =>
            {
                for (int i = 0; i < childCount; i++)
                    childIndices.Add(reader.Read<int>());
            });
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            if(name != "")
                writer.WriteStringTableEntry(name);
            else
                writer.WriteNulls(8);
            if (data != null)
                writer.AddOffset($"data{GetHashCode()}");
            else
                writer.WriteNulls(8);
            if (childIndices.Count > 0)
                writer.AddOffset($"childIndices{GetHashCode()}");
            else
                writer.WriteNulls(8);
            writer.Write(parentidx);
            writer.Write(globalidx);
            writer.Write(dataidx);
            writer.Write((short)childIndices.Count);
            writer.Write(data != null);
            writer.Write(bufStartIndex);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {

        }
    }

    public class FileNode : IExtendedBinarySerializable
    {
        public int ID = 0;
        public byte[] Data = new byte[0];
        public string Extension = "";
        public long flags = 0;

        public FileNode() { }

        public FileNode(IFile file)
        {
            Data = file.Data;
            Extension = file.Extension;
        }

        public void Read(ExtendedBinaryReader reader)
        {
            ID = reader.Read<int>();
            int dataSize = reader.Read<int>();
            Data = new byte[dataSize];
            reader.Skip(8);
            long dataPtr = reader.Read<long>();
            reader.Skip(8);
            Extension = reader.ReadStringTableEntry64();
            flags = reader.Read<long>();
            if(dataPtr != 0)
                reader.ReadAtOffset(dataPtr, () => Data = reader.ReadArray<byte>(dataSize));
        }

        public void Write(ExtendedBinaryWriter writer)
        {
            writer.Write(ID);
            writer.Write(Data.Count());
            writer.WriteNulls(8);
            if((flags & 1) == 0)
                writer.AddOffset($"data{GetHashCode()}");
            else
                writer.WriteNulls(8);
            writer.WriteNulls(8);
            if (Extension != "")
                writer.WriteStringTableEntry(Extension);
            else
                writer.WriteNulls(8);
            writer.Write(flags);
        }

        public void FinishWrite(ExtendedBinaryWriter writer)
        {

        }
    }
}
