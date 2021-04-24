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
        private Int32 _showGESelected = 0;
        private Int32 _showLESelected = 100;
        private ShowPercentage _showPercentage = ShowPercentage.pct_of_sel;
        private SelectOperation _selectOperation = SelectOperation.overwrite_selection;
        private bool _colAscending;
        private bool[] _hideColumns;
        /// end Serialized Items 

        protected override int Version => MangaSettingsVersion1;
        protected override string Spec => "";
        protected override MangaFileType FileType => MangaFileType.settings;

        protected override void Save(IMangaSerializationWriter writer)
        {
            writer.WriteInt32(_showGESelected);
            writer.WriteInt32(_showLESelected);
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
            _showGESelected = reader.ReadInt32();
            _showLESelected = reader.ReadInt32();
            _showPercentage = reader.ReadEnum<ShowPercentage>();
            _selectOperation = reader.ReadEnum<SelectOperation>();
            _colAscending = reader.ReadBool();
            _hideColumns = new bool[reader.ReadInt32()];
            for(int i=0;i<_hideColumns.Length;i++)
            {
                _hideColumns[i] = reader.ReadBool();
            }
        }

        internal void SetOptions(int showGESelected, int showLESelected, string selectOperation, string showPercentage, bool colAscending, bool[] hideColumns)
        {
            _showGESelected = showGESelected;
            _showLESelected = showLESelected;
            Enum.TryParse<ShowPercentage>(showPercentage, out _showPercentage);
            Enum.TryParse<SelectOperation>(selectOperation, out _selectOperation);
            _colAscending = colAscending;
            _hideColumns = hideColumns;
        }

        public int ShowGESelected { get => _showGESelected; }
        public int ShowLESelected { get => _showLESelected; }
        public ShowPercentage ShowPercentage { get => _showPercentage; }
        public SelectOperation SelectOperation { get => _selectOperation; }
        public bool ColAscending { get => _colAscending; }
        public bool[] HideColumns { get => _hideColumns; set => _hideColumns = value; }
    }
}
