//#define NOSAVE_NULLS
using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public class RawDataCache<T> : IMangaSerializeFile
    {
        private int _colIndex;
        private string _indexFilePath;
        private string _dataFilePath;
        private string _nullsFilePath;
        private bool _keep = false;
        private FileStream _indexStream;
        private BinaryWriter _indexWriter;
        private FileStream _dataStream;
        private BinaryWriter _dataWriter;
        private Dictionary<T, long> _positions = new Dictionary<T, long>();
#if NOSAVE_NULLS
        private MyBitArray _hasNoValue = new MyBitArray();
#endif

        public RawDataCache(int colIndex, string workFolder)
        {
            _colIndex = colIndex;
            string workName = Guid.NewGuid().ToString("N");
            _indexFilePath = Path.Combine(workFolder, string.Format("{0}_{1}.idx", workName, colIndex));
            _indexStream = new FileStream(_indexFilePath, FileMode.CreateNew);
            _indexWriter = new BinaryWriter(_indexStream);

            _dataFilePath = Path.Combine(workFolder, string.Format("{0}_{1}.dat", workName, colIndex));
            _dataStream = new FileStream(_dataFilePath, FileMode.CreateNew);
            _dataWriter = new BinaryWriter(_dataStream);
        }

        public RawDataCache(int colIndex)
        {
            _colIndex = colIndex;
        }

        public void Save(string path)
        {
            throw new NotImplementedException();
        }

        public bool Load(string path, MangaLoadOptions loadOptions)
        {
            SetMangaColPath(path);
#if NOSAVE_NULLS
            using (FileStream nullsStream = new FileStream(_nullsFilePath, FileMode.Open))
            {
                using (BinaryReader nullsWriter = new BinaryReader(nullsStream))
                {
                    _hasNoValue.Deserialize(nullsStream);
                }
            }
#endif

            return true;
        }

        public int Positions { get => _positions.Count; }

        private void SetMangaColPath(string mangaPath)
        {
            _indexFilePath = MangaState.ManagaFilePath(mangaPath, MangaFileType.colindex, _colIndex.ToString());
            _dataFilePath = MangaState.ManagaFilePath(mangaPath, MangaFileType.coldata, _colIndex.ToString());
            _nullsFilePath = MangaState.ManagaFilePath(mangaPath, MangaFileType.colnulls, _colIndex.ToString());
        }

        public void FinalizeCache(bool keep)
        {
            MiscHelpers.SafeDispose(_indexWriter);
            MiscHelpers.SafeDispose(_indexStream);

            MiscHelpers.SafeDispose(_dataWriter);
            MiscHelpers.SafeDispose(_dataStream);

            if (keep)
            {
                _keep = true;
            }
            else
            {
                StorageHelpers.SafeFileDelete(_indexFilePath);
                StorageHelpers.SafeFileDelete(_dataFilePath);
            }
        }

        public void ProcessWorkingFolder(string mangaPath)
        {
            if (_keep )
            {
                string workIndexFilePath = _indexFilePath;
                string workDataFilePath = _dataFilePath;

                SetMangaColPath(mangaPath);

                File.Move(workIndexFilePath, _indexFilePath);
                File.Move(workDataFilePath, _dataFilePath );
#if NOSAVE_NULLS
                using (var nullsStream = new FileStream(_nullsFilePath, FileMode.CreateNew))
                {
                    using (var nullsWriter = new BinaryWriter(nullsStream))
                    {
                        _hasNoValue.Serialize(nullsStream);
                    }
                }
#endif
            }
        }

        private void Set(int row, long position)
        {
            _indexStream.Seek(sizeof(long) * row, SeekOrigin.Begin);
            _indexWriter.Write(position);
        }

        public void Set(int row, T result, bool hasValue)
        {
            if (hasValue)
            {
                long position;
                if (_positions.TryGetValue(result, out position))
                {
                    Set(row, position);
                }
                else
                {
                    _positions.Add(result, _dataStream.Position);
                    Set(row, _dataStream.Position);
                    if (result.GetType() == typeof(decimal))
                        _dataWriter.Write((decimal)(object)result);
                    else if (result.GetType() == typeof(long))
                        _dataWriter.Write((long)(object)result);
                    else
                        _dataWriter.Write((string)(object)result);
                }
            }
            else
            {
#if NOSAVE_NULLS
                _hasNoValue.Set(row);
#else
                Set(row, -1);
#endif
            }
        }

        public IEnumerable<object> ReadSequential(DataType rawType)
        {
            try
            {
                using (var reader = new RawDataReader(rawType, _indexFilePath, _dataFilePath))
                {
                    foreach (var obj in reader.ReadSequential())
                    {
                        yield return obj;
                    }
                }
            }
            finally
            {

            }
        }

        public RawDataReader GetReader(DataType rawType)
        {
            return new RawDataReader(rawType, _indexFilePath, _dataFilePath);
        }
    }
}
