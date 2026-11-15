using System;

namespace SkinRecolor;

public class TypeToPath(Type type, string path, bool isChild)
{
    public Type type = type;
    public string path = path;
    public bool isChild = isChild;
    
    public bool Equals(TypeToPath other)
    {
        return type == other.type &&
               path == other.path &&
               isChild == other.isChild;
    }
}