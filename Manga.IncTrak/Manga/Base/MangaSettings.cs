using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public class MangaSettings : MangaSerialize
    {
        private const Int32 MangaSettingsVersion1 = 1;

        /// Start Serialized Items
        private bool _showLowEqual = true;
        private int _showLowBound = 0;
        private bool _showHighEqual = true;
        private int _showHighBound = 100;
        private ShowPercentage _showPercentage = ShowPercentage.pct_tot_sel;
        private SelectOperation _selectOperation = SelectOperation.overwrite_selection;
        private bool _colAscending;
        private bool[] _hideColumns;
        /// end Serialized Items 

        protected override int Version => MangaSettingsVersion1;
        protected override string Spec => "";
        protected override MangaFileType FileType => MangaFileType.settings;

        protected override void Save(IMangaSerializationWriter writer)
        {
            writer.WriteBool(_showLowEqual);
            writer.WriteInt32(_showLowBound);
            writer.WriteBool(_showHighEqual);
            writer.WriteInt32(_showHighBound);
            writer.WriteEnum<ShowPercentage>(_showPercentage);
            writer.WriteEnum<SelectOperation>(_selectOperation);
            writer.WriteBool(_colAscending);
            if ( _hideColumns == null )
            {
                writer.WriteInt32(0);
            }
            else
            {
                writer.WriteInt32(_hideColumns.Length);
                foreach(bool showHide in _hideColumns)
                {
                    writer.WriteBool(showHide);
                }
            }
        }

        protected override void Load(int version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _showLowEqual = reader.ReadBool();
            _showLowBound = reader.ReadInt32();
            _showHighEqual = reader.ReadBool();
            _showHighBound = reader.ReadInt32();
            _showPercentage = reader.ReadEnum<ShowPercentage>();
            _selectOperation = reader.ReadEnum<SelectOperation>();
            _colAscending = reader.ReadBool();
            _hideColumns = new bool[reader.ReadInt32()];
            for(int i=0;i<_hideColumns.Length;i++)
            {
                _hideColumns[i] = reader.ReadBool();
            }
        }

        internal void SetOptions(bool showLowEqual, int showLowBound, bool showHighEqual, int showHighBound, string selectOperation, string showPercentage, bool colAscending, bool[] hideColumns)
        {
            _showLowEqual = showLowEqual;
            _showLowBound = showLowBound;
            _showHighEqual = showHighEqual;
            _showHighBound = showHighBound;
            Enum.TryParse<ShowPercentage>(showPercentage, out _showPercentage);
            Enum.TryParse<SelectOperation>(selectOperation, out _selectOperation);
            _colAscending = colAscending;
            _hideColumns = hideColumns;
        }

        public bool ShowLowEqual { get => _showLowEqual; }
        public int ShowLowBound { get => _showLowBound; }
        public bool ShowHighEqual { get => _showHighEqual; }
        public int ShowHighBound { get => _showHighBound; }
        public ShowPercentage ShowPercentage { get => _showPercentage; }
        public SelectOperation SelectOperation { get => _selectOperation; }
        public bool ColAscending { get => _colAscending; }
        public bool[] HideColumns { get => _hideColumns; set => _hideColumns = value; }
    }
}
