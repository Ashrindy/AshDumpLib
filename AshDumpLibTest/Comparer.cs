using AshDumpLib.Helpers.Archives;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AshDumpLibTest;
public class IFileComparer : IEqualityComparer<IFile>
{
    public bool Equals(IFile p1, IFile p2)
    {
        if (p1.FileName == p2.FileName)
            return true;
        return false;
    }

    public int GetHashCode([DisallowNull] IFile obj)
    {
        return obj.FileName.GetHashCode();
    }
}
