using Manga.IncTrak.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public class MangaFilter : MangaSerialize
    {
        private const Int32 MangaFilterVersion1 = 1;

        /// Start Serialized Items 
        private string _selectionExpression;
        private MyBitArray _bitmapFilter;
        /// end Serialized Items 

        protected override int Version => MangaFilterVersion1;
        protected override string Spec => "";
        protected override MangaFileType FileType => MangaFileType.filter;

        protected override void Save(IMangaSerializationWriter writer)
        {
            writer.WriteString(_selectionExpression);
            writer.SaveChild(_bitmapFilter);
        }

        protected override void Load(int version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _selectionExpression = reader.ReadString();
            _bitmapFilter = reader.LoadChild<MyBitArray>(new MyBitArray(0), loadOptions);
        }

        public void SetFilter(string selectionExpression, MyBitArray bitmapFilter)
        {
            _selectionExpression = selectionExpression;
            _bitmapFilter = bitmapFilter;
        }

        public bool HasFilter
        {
            get
            {
                return string.IsNullOrWhiteSpace(_selectionExpression) == false && _bitmapFilter != null;
            }
        }

        public string SelectionExpression { get => _selectionExpression; }
        public MyBitArray BitmapFilter { get => _bitmapFilter; }
    }
}
