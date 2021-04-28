using Manga.IncTrak.Processing;
using Manga.IncTrak.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;

namespace Manga.IncTrak.Manga
{
    public abstract class BasePatterns<T> : MangaSerialize, IPatterns
    {
        private const Int32 BasePatternsVersion1 = 1;

        /// Start Serialized Items 
        private Int32 _index;
        private Int32 _distinctValues = 0;
        /// End Serialized Items 

        private RawDataCache<T> _metric = null;

        protected abstract Int32 DerivedVersion { get; }
        protected abstract void SaveDerived(IMangaSerializationWriter writer);
        protected abstract void LoadDerived(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions);
        protected abstract void ProcessData(int row, T data, bool hasValue, bool isText, IBackgroundJob status);
        protected abstract int TrueDistinctValues { get; }
        public abstract bool CalulateBuckets(bool dimension, int totalRows, int maxDisplayRows, IBackgroundJob status);
        public abstract int MinBucketSize { get; }
        public abstract decimal MinBucketMod { get; }
        public abstract UInt32[] AllowedBuckets { get; }
        public abstract Tuple<int, decimal> ReSpread(string name, int totalRows, Dictionary<string, MyBitArray> rows, Dictionary<string, int> rowCounts, int? newBucketSize, decimal? newBucketMod, IBackgroundJob status);
        public abstract string ColType { get; }
        public abstract object Stat { get; }
        public abstract object DetailedStats { get; }
        public abstract RawDataReader GetReader();

        protected int Positions { get => _metric.Positions; }

        protected BasePatterns(int index)
        {
            _index = index;
        }

        protected BasePatterns(int index, string workFolder)
        {
            _index = index;
            _metric = new RawDataCache<T>(index, workFolder);
        }

        protected override Int32 Version => BasePatternsVersion1;
        protected override string Spec => _index.ToString();

        protected override void Save(IMangaSerializationWriter writer)
        {
            writer.WriteInt32(_index);
            writer.WriteInt32(_distinctValues);

            writer.WriteInt32(DerivedVersion);
            SaveDerived(writer);
        }

        protected override void Load(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _index = reader.ReadInt32();
            _distinctValues = reader.ReadInt32();

            Int32 derivedVersion = reader.ReadInt32();
            LoadDerived(derivedVersion, reader, loadOptions);

            if (loadOptions.ForDisplayOnly == false)
            {
                _metric = new RawDataCache<T>(_index);
                LoadChild(_metric, loadOptions);
            }
        }

        public int DistinctValues 
        { 
            get
            {
                if (_distinctValues == 0)
                {
                    _distinctValues = TrueDistinctValues;
                }

                return _distinctValues;
            }
        }

        public void Process(int row, T data, bool hasValue, bool isText, IBackgroundJob status)
        {
            _metric.Set(row, data, hasValue);

            ProcessData(row, data, hasValue, isText, status);
        }

        public void ProcessWorkingFolder(string mangaPath)
        {
            _metric.ProcessWorkingFolder(mangaPath);
        }

        public void FinalizeCache(bool keep)
        {
            _metric.FinalizeCache(keep);
        }

        protected IEnumerable<object> ReadSequential(DataType dataType)
        {
            foreach (var obj in _metric.ReadSequential(dataType))
                yield return obj;
        }

        public RawDataReader GetReader(DataType dataType)
        {
            return _metric.GetReader(dataType);
        }
    }
}
