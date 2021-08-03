#define TEST_LIMITS
using Manga.IncTrak.Manga.Serialization;
using Manga.IncTrak.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Manga.IncTrak.Manga
{
    public static class MangaState
    {
        public static string ManagaFilePath(string mangaPath, MangaFileType fileType, string spec)
        {
            switch (fileType)
            {
                case MangaFileType.coldef:
                    mangaPath = Path.Combine(mangaPath, MangaFileType.coldef.ToString());
                    break;
                case MangaFileType.coldata:
                case MangaFileType.colindex:
                case MangaFileType.colnulls:
                    mangaPath = Path.Combine(mangaPath, "raw");
                    break;
                case MangaFileType.textpatterns:
                case MangaFileType.datepatterns:
                case MangaFileType.numericpatterns:
                    mangaPath = Path.Combine(mangaPath, "patterns");
                    break;
                case MangaFileType.filter:
                case MangaFileType.bucket:
                    mangaPath = Path.Combine(mangaPath, "adjustments");
                    break;
                case MangaFileType.catalog:
                case MangaFileType.datamap:
                case MangaFileType.settings:
                default:
                    break;
            }

            Directory.CreateDirectory(mangaPath);

            return Path.Combine(mangaPath, string.Format("{0}{1}{2}.manga", fileType, spec == String.Empty ? "" : "_", spec));
        }

        private static string UserPath(string userFolder)
        {
            return Path.Combine(MangaRoot.Folder, userFolder);
        }

        public static void SetUserMangaCatalog(string accessToken, string userIdentifier, string userEmail, MangaAuthType authType)
        {
            MangaCatalog mangas = LoadUserMangaCatalog(userIdentifier, new MangaLoadOptions(false));
            mangas.SetAccess(accessToken, userIdentifier, userEmail, authType);

            var userFolder = UserPath(userIdentifier);
            mangas.Save(userFolder);
        }

        public static MangaAuthType ValidateUserMangaCatalog(Tuple<string, Guid> userAccess)
        {
            MangaCatalog mangas = LoadUserMangaCatalog(userAccess.Item1, new MangaLoadOptions(false));

            bool rc = (mangas.UserId == userAccess.Item1 && mangas.AccessToken == userAccess.Item2.ToString("N"));
            if (rc)
            {
                return mangas.AuthType;
            }

            return MangaAuthType.Invalid;
        }

        public static void CheckProjectCount(string userIdentifier)
        {
            var cats = LoadUserMangaCatalog(userIdentifier, new MangaLoadOptions(true));
            VisAlyzerLicense.CheckProjectCount(cats.MyMangas.Count);
        }

        public static MangaCatalog LoadUserMangaCatalog(string userIdentifier, MangaLoadOptions loadOptions)
        {
            MangaCatalog mangas = new MangaCatalog();
            var userPath = UserPath(userIdentifier);
            if ( mangas.Load(userPath, loadOptions) )
                return mangas;
            return new MangaCatalog(); 
        }

        public static void UserLog(string userFolder, string mangaName, string message)
        {
#if DEBUG
            var performanceFile = Path.Combine(UserPath(userFolder), "performance.txt");
            using (StreamWriter logFile = new StreamWriter(performanceFile, true))
            {
                logFile.WriteLine("{0}: {1} - {2}", DateTime.Now.ToString("yyy-MM-dd HH:mm:ss"), mangaName, message);
            }
#endif
        }

        public static void UserLogSize(string userFolder, string mangaName, Guid mangaGuid)
        {
#if DEBUG
            var performanceFile = Path.Combine(UserPath(userFolder), "performance.txt");
            using (StreamWriter logFile = new StreamWriter(performanceFile, true))
            {
                var mangaPath = ManagaPath(userFolder, mangaGuid);
                var message = StorageHelpers.FolderSizes(mangaPath);
                logFile.WriteLine("{0} - {1}", mangaName, message);
            }
#endif
        }

        private static string ManagaPath(string userFolder, Guid mangaGuid)
        {
            return Path.Combine(UserPath(userFolder), mangaGuid.ToString("N"));
        }

        public static string MangaWorkFolder(string userFolder)
        {
            var mangaWorkFolder = Path.Combine(UserPath(userFolder), "work");
            Directory.CreateDirectory(mangaWorkFolder);
            return mangaWorkFolder;
        }

        public static void SaveManga(string userFolder, MangaInfo mangaInfo, DataManga manga)
        {
            MangaCatalog mangas = LoadUserMangaCatalog(userFolder, new MangaLoadOptions(false));
            mangas.SetManga(mangaInfo);

            var userPath = UserPath(userFolder);
            mangas.Save(userPath);

            var mangaPath = ManagaPath(userFolder, mangaInfo.ManagGuid);
            manga.ProcessWorkingFolder(mangaPath);
            manga.Save(mangaPath);
        }

        public static bool SaveMangaSettings(Tuple<string, Guid> visId, bool showLowEqual, int showLowBound, bool showHighEqual, int showHighBound, string selectOperation, string showPercentage, bool colAscending, bool[] hideColumns)
        {
            var userFolder = visId.Item1;
            Guid mangaGuid = visId.Item2;

            var mangas = LoadUserMangaCatalog(userFolder, new MangaLoadOptions(false));
            if (mangas.GetManga(mangaGuid))
            {
                MangaSettings mangaSettings = LoadMangaSettings(visId);
                mangaSettings.SetOptions(showLowEqual, showLowBound, showHighEqual, showHighBound, selectOperation, showPercentage, colAscending, hideColumns);

                var mangaPath = ManagaPath(userFolder, mangaGuid);

                mangaSettings.Save(mangaPath);

                return true;
            }

            return false;
        }

        internal static void SaveMangaBuckets(Tuple<string, Guid> visId, int colIndex, ColumnDefBucket column)
        {
            var userFolder = visId.Item1;
            Guid mangaGuid = visId.Item2;

            var mangas = LoadUserMangaCatalog(userFolder, new MangaLoadOptions(false));
            if (mangas.GetManga(mangaGuid))
            {
                var mangaPath = ManagaPath(userFolder, mangaGuid);

                if (column != null)
                {
                    column.Save(mangaPath);
                }
                else
                {
                    StorageHelpers.SafeFileDelete(MangaState.ManagaFilePath(mangaPath, MangaFileType.bucket, colIndex.ToString()));
                }
            }
        }

        public static DataManga LoadManga(Tuple<string, Guid> visId, bool applyFilter, int ignoreBucketCol, MangaLoadOptions loadOptions)
        {
            string mangaName;
            return LoadManga(visId, applyFilter, ignoreBucketCol, loadOptions, out mangaName);
        }

        public static DataManga LoadManga(Tuple<string, Guid> visId, bool applyFilter, int ignoreBucketCol, MangaLoadOptions loadOptions, out string mangaName)
        {
            var userFolder = visId.Item1;
            Guid mangaGuid = visId.Item2;

            var mangas = LoadUserMangaCatalog(userFolder, new MangaLoadOptions(false));
            
            if ( mangas.GetManga(mangaGuid, out mangaName) )
            {
                var mangaPath = ManagaPath(userFolder, mangaGuid);

                var manga = new DataManga();
                if (manga.Load(mangaPath, loadOptions))
                {
                    MangaSettings settings = LoadMangaSettings(visId);
                    manga.ApplySettings(settings);

                    for (int colIndex = 0; colIndex < manga.Columns; colIndex++)
                    {
                        if (colIndex != ignoreBucketCol)
                        {
                            ColumnDefBucket column = LoadMangaBucket(visId, colIndex, loadOptions);
                            if (column != null)
                            {
                                manga.ApplyBuckets(column, colIndex);
                            }
                        }
                    }

                    if (applyFilter)
                    {
                        MangaFilter filter = LoadMangaFilter(visId, loadOptions);
                        manga.ApplyFilter(filter, applyFilter, ignoreBucketCol);
                    }

                    return manga;
                }
            }

            return new DataManga();
        }

        private static ColumnDefBucket LoadMangaBucket(Tuple<string, Guid> visId, int colIndex, MangaLoadOptions loadOptions)
        {
            var userFolder = visId.Item1;
            Guid mangaGuid = visId.Item2;

            var mangas = LoadUserMangaCatalog(userFolder, new MangaLoadOptions(false));
            if (mangas.GetManga(mangaGuid))
            {
                var mangaPath = ManagaPath(userFolder, mangaGuid);
                ColumnDefBucket bucket = new ColumnDefBucket(colIndex);
                if (bucket.Load(mangaPath, loadOptions))
                {
                    return bucket;
                }
            }

            return null;
        }

        private static MangaSettings LoadMangaSettings(Tuple<string, Guid> visId)
        {
            var userFolder = visId.Item1;
            Guid mangaGuid = visId.Item2;

            MangaLoadOptions loadOptions = new MangaLoadOptions(false);
            var mangas = LoadUserMangaCatalog(userFolder, loadOptions);
            if (mangas.GetManga(mangaGuid))
            {
                var mangaPath = ManagaPath(userFolder, mangaGuid);

                MangaSettings settings = new MangaSettings();
                if (settings.Load(mangaPath, loadOptions))
                {
                    return settings;
                }
            }

            return new MangaSettings();
        }

        private static MangaFilter LoadMangaFilter(Tuple<string, Guid> visId, MangaLoadOptions loadOptions)
        {
            var userFolder = visId.Item1;
            Guid mangaGuid = visId.Item2;

            var mangas = LoadUserMangaCatalog(userFolder, new MangaLoadOptions(false));
            if (mangas.GetManga(mangaGuid))
            {
                var mangaPath = ManagaPath(userFolder, mangaGuid);
                MangaFilter filter = new MangaFilter();
                if ( filter.Load(mangaPath, loadOptions) )
                {
                    return filter;
                }
            }

            return new MangaFilter();
        }

        public static MangaFilter SaveMangaFilter(Tuple<string, Guid> visId, string selectionExpression, MyBitArray bitmapFilter)
        {
            var userFolder = visId.Item1;
            Guid mangaGuid = visId.Item2;

            MangaLoadOptions loadOptions = new MangaLoadOptions(false);
            var mangas = LoadUserMangaCatalog(userFolder, loadOptions);
            if (mangas.GetManga(mangaGuid))
            {
                MangaFilter mangaFilter = LoadMangaFilter(visId, loadOptions);
                mangaFilter.SetFilter(selectionExpression, bitmapFilter);

                var mangaPath = ManagaPath(userFolder, mangaGuid);
                if (string.IsNullOrWhiteSpace(selectionExpression))
                {
                    mangaFilter.Delete(mangaPath);
                }
                else if (bitmapFilter != null)
                {
                    mangaFilter.Save(mangaPath);
                }

                return mangaFilter;
            }

            return null;
        }

        public static bool DeleteManga(Tuple<string, Guid> visId)
        {
            var userFolder = visId.Item1;
            Guid mangaGuid = visId.Item2;

            var mangas = LoadUserMangaCatalog(userFolder, new MangaLoadOptions(false));
            if (mangas.DeleteManga(mangaGuid))
            {
                var userPath = UserPath(userFolder);
                mangas.Save(userPath);
                var mangaPath = ManagaPath(userFolder, mangaGuid);
                if (Directory.Exists(mangaPath))
                    Directory.Delete(mangaPath, true);
                return true;
            }

            return false;
        }
    }
}
