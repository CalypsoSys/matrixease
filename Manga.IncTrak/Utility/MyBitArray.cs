//#define MS_BITMAP
//#define WHA_BITMAP
#define EWAH_BITMAP
//#define ROARING_BITMAP
using Collections.Special;
using System;
using System.IO;
using MatrixEase.Manga.Manga;
#if MS_BITMAP
using System.Collections;
#elif WHA_BITMAP
using RaptorDB;
#elif EWAH_BITMAP
using Ewah;
using System.Collections.Generic;
#endif

namespace MatrixEase.Manga.Utility
{
#if EWAH_BITMAP || ROARING_BITMAP
    public class MyBitArray : IMangaSerializeCustom
#elif MS_BITMAP || WHA_BITMAP
    public class MyBitArray : IMangaSerializeInline
#endif
    {
        private const Int32 MyBitArrayVersion1 = 1;
#if MS_BITMAP
        private BitArray _bits;
#elif WHA_BITMAP
        private WAHBitArray _bits;
#elif EWAH_BITMAP
        private EwahCompressedBitArray _bits;
        private EwahCompressedBitArraySerializer _customSerializer;
        private List<int> _positions = null;
#elif ROARING_BITMAP
        private RoaringBitmap _bits;
#endif

        public int Version => MyBitArrayVersion1;

        public MyBitArray()
        {
#if EWAH_BITMAP
            _bits = new EwahCompressedBitArray();
            _customSerializer = new EwahCompressedBitArraySerializer();
#elif ROARING_BITMAP
            _bits = RoaringBitmap.Create();
#endif
        }

        public MyBitArray(int length)
        {
#if MS_BITMAP
            _bits = new BitArray(length);
#elif WHA_BITMAP
            _bits = new WAHBitArray();
#elif EWAH_BITMAP
            _bits = new EwahCompressedBitArray();
            _customSerializer = new EwahCompressedBitArraySerializer();
#elif ROARING_BITMAP
            _bits = RoaringBitmap.Create();
#endif
        }

#if MS_BITMAP
        private MyBitArray(BitArray bits)
#elif WHA_BITMAP
        private MyBitArray(WAHBitArray bits)
#elif EWAH_BITMAP
        private MyBitArray(EwahCompressedBitArray bits)
#elif ROARING_BITMAP
        private MyBitArray(RoaringBitmap bits)
#endif
        {
            _bits = bits;
#if EWAH_BITMAP
            _customSerializer = new EwahCompressedBitArraySerializer();
#endif
        }

        public void Save(IMangaSerializationWriter writer)
        {
#if MS_BITMAP
            byte[] bytes = _bits.GetBytes();
            writer.WriteBytes(bytes);
#elif WHA_BITMAP
            WAHBitArray.TYPE type;
            uint[] comp = _bits.GetCompressed(out type);
            writer.WriteEnum<WAHBitArray.TYPE>(type);
            writer.WriteUints(comp);
#elif EWAH_BITMAP || ROARING_BITMAP
            writer.WriteCustom(this);
#endif
        }

#if EWAH_BITMAP || ROARING_BITMAP
        public void Serialize(Stream serializationStream)
        {
#if EWAH_BITMAP
            _customSerializer.Serialize(serializationStream, _bits);
#elif ROARING_BITMAP
            RoaringBitmap.Serialize(_bits, serializationStream);
#endif
        }
#endif

        public void Load(int version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
#if MS_BITMAP
            byte[] bytes = reader.ReadBytes();
            _bits = new BitArray(bytes);
#elif WHA_BITMAP
            WAHBitArray.TYPE type = reader.ReadEnum<WAHBitArray.TYPE>();
            uint[] comp = reader.ReadUints();
            _bits = new WAHBitArray(type, comp);
#elif EWAH_BITMAP || ROARING_BITMAP
            reader.ReadCustom(this);
#endif
        }

#if EWAH_BITMAP || ROARING_BITMAP
        public void Deserialize(Stream serializationStream)
        {
#if EWAH_BITMAP
            _bits = _customSerializer.Deserialize(serializationStream);
#elif ROARING_BITMAP
            _bits = RoaringBitmap.Deserialize(serializationStream);
#endif
        }
#endif

        public MyBitArray And(MyBitArray value)
        {
#if ROARING_BITMAP            
            _bits = _bits & value._bits;
#else
            _bits = _bits.And(value._bits);
#endif
            return this;
        }

        public MyBitArray Or(MyBitArray value)
        {
#if ROARING_BITMAP            
            _bits = _bits | value._bits;
#else
            _bits = _bits.Or(value._bits);
#endif
            return this;
        }

        public MyBitArray Not()
        {
#if MS_BITMAP
            _bits = _bits.Not();
#elif WHA_BITMAP
            _bits = _bits.Not(_bits.Length);
#elif EWAH_BITMAP
            _bits.Not();
#elif ROARING_BITMAP 
            _bits = ~_bits;
#endif
            return this;
        }

        public MyBitArray Clone()
        {
#if MS_BITMAP
            var bits = _bits.Clone() as BitArray;

            return new MyBitArray(bits);
#elif WHA_BITMAP
            var bits = _bits.Copy();
            return new MyBitArray(bits); 
#elif EWAH_BITMAP
            var bits = _bits.Clone() as EwahCompressedBitArray;
            return new MyBitArray(bits);
#elif ROARING_BITMAP
            return RoaringBitmap.Create()
#endif
        }

        public void Set(int index)
        {
#if EWAH_BITMAP
                _bits.Set(index);
#elif ROARING_BITMAP
            x = _bits[index];
#else
            _bits.Set(index, true);

#endif
        }

        public Int32 GetCardinality()
        {
#if MS_BITMAP
            return _bits.GetCardinality();
#elif WHA_BITMAP
            return (Int32)_bits.CountOnes();
#elif EWAH_BITMAP
            return (Int32)_bits.GetCardinality();
#elif ROARING_BITMAP
            return (Int32)_bits.Cardinality;
#endif
        }

        public void SetPositions()
        {
#if EWAH_BITMAP
            _positions = _bits.GetPositions();
#endif
        }

        public void ClearPositions()
        {
#if EWAH_BITMAP
            _positions = null;
#endif
        }

        /*
                public bool On(int index)
                {
        #if EWAH_BITMAP
                    if (_positions == null)
                    {
                        foreach (int test in _bits)
                        {
                            if (test == index)
                                return true;
                            else if (test > index)
                                break;
                        }
                        return false;
                    }
                    else
                    {
                        return _positions.Contains(index);
                    }
        #elif ROARING_BITMAP
                    return _bits[]
        #else
                    return _bits.Get(index);
        #endif
                }
        */

        public IEnumerable<int> GetEnumerator()
        {
            var loopy = _bits.GetEnumerator();
            while (loopy.MoveNext())
            {
                yield return loopy.Current;
            }
        }
    }
}
