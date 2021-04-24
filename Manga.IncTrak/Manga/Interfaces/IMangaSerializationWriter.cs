using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public interface IMangaSerializationWriter
    {
        void WriteBool(bool data);
        void WriteInt32(Int32 data);
        void WriteUInt64(UInt64 data);
        void WriteDecimal(decimal data);
        void WriteDouble(double data);
        void WriteString(string data);
        void WriteGuid(Guid data);
        void WriteEnum<T>(T data);
        void WriteArrayBytes(byte[] data);
        void WriteArrayUInt32s(UInt32[] data);
        void WriteListString(List<string> data);
        void WriteDictStringInt32(Dictionary<string, Int32> data);
        void WriteDictInt32Int32(Dictionary<Int32, Int32> data);
        void WriteDictStringDouble(Dictionary<string, double> data);
        void WriteDictStringString(Dictionary<string, string> data);
        void WriteCustom(IMangaSerializeCustom serialize);
        void SaveChild(IMangaSerializeInline child);
    }
}
