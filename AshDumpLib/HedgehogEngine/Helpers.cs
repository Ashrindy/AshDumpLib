using Amicitia.IO.Binary;

namespace AshDumpLib.HedgehogEngine;

public static class Helpers
{
    public static string ReadStringTableEntry(BinaryObjectReader reader, int stringTableOffset)
    {
        long pointer = reader.Read<long>();
        long prePos = reader.Position;
        reader.Seek(pointer + stringTableOffset, SeekOrigin.Begin);
        string value = reader.ReadString(StringBinaryFormat.NullTerminated);
        reader.Seek(prePos, SeekOrigin.Begin);
        return value;
    }
}
