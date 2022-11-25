using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MatrixEase.Manga.Manga
{
    public class RawDataReader : MyDisposable
    {
        private DataType _rawType;
        private FileStream _indexStream;
        private BinaryReader _indexReader;
        private FileStream _dataStream;
        private BinaryReader _dataReader;

        public RawDataReader(DataType rawType, string indexFilePath, string dataFilePath)
        {
            _rawType = rawType;
            _indexStream = new FileStream(indexFilePath, FileMode.Open);
            _indexReader = new BinaryReader(_indexStream);
            _dataStream = new FileStream(dataFilePath, FileMode.Open);
            _dataReader = new BinaryReader(_dataStream);
        }

        protected override void DisposeManaged()
        {
            MiscHelpers.SafeDispose(_dataReader);
            MiscHelpers.SafeDispose(_dataStream);
            MiscHelpers.SafeDispose(_indexReader);
            MiscHelpers.SafeDispose(_indexStream);
        }

        protected override void DisposeUnManaged()
        {
        }

        private object ReadData()
        {
            if (_rawType == DataType.Numeric)
                return _dataReader.ReadDecimal();
            else if (_rawType == DataType.Date)
                return new DateTime(_dataReader.ReadInt64());
            else if (_rawType == DataType.Text)
                return _dataReader.ReadString();

            return null;
        }

        public IEnumerable<object> ReadSequential()
        {
            try
            {
#if NOSAVE_NULLS
                _hasNoValue.SetPositions();
#endif
#if NOSAVE_NULLS
                for (int row = 0; indexStream.Position != indexStream.Length; row++)
                {
                    if (_hasNoValue.On(row) == false)
                    if (_hasNoValue.On(row) == false)
                    {
                        indexStream.Seek(sizeof(long) * row, SeekOrigin.Begin);
#else
                while (_indexStream.Position != _indexStream.Length)
                {
#endif
                    object obj = null;
                    long position = _indexReader.ReadInt64();
                    if (position != -1)
                    {
                        _dataStream.Seek(position, SeekOrigin.Begin);
                        obj = ReadData();
                    }
                    yield return obj;
                }
            }
            finally
            {
#if NOSAVE_NULLS
                _hasNoValue.ClearPositions();
#endif
            }
        }

        public object ReadRow(int row, bool forCSV)
        {
            object obj = null;
            _indexStream.Seek(sizeof(long) * row, SeekOrigin.Begin);
            try
            {
                long position = _indexReader.ReadInt64();
                if (position != -1)
                {
                    _dataStream.Seek(position, SeekOrigin.Begin);
                    obj = ReadData();
                    if (forCSV)
                    {
                        if (obj == null)
                            return string.Empty;
                        else if (_rawType == DataType.Numeric)
                            return obj.ToString();
                        else if (_rawType == DataType.Date)
                            return ((DateTime)obj).ToString("yyyy-MM-dd HH:mm:ss");
                        else if (_rawType == DataType.Text)
                            return string.Format("\"{0}\"", obj.ToString().Replace("\"", "\"\""));
                    }
                }
            }
            catch(Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase read fow {0} {1}", row, _rawType);
            }
            return obj;
        }
    }
}