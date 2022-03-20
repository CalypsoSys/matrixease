using Manga.IncTrak.Manga;
using Manga.IncTrak.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Manga.IncTrak.Processing
{
    public abstract class MangaFactory : MangaJobFactory
    {
        private static int _maxWorkersPerUser = 1;
        private static volatile object _lockWorkingSets = new object();
        private static Dictionary<string, Dictionary<Guid, MangaInfo>> _workingSets = new Dictionary<string, Dictionary<Guid, MangaInfo>>();
        private string _workSetFile;
        private string _userId;
        private string _workFolder;
        private MangaInfo _mangaInfo;
        private int _rowIndex = 0;
        private int _calculatedNumberOfCols = 0;
        private List<UInt32> _ignoreIndexes = null;

        public Guid WorkSet { get => _mangaInfo.ManagGuid; }

        protected abstract IEnumerable<IList<object>> RowIterator(IBackgroundJob status);
        protected abstract void Cleanup();

        protected MangaFactory(string userId, MangaInfo mangaInfo)
        {
            _workFolder = MangaState.MangaWorkFolder(userId);
            lock (_lockWorkingSets)
            {
                if (_workingSets.ContainsKey(userId) && _workingSets[userId].Count > _maxWorkersPerUser)
                    throw new Exception("Max workers for this user - only 1 job at a time for this license type");
                else if (_workingSets.Count == 0)
                {
                    // Cleanup any old stale data and reset
                    if (Directory.Exists(_workFolder))
                        Directory.Delete(_workFolder, true);
                    _workFolder = MangaState.MangaWorkFolder(userId);
                }

                _mangaInfo = mangaInfo;
                _workSetFile = Path.Combine(_workFolder, string.Format("{0}.workset", _mangaInfo.ManagGuid.ToString("N")));
                mangaInfo.WorkingSetFile = _workSetFile;

                if (_workingSets.ContainsKey(userId) == false)
                    _workingSets.Add(userId, new Dictionary<Guid, MangaInfo>());
                _workingSets[userId].Add(_mangaInfo.ManagGuid, mangaInfo);
                _userId = userId;
            }
        }

        protected RowStatus IsStatusCheck(IBackgroundJob status)
        {
            if (_mangaInfo.MaxRows != 0)
            {
                if (_rowIndex > _mangaInfo.MaxRows)
                    return RowStatus.Break;
            }
            if ((_rowIndex % TaskConstants.StatusUpdateCheckFreq) == 0)
            {
                if (status.IsCancellationRequested)
                    return RowStatus.Break;

                return RowStatus.Check;
            }

            return RowStatus.Continue;
        }

        protected void SendStatus(int rowCount, long pctRead)
        {
            string overallStat = string.Empty;
            if (rowCount != -1)
                overallStat = string.Format("{0} of {1} - {2}%", _rowIndex, rowCount, ((_rowIndex * 100) / rowCount));
            else if (pctRead != -1)
                overallStat = string.Format("{0} - {1}%", _rowIndex, pctRead);
            string limitStat = string.Empty;
            if (_mangaInfo.MaxRows != 0)
            {
                limitStat = string.Format(" Limit Row {0} of {1} - {2}%", _rowIndex, _mangaInfo.MaxRows, ((_rowIndex * 100) / _mangaInfo.MaxRows));
            }
            SetStatus(MangaFactoryStatusKey.Processing, string.Format("Processing row {0}{1}", overallStat, limitStat), MangaFactoryStatusState.Running);
        }

        public static MangaInfo[] GetPending(string userId)
        {
            lock (_lockWorkingSets)
            {
                if (_workingSets.ContainsKey(userId))
                {
                    return _workingSets[userId].Values.ToArray();
                }
            }

            return new MangaInfo[0];
        }

        public static object GetStatus(string userId, Guid workSet)
        {
            string workSetFile = null;
            lock (_lockWorkingSets)
            {
                if (_workingSets.ContainsKey(userId))
                {
                    if (_workingSets[userId].ContainsKey(workSet))
                        workSetFile = _workingSets[userId][workSet].WorkingSetFile;
                }
            }

            if (workSetFile != null)
            {
                using (new FileLocker(workSetFile))
                {
                    if (File.Exists(workSetFile))
                    {
                        using (StreamReader reader = new StreamReader(workSetFile))
                        {
                            var result = reader.ReadToEnd();
                            var statuses = JsonSerializer.Deserialize<Dictionary<MangaFactoryStatusKey, MangaFactoryStatus>>(result);
                            foreach (var val in statuses.Values)
                                val.UpdateElapsed();
                            return new { Success = true, Complete = false, StatusData = statuses };
                        }
                    }
                }

                lock (_lockWorkingSets)
                {
                    bool success = true;
                    string message = "";
                    if (_workingSets.ContainsKey(userId))
                    {
                        if (_workingSets[userId].ContainsKey(workSet))
                        {
                            if (_workingSets[userId][workSet].Status == "Failed")
                            {
                                success = false;
                                message = _workingSets[userId][workSet].Message;
                            }
                            _workingSets[userId].Remove(workSet);
                        }
                        if (_workingSets[userId].Count == 0)
                        {
                            _workingSets.Remove(userId);
                        }
                    }

                    return new { Success = success, Complete= true, Message = message };
                }
            }

            return new { Success = false };
        }

        public override void SetStatusExtra(Dictionary<MangaFactoryStatusKey, MangaFactoryStatus> status)
        {
            using (new FileLocker(_workSetFile))
            {
                using (StreamWriter writer = new StreamWriter(_workSetFile))
                {
                    writer.Write( JsonSerializer.Serialize(status) );
                }
            }
        }

        protected override void DisposeManaged()
        {
            Cleanup();
            StorageHelpers.SafeFileDelete(_workSetFile);
        }

        protected override void DisposeUnManaged()
        {
        }

        private void ProcessHeaderRow(bool hasHeader, DataManga manga, IList<object> row)
        {
            _ignoreIndexes = manga.AddHeaders(hasHeader, row, _workFolder, _mangaInfo.IgnoreColIndexes, _mangaInfo.IgnoreColNames, _calculatedNumberOfCols);
            _rowIndex = 0;
        }

        private void ProcessRow(DataManga manga, int rawRowIndex, IList<object> row)
        {
            if (_mangaInfo.HeaderRows != 0 && rawRowIndex <= _mangaInfo.HeaderRow)
            {
                if (_mangaInfo.HeaderRow == rawRowIndex)
                {
                    ProcessHeaderRow(true, manga, row);
                }
            }
            else
            {
                if (_mangaInfo.HeaderRows == 0 && _ignoreIndexes == null)
                {
                    ProcessHeaderRow(false, manga, row);
                }

                bool addRow = !_mangaInfo.IgnoreBlankRows;
                if (addRow == false)
                {
                    for (int i = 0; addRow == false && i < row.Count; i++)
                    {
                        addRow = !string.IsNullOrWhiteSpace(row[i] as string);
                    }
                }

                if (addRow)
                {
                    manga.AddRow(row, _rowIndex, this, _ignoreIndexes, _calculatedNumberOfCols, _mangaInfo.IgnoreTextCase, _mangaInfo.TrimLeadingWhitespace, _mangaInfo.TrimTrailingWhitespace);
                    ++_rowIndex;
                }
            }

        }

        private void ProcessSamples(List<IList<object>> samples, DataManga manga)
        {
            int sampleRowIndex = 1;
            foreach (var sampleRow in samples)
            {
                ProcessRow(manga, sampleRowIndex, sampleRow);
                ++sampleRowIndex;
            }
        }

        public override void Process(CancellationToken cancellationToken)
        {
            DataManga manga = null;

            try
            {
                CancelToken = cancellationToken;
                _mangaInfo.Status = "Running";
                SetStatus(MangaFactoryStatusKey.Queued, "Job execution started", MangaFactoryStatusState.Complete);

                SetStatus(MangaFactoryStatusKey.Processing, "Starting processing of data", MangaFactoryStatusState.Starting);
                MyStopWatch stopWatch = MyStopWatch.StartNew("MatrixEase Total Time");

                stopWatch.StartSubTime("MatrixEase Initialization");
                manga = new DataManga();
                MangaState.UserLog(_userId, _mangaInfo.OriginalName, stopWatch.StopSubTime());
                if (IsCancellationRequested)
                    return;

                stopWatch.StartSubTime("MatrixEase Data Loop");
                int rawRowIndex = 1, totalCells = 0;
                List<IList<object>> samples = new List<IList<object>>();
                foreach (var row in RowIterator(this))
                {
                    if (rawRowIndex < MangaConstants.SampleSize)
                    {
                        samples.Add(row);
                        for (int i = 0; i < row.Count; i++)
                        {
                            if (row[i] != null && string.IsNullOrWhiteSpace(row[i].ToString()) == false)
                            {
                                _calculatedNumberOfCols = Math.Max(i + 1, _calculatedNumberOfCols);
                            }
                        }
                    }
                    else if (rawRowIndex == MangaConstants.SampleSize)
                    {
                        ProcessSamples(samples, manga);
                        ProcessRow(manga, rawRowIndex, row);
                    }
                    else
                    {
                        ProcessRow(manga, rawRowIndex, row);
                    }
                    ++rawRowIndex;
                    totalCells += _calculatedNumberOfCols;

                    if ((rawRowIndex % TaskConstants.StatusUpdateCheckFreq) == 0)
                    {
                        MatrixEaseLicense.CheckCellCount(totalCells);
                        //SetStatus(MangaFactoryStatusKey.Processing, string.Format("Processing row {0}", rowIndex), MangaFactoryStatusState.Running);
                        if (IsCancellationRequested)
                        {
                            return;
                        }
                    }
                }
                if (rawRowIndex < MangaConstants.SampleSize)
                {
                    ProcessSamples(samples, manga);
                }

                SetStatus(MangaFactoryStatusKey.Processing, string.Format("Processed {0} rows", rawRowIndex), MangaFactoryStatusState.Complete);
                MangaState.UserLog(_userId, _mangaInfo.OriginalName, stopWatch.StopSubTime());

                stopWatch.StartSubTime("MatrixEase Process");
                SetStatus(MangaFactoryStatusKey.Analyzing, "Analyzing data", MangaFactoryStatusState.Started);
                manga.Process(this);
                SetStatus(MangaFactoryStatusKey.Analyzing, "Analyzing data", MangaFactoryStatusState.Complete);
                MangaState.UserLog(_userId, _mangaInfo.OriginalName, stopWatch.StopSubTime());

                stopWatch.StartSubTime("MatrixEase Save");
                SetStatus(MangaFactoryStatusKey.Saving, "Saving dataset", MangaFactoryStatusState.Started);
                if (IsCancellationRequested)
                    return;

                _mangaInfo.SetCounts(rawRowIndex, _calculatedNumberOfCols, totalCells);
                MangaState.SaveManga(_userId, _mangaInfo, manga);
                SetStatus(MangaFactoryStatusKey.Saving, "Saving dataset", MangaFactoryStatusState.Complete);
                MangaState.UserLog(_userId, _mangaInfo.OriginalName, stopWatch.StopSubTime());

                MangaState.UserLog(_userId, _mangaInfo.OriginalName, stopWatch.Stop());
                MangaState.UserLogSize(_userId, _mangaInfo.OriginalName, WorkSet);
                SetStatus(MangaFactoryStatusKey.Complete, "MatrixEase Job", MangaFactoryStatusState.Complete);

                _mangaInfo.Status = "Complete";
            }
            catch (MatrixEaseLicenseException licExcp)
            {
                SetStatus(MangaFactoryStatusKey.Failed, licExcp.Message, MangaFactoryStatusState.Failed);
                _mangaInfo.Status = "Failed";
                _mangaInfo.Message = licExcp.Message;
            }
            catch (Exception excp)
            {
                SetStatus(MangaFactoryStatusKey.Failed, "An known error has occured", MangaFactoryStatusState.Failed);
                _mangaInfo.Status = "Failed";
                _mangaInfo.Message = "An known error has occured";
                MyLogger.LogError(excp, "Error adding MatrixEase");
            }

            if (_mangaInfo.Status != "Complete" && manga != null)
            {
                manga.CleanupWorkingSet();
            }
        }
    }
}
