using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public abstract class MangaSerialize : IMangaSerializeFile, IMangaSerializationWriter, IMangaSerializationReader
    {
        private const int GuidBytes = 16;

        private string _path;
        private BinaryWriter _dataWriter;
        private BinaryReader _dataReader;
        protected abstract Int32 Version { get; }
        protected abstract string Spec { get; }
        protected abstract MangaFileType FileType { get; }
        protected abstract void Save(IMangaSerializationWriter writer);
        protected abstract void Load(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions);

        public void Delete(string path)
        {
            string filePath = MangaState.ManagaFilePath(path, FileType, Spec);
            StorageHelpers.SafeFileDelete(filePath);
        }

        public void Save(string path)
        {
            _path = path;
            string filePath = MangaState.ManagaFilePath(path, FileType, Spec);
            using (new FileLocker(filePath))
            {
                using (FileStream dataStream = new FileStream(filePath, FileMode.OpenOrCreate))
                {
                    using (_dataWriter = new BinaryWriter(dataStream))
                    {
                        _dataWriter.Write(Version);
                        Save(this);
                    }
                }
            }
        }

        protected void SaveChild(IMangaSerializeFile child)
        {
            child.Save(_path);
        }
        
        void IMangaSerializationWriter.SaveChild(IMangaSerializeInline child)
        {
            _dataWriter.Write(child.Version);
            child.Save(this);
        }

        public bool Load(string path, MangaLoadOptions loadOptions)
        {
            _path = path;
            string filePath = MangaState.ManagaFilePath(path, FileType, Spec);
            if (File.Exists(filePath))
            {
                using (new FileLocker(filePath))
                {
                    using (FileStream dataStream = new FileStream(filePath, FileMode.Open))
                    {
                        using (_dataReader = new BinaryReader(dataStream))
                        {
                            Int32 vesion = _dataReader.ReadInt32();
                            Load(vesion, this, loadOptions);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        protected void LoadChild(IMangaSerializeFile child, MangaLoadOptions loadOptions)
        {
            child.Load(_path, loadOptions);
        }

        T IMangaSerializationReader.LoadChild<T>(IMangaSerializeInline child, MangaLoadOptions loadOptions) where T : class
        {
            Int32 vesion = _dataReader.ReadInt32();
            child.Load(vesion, this, loadOptions);
            return child as T;
        }

        void IMangaSerializationWriter.WriteBool(bool data)
        {
            _dataWriter.Write(data);
        }

        bool IMangaSerializationReader.ReadBool()
        {
            return _dataReader.ReadBoolean();
        }

        void IMangaSerializationWriter.WriteInt32(Int32 data)
        {
            _dataWriter.Write(data);
        }

        Int32 IMangaSerializationReader.ReadInt32()
        {
            return _dataReader.ReadInt32();
        }

        void IMangaSerializationWriter.WriteUInt64(UInt64 data)
        {
            _dataWriter.Write(data);
        }

        UInt64 IMangaSerializationReader.ReadUInt64()
        {
            return _dataReader.ReadUInt64();
        }

        void IMangaSerializationWriter.WriteDecimal(decimal data)
        {
            _dataWriter.Write(data);
        }

        decimal IMangaSerializationReader.ReadDecimal()
        {
            return _dataReader.ReadDecimal();
        }

        void IMangaSerializationWriter.WriteDouble(double data)
        {
            _dataWriter.Write(data);
        }

        double IMangaSerializationReader.ReadDouble()
        {
            return _dataReader.ReadDouble();
        }

        void IMangaSerializationWriter.WriteString(string data)
        {
            _dataWriter.Write(data);
        }

        string IMangaSerializationReader.ReadString()
        {
            return _dataReader.ReadString();
        }

        void IMangaSerializationWriter.WriteGuid(Guid data)
        {
            _dataWriter.Write(data.ToByteArray());
        }

        Guid IMangaSerializationReader.ReadGuid()
        {
            return new Guid(_dataReader.ReadBytes(GuidBytes));
        }

        void IMangaSerializationWriter.WriteEnum<T>(T data)
        {
            _dataWriter.Write(Convert.ToInt16(data));
        }

        T IMangaSerializationReader.ReadEnum<T>()
        {
            short data = _dataReader.ReadInt16();
            return (T)Enum.Parse(typeof(T), data.ToString());
        }

        void IMangaSerializationWriter.WriteArrayBytes(byte[] data)
        {
            if (data != null && data.Count() != 0)
            {
                Int32 count = data.Count();
                _dataWriter.Write(count);
                _dataWriter.Write(data);
            }
            else
            {
                Int32 count = 0;
                _dataWriter.Write(count);
            }
        }

        byte[] IMangaSerializationReader.ReadArrayBytes()
        {
            Int32 count = _dataReader.ReadInt32();
            if (count > 0)
            {
                return _dataReader.ReadBytes(count);
            }
            else
            {
                return new byte[0];
            }
        }

        void IMangaSerializationWriter.WriteArrayUInt32s(UInt32[] data)
        {
            if (data != null)
            {
                Int32 count = data.Count();
                _dataWriter.Write(count);
                for (int i = 0; i < count; i++)
                    _dataWriter.Write(data[i]);
            }
            else
            {
                Int32 count = 0;
                _dataWriter.Write(count);
            }
        }

        UInt32[] IMangaSerializationReader.ReadArrayUInt32s()
        {
            Int32 count = _dataReader.ReadInt32();
            UInt32[] data = new UInt32[count];
            for (int i = 0; i < count; i++)
                data[i] = _dataReader.ReadUInt32();
            return data;
        }

        void IMangaSerializationWriter.WriteListString(List<string> data)
        {
            if (data != null)
            {
                Int32 count = data.Count();
                _dataWriter.Write(count);
                foreach (var str in data)
                {
                    _dataWriter.Write(str);
                }
            }
            else
            {
                Int32 count = 0;
                _dataWriter.Write(count);
            }
        }

        void IMangaSerializationWriter.WriteHashString(HashSet<string> data)
        {
            if (data != null)
            {
                Int32 count = data.Count();
                _dataWriter.Write(count);
                foreach (var str in data)
                {
                    _dataWriter.Write(str);
                }
            }
            else
            {
                Int32 count = 0;
                _dataWriter.Write(count);
            }
        }

        List<string> IMangaSerializationReader.ReadListString()
        {
            Int32 count = _dataReader.ReadInt32();
            List<string> data = new List<string>(count);
            for (int i = 0; i < count; i++)
            {
                string key = _dataReader.ReadString();
                data.Add(key);
            }

            return data;
        }

        HashSet<string> IMangaSerializationReader.ReadHashString()
        {
            Int32 count = _dataReader.ReadInt32();
            HashSet<string> data = new HashSet<string>(count);
            for (int i = 0; i < count; i++)
            {
                string key = _dataReader.ReadString();
                data.Add(key);
            }

            return data;
        }

        void IMangaSerializationWriter.WriteDictStringInt32(Dictionary<string, Int32> data)
        {
            if (data != null)
            {
                Int32 count = data.Count();
                _dataWriter.Write(count);
                foreach (var pair in data)
                {
                    _dataWriter.Write(pair.Key);
                    _dataWriter.Write(pair.Value);
                }
            }
            else
            {
                Int32 count = 0;
                _dataWriter.Write(count);
            }
        }

        Dictionary<string, Int32> IMangaSerializationReader.ReadDictStringInt32()
        {
            Int32 rowCounts = _dataReader.ReadInt32();
            Dictionary<string, int> dict = new Dictionary<string, int>(rowCounts);
            for (int i = 0; i < rowCounts; i++)
            {
                string key = _dataReader.ReadString();
                Int32 data = _dataReader.ReadInt32();
                dict.Add(key, data);
            }

            return dict;
        }

        void IMangaSerializationWriter.WriteDictInt32Int32(Dictionary<Int32, Int32> data)
        {
            if (data != null)
            {
                Int32 count = data.Count();
                _dataWriter.Write(count);
                foreach (var pair in data)
                {
                    _dataWriter.Write(pair.Key);
                    _dataWriter.Write(pair.Value);
                }
            }
            else
            {
                Int32 count = 0;
                _dataWriter.Write(count);
            }
        }

        Dictionary<Int32, Int32> IMangaSerializationReader.ReadDictInt32Int32()
        {
            Int32 rowCounts = _dataReader.ReadInt32();
            Dictionary<Int32, int> dict = new Dictionary<Int32, int>(rowCounts);
            for (int i = 0; i < rowCounts; i++)
            {
                Int32 key = _dataReader.ReadInt32();
                Int32 data = _dataReader.ReadInt32();
                dict.Add(key, data);
            }

            return dict;
        }

        void IMangaSerializationWriter.WriteDictStringString(Dictionary<string, string> data)
        {
            if (data != null)
            {
                Int32 count = data.Count();
                _dataWriter.Write(count);
                foreach (var pair in data)
                {
                    _dataWriter.Write(pair.Key);
                    _dataWriter.Write(pair.Value);
                }
            }
            else
            {
                Int32 count = 0;
                _dataWriter.Write(count);
            }
        }

        Dictionary<string, string> IMangaSerializationReader.ReadDictStringString()
        {
            Int32 rowCounts = _dataReader.ReadInt32();
            Dictionary<string, string> dict = new Dictionary<string, string>(rowCounts);
            for (int i = 0; i < rowCounts; i++)
            {
                string key = _dataReader.ReadString();
                string data = _dataReader.ReadString();
                dict.Add(key, data);
            }

            return dict;
        }

        void IMangaSerializationWriter.WriteDictStringDouble(Dictionary<string, double> data)
        {
            if (data != null)
            {
                Int32 count = data.Count();
                _dataWriter.Write(count);
                foreach (var pair in data)
                {
                    _dataWriter.Write(pair.Key);
                    _dataWriter.Write(pair.Value);
                }
            }
            else
            {
                Int32 count = 0;
                _dataWriter.Write(count);
            }
        }

        Dictionary<string, double> IMangaSerializationReader.ReadDictStringDouble()
        {
            Int32 rowCounts = _dataReader.ReadInt32();
            Dictionary<string, double> dict = new Dictionary<string, double>(rowCounts);
            for (int i = 0; i < rowCounts; i++)
            {
                string key = _dataReader.ReadString();
                double data = _dataReader.ReadDouble();
                dict.Add(key, data);
            }

            return dict;
        }

        void IMangaSerializationWriter.WriteCustom(IMangaSerializeCustom serialize)
        {
            serialize.Serialize(_dataWriter.BaseStream);
        }

        void IMangaSerializationReader.ReadCustom(IMangaSerializeCustom serialize)
        {
            serialize.Deserialize(_dataReader.BaseStream);
        }
    }
}
