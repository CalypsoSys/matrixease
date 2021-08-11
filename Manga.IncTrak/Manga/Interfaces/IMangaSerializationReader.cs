using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public interface IMangaSerializationReader
    {
        bool ReadBool();
        Int32 ReadInt32();
        UInt64 ReadUInt64();
        decimal ReadDecimal();
        double ReadDouble();
        string ReadString();
        Guid ReadGuid();
        T ReadEnum<T>();
        byte[] ReadArrayBytes();
        UInt32[] ReadArrayUInt32s();
        List<string> ReadListString();
        HashSet<string> ReadHashString();
        Dictionary<string, Int32> ReadDictStringInt32();
        Dictionary<Int32, Int32> ReadDictInt32Int32();
        Dictionary<string, double> ReadDictStringDouble();
        Dictionary<string, string> ReadDictStringString();
        void ReadCustom(IMangaSerializeCustom serialize);
        T LoadChild<T>(IMangaSerializeInline child, MangaLoadOptions loadOptions) where T : class;
    }
}
