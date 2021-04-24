using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public class MangaCatalog : MangaSerialize
    {
        private const Int32 MangaCatalogVersion1 = 1;

        /// Start Serialized Items 
        private string _accessToken;
        private string _userId;
        private string _userEmail;
        private MangaAuthType _authType;
        private Dictionary<Guid, MangaInfo> _mangas = new Dictionary<Guid, MangaInfo>();
        /// End Serialized Items 

        protected override Int32 Version => MangaCatalogVersion1;
        protected override string Spec => "";
        protected override MangaFileType FileType => MangaFileType.catalog;

        public string AccessToken { get => _accessToken; }
        public string UserId { get => _userId; }
        public MangaAuthType AuthType { get => _authType; }

        public List<MangaInfo> MyMangas { get => _mangas.Values.ToList(); }

        protected override void Save(IMangaSerializationWriter writer)
        {
            writer.WriteString(_accessToken);
            writer.WriteString(_userId);
            writer.WriteString(_userEmail);
            writer.WriteEnum<MangaAuthType>(_authType);
            Int32 items = _mangas.Count();
            writer.WriteInt32(items);
            foreach(var pairs in _mangas)
            {
                writer.WriteGuid(pairs.Key);
                writer.SaveChild(pairs.Value);
            }
        }

        protected override void Load(Int32 version, IMangaSerializationReader reader, MangaLoadOptions loadOptions)
        {
            _accessToken = reader.ReadString();
            _userId = reader.ReadString();
            _userEmail = reader.ReadString();
            _authType = reader.ReadEnum<MangaAuthType>();
            Int32 items = reader.ReadInt32();
            _mangas = new Dictionary<Guid, MangaInfo>(items);
            for (int i=0;i<items;i++)
            {
                Guid mangaId = reader.ReadGuid();

                _mangas.Add(mangaId, reader.LoadChild<MangaInfo>(new MangaInfo(), loadOptions));
            }
        }

        public void SetAccess(string accessToke)
        {
            _accessToken = accessToke;
        }

        public void SetAccess(string accessToken, string userIdentifier, string userEmail, MangaAuthType authType)
        {
            if (authType == MangaAuthType.Invalid)
                throw new Exception("Cannot use invalid auth type");
            _accessToken = accessToken;
            _userId = userIdentifier;
            _userEmail = userEmail;
            _authType = authType;
        }

        public void SetManga(MangaInfo mangaInfo)
        {
            if (_mangas.ContainsKey(mangaInfo.ManagGuid))
            {
                _mangas[mangaInfo.ManagGuid] = mangaInfo;
            }
            else
            {
                _mangas.Add(mangaInfo.ManagGuid, mangaInfo);
            }
        }

        public bool GetManga(Guid mangaGuid)
        {
            string mangaName;
            return GetManga(mangaGuid, out mangaName);
        }

        public bool GetManga(Guid mangaGuid, out string mangaName)
        {
            foreach (var manga in _mangas)
            {
                if (manga.Value.ManagGuid == mangaGuid)
                {
                    mangaName = manga.Value.MangaName;
                    return true;
                }
            }

            mangaName = null;
            return false;
        }

        public bool DeleteManga(Guid mangaGuid)
        {
            Guid? delKey = null;
            foreach (var manga in _mangas)
            {
                if (manga.Value.ManagGuid == mangaGuid && manga.Key == mangaGuid)
                {
                    delKey = manga.Key;
                    break;
                }
            }
            if (!delKey.HasValue || _mangas.ContainsKey(delKey.Value) == false)
                return false;
            _mangas.Remove(delKey.Value);
            return true;
        }
    }
}
