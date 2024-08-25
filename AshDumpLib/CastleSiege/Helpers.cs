﻿using Amicitia.IO.Binary;

namespace AshDumpLib.CastleSiege;

public static class Helpers
{
    public static string CastleStrikeString(ExtendedBinaryReader reader)
    {
        long prePos = reader.Position;
        string value = reader.ReadString(StringBinaryFormat.NullTerminated);
        reader.Seek(prePos, SeekOrigin.Begin);
        reader.Skip(256);
        return value;
    }
}