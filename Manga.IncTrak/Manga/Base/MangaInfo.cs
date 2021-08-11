using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public class MangaInfo : IMangaSerializeInline
    {
        private const Int32 MangaInfoVersion1 = 1;

        /// Start Serialized Items 
        private DateTime _created;
        private Guid _managGuid;
        private string _originalName;
        private string _mangaName;
        private int _headerRow;
        private int _headerRows;
        private int _maxRows;
        private bool _ignoreBlankRows = true;
        private bool _ignoreTextCase = true;
        private bool _trimLeadingWhitespace = false;
        private bool _trimTrailingWhitespace = true;
        private UInt32[] _ignoreColIndexes;
        private List<string> _ignoreColNames;
        private string _sheetType;
        private Dictionary<string, string> _extraInfo;
        /// End Serialized Items


        public string WorkingSetFile { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }

        public MangaInfo()
        {
        }

        public MangaInfo(string originalName, string mangaName, int headerRow, int headerRows, int maxRows, bool ignoreBlankRows, bool ignoreTextCase, bool trimLeadingWhitespace, bool trimTrailingWhitespace, string ignoreCols, string sheetType, Dictionary<string, string> extraInfo)
        {
            _created = DateTime.Now;
            _managGuid = Guid.NewGuid();
            _originalName = originalName;

            _mangaName = mangaName;
            _headerRow = headerRow;
            _headerRows = headerRows;
            _maxRows = maxRows;
            _ignoreBlankRows = ignoreBlankRows;
            _ignoreTextCase = ignoreTextCase;
            _trimLeadingWhitespace = trimLeadingWhitespace;
            _trimTrailingWhitespace = trimTrailingWhitespace;

        _ignoreColNames = new List<string>();
            if (string.IsNullOrWhiteSpace(ignoreCols))
            {
                _ignoreColIndexes = new UInt32[0];
            }
            else
            {
                var colIndexes = new List<UInt32>();
                foreach(var ignore in ignoreCols.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    UInt32 colIndex;
                    if ( UInt32.TryParse(ignore, out colIndex))
                    {
                        colIndexes.Add(colIndex);
                    }
                    else
                    {
                        _ignoreColNames.Add(ignore.Trim(' ', '"', '\t').ToLower());
                    }
                }
                _ignoreColIndexes = colIndexes.ToArray();
            }
            _sheetType = sheetType;
            _extraInfo = extraInfo;

            Status = "Queued";
        }

        public int Version => MangaInfoVersion1;
        public Guid ManagGuid { get => _managGuid; }
        public string OriginalName { get => _originalName; set => _originalName = value; }
        public string MangaName { get => _mangaName; }
        public string SheetType { get => _sheetType; }
        public DateTime Created { get => _created; }
        public int HeaderRow { get => _headerRow; }
        public int HeaderRows { get => _headerRows; }
        public int MaxRows { get => _maxRows; }
        public bool IgnoreBlankRows { get => _ignoreBlankRows; }
        public bool IgnoreTextCase { get => _ignoreTextCase; }
        public bool TrimLeadingWhitespace { get => _trimLeadingWhitespace; }
        public bool TrimTrailingWhitespace { get => _trimTrailingWhitespace; }
        public uint[] IgnoreColIndexes { get => _ignoreColIndexes; }
        public List<string> IgnoreColNames { get => _ignoreColNames; }

        public string GetExtraInfo(string key)
        {
            if (_extraInfo.ContainsKey(key))
                return _extraInfo[key];

            return null;
        }

        public void Load(int version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _created = new DateTime((long)reader.ReadUInt64());
            _managGuid = reader.ReadGuid();
            _originalName = reader.ReadString();

            _mangaName = reader.ReadString();
            _headerRow = reader.ReadInt32();
            _headerRows = reader.ReadInt32();
            _maxRows = reader.ReadInt32();
            _ignoreBlankRows = reader.ReadBool();
            _ignoreTextCase = reader.ReadBool();
            _trimLeadingWhitespace = reader.ReadBool();
            _trimTrailingWhitespace = reader.ReadBool();
            _ignoreColIndexes = reader.ReadArrayUInt32s();
            _ignoreColNames = reader.ReadListString();
            _sheetType = reader.ReadString();
            _extraInfo = reader.ReadDictStringString();

            Status = "Complete";
        }

        public void Save(IMangaSerializationWriter writer)
        {
            writer.WriteUInt64((UInt64)_created.Ticks);
            writer.WriteGuid(_managGuid);
            writer.WriteString(_originalName);

            writer.WriteString(_mangaName);
            writer.WriteInt32(_headerRow);
            writer.WriteInt32(_headerRows);
            writer.WriteInt32(_maxRows);
            writer.WriteBool(_ignoreBlankRows);
            writer.WriteBool(_ignoreTextCase);
            writer.WriteBool(_trimLeadingWhitespace);
            writer.WriteBool(_trimTrailingWhitespace);
            writer.WriteArrayUInt32s(_ignoreColIndexes);
            writer.WriteListString(_ignoreColNames);
            writer.WriteString(_sheetType);
            writer.WriteDictStringString(_extraInfo);
        }
    }
}
