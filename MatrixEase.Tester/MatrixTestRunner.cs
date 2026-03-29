using System.Collections.Concurrent;
using System.Dynamic;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Manga.Serialization;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MatrixEase.Tester;

public sealed class MatrixTestRunner
{
    private const string MatrixEaseIdentifier = "matrixease.tester";
    private static readonly Regex CleanName = new Regex("[^a-zA-Z0-9 -]", RegexOptions.Compiled);
    private static readonly ConcurrentQueue<MyPerformance> PerformanceQueue = new ConcurrentQueue<MyPerformance>();
    private static readonly AsyncLocal<CancellationToken> ActiveCancellation = new AsyncLocal<CancellationToken>();

    private readonly MatrixTestSpec _spec;
    private readonly string _outputDirectory;
    private readonly TextWriter _stdout;
    private readonly TextWriter _stderr;

    public MatrixTestRunner(MatrixTestSpec spec, string outputDirectory, TextWriter stdout, TextWriter stderr)
    {
        _spec = spec;
        _outputDirectory = outputDirectory;
        _stdout = stdout;
        _stderr = stderr;
    }

    public async Task<int> RunAsync(bool baseline, int? maxRowsOverride, IReadOnlyList<string> onlyNames, bool includeDisabled, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_outputDirectory);
        string testsPath = Path.Combine(_outputDirectory, "tests");
        Directory.CreateDirectory(testsPath);

        MangaRoot.SetRootFolder(testsPath);
        MangaState.SetUserMangaCatalog(MatrixEaseIdentifier, MatrixEaseIdentifier, MatrixEaseIdentifier, MangaAuthType.Testing);
        MangaState.SetPerformanceLogger(PerformanceLogger);

        int failures = 0;
        foreach (var test in SelectTests(onlyNames, includeDisabled))
        {
            if (!await RunSingleAsync(test, baseline, maxRowsOverride, testsPath, cancellationToken))
            {
                failures++;
            }
        }

        return failures == 0 ? 0 : 1;
    }

    private IEnumerable<MatrixTestCase> SelectTests(IReadOnlyList<string> onlyNames, bool includeDisabled)
    {
        IEnumerable<MatrixTestCase> tests = _spec.Tests;
        if (!includeDisabled)
        {
            tests = tests.Where(test => test.Enabled);
        }

        if (onlyNames.Count > 0)
        {
            var selected = new HashSet<string>(onlyNames, StringComparer.OrdinalIgnoreCase);
            tests = tests.Where(test => selected.Contains(test.Name));
        }

        return tests;
    }

    private async Task<bool> RunSingleAsync(MatrixTestCase test, bool baseline, int? maxRowsOverride, string testsPath, CancellationToken cancellationToken)
    {
        DateTime startTime = DateTime.Now;
        Guid? mangaGuid = null;
        string status = "Completed";
        string testName = test.Name;

        try
        {
            await _stdout.WriteLineAsync($"[{startTime:yyyy-MM-dd HH:mm:ss}] Processing {test.Name}");
            while (PerformanceQueue.TryDequeue(out _))
            {
            }

            var resolved = ResolvedTestCase.Resolve(_spec.Defaults, test, maxRowsOverride);
            Dictionary<string, object> extraDetails = CreateExtraDetails(resolved);
            MyStopWatch stopWatch = MyStopWatch.StartNew("total_test_time", "MatrixEase Test Run");
            stopWatch.StartSubTime("init_time", "MatrixEase Initialization");

            if (resolved.Type == "google")
            {
                var sheetSpec = new Dictionary<string, string>
                {
                    { MangaConstants.SheetID, resolved.SheetId },
                    { MangaConstants.SheetRange, resolved.Range },
                };

                MangaInfo mangaInfo = CreateMangaInfo(resolved, "", resolved.SheetId, sheetSpec);
                UserCredential credential = GoogsAuth.AuthenticateLocal(resolved.GoogleClientId, resolved.GoogleClientSecret);
                mangaGuid = await RunBackgroundAsync(process => SheetProcessing.ProcessSheet(credential, MatrixEaseIdentifier, mangaInfo, process), cancellationToken);
                extraDetails.Add("google_name", mangaInfo.OriginalName);
                testName = CleanName.Replace(mangaInfo.OriginalName, "");
            }
            else
            {
                var sheetSpec = new Dictionary<string, string>
                {
                    { MangaConstants.CsvSeparator, resolved.Separator.ToString() },
                    { MangaConstants.CsvQuote, "\"" },
                    { MangaConstants.CsvEscape, "\"" },
                    { MangaConstants.CsvNull, "" },
                    { MangaConstants.CsvEol, "\r\n" },
                };

                string sourceName = Path.GetFileNameWithoutExtension(resolved.Path);
                MangaInfo mangaInfo = CreateMangaInfo(resolved, resolved.Path, sourceName, sheetSpec);
                using var input = new StreamReader(resolved.Path);
                mangaGuid = await RunBackgroundAsync(process => SheetProcessing.ProcessSheet(MatrixEaseIdentifier, input.BaseStream, mangaInfo, process), cancellationToken);
            }

            string extension = baseline ? "base" : DateTime.Now.ToString("yyyyMMddHHmmss");
            using var matrixTest = new JsonStreamer(Path.ChangeExtension(Path.Combine(testsPath, testName), extension));
            matrixTest.WriteNode("ExtraDetails", extraDetails);
            matrixTest.WriteNode("PerformanceStats", CollectAndRenderResults(mangaGuid.Value, matrixTest, stopWatch));

            MangaState.DeleteManga(Tuple.Create(MatrixEaseIdentifier, mangaGuid.Value));
            return true;
        }
        catch (Exception excp)
        {
            status = "Error";
            await _stderr.WriteLineAsync(excp.ToString());
            SimpleLogger.LogError(excp, test.Name);
            return false;
        }
        finally
        {
            DateTime endTime = DateTime.Now;
            await _stdout.WriteLineAsync($"[{endTime:yyyy-MM-dd HH:mm:ss}] {status} - {test.Name} - [{endTime - startTime}]");
            if (mangaGuid.HasValue)
            {
                try
                {
                    MangaFactory.GetStatus(MatrixEaseIdentifier, mangaGuid.Value);
                }
                catch
                {
                }
            }
        }
    }

    private List<MyPerformance> CollectAndRenderResults(Guid mangaGuid, JsonStreamer matrixTest, MyStopWatch stopWatch)
    {
        var perfStats = new List<MyPerformance>();
        perfStats.Add(stopWatch.StopSubTime());

        var myManga = Tuple.Create(MatrixEaseIdentifier, mangaGuid);
        stopWatch.StartSubTime("load_display_matrix", "Loading the MatrixEase for Display");
        string mangaName;
        var mangaDisplay = MangaState.LoadManga(myManga, true, -1, new MangaLoadOptions(true), out mangaName);
        var matrixDisplayData = mangaDisplay.ReturnMatrixEase();
        perfStats.Add(stopWatch.StopSubTime());
        matrixTest.WriteNode("MatrixDisplayData", matrixDisplayData);

        dynamic matrix = JsonConvert.DeserializeObject<ExpandoObject>(JsonConvert.SerializeObject(matrixDisplayData), new ExpandoObjectConverter());
        List<int> measures = new List<int>();
        foreach (dynamic col in matrix.Columns)
        {
            int index = (int)col.Value.Index;
            if (col.Value.ColType == "Measure")
            {
                measures.Add(index);
            }
        }

        stopWatch.StartSubTime("load_full_matrix", "Loading the MatrixEase full");
        var manga = MangaState.LoadManga(myManga, true, -1, new MangaLoadOptions(false), out mangaName);
        perfStats.Add(stopWatch.StopSubTime());

        stopWatch.StartSubTime("load_columndata", "Loading the MatrixEase Columns");
        matrixTest.StartNode("ColumnData");
        foreach (dynamic col in matrix.Columns)
        {
            Dictionary<string, object> colData = new Dictionary<string, object>();
            int index = (int)col.Value.Index;
            stopWatch.StartSubTime("load_column_stats", "Loading the MatrixEase Columns Stats");
            colData.Add("col_stats", manga.ReturnColStats(index));

            double lastHighestPct = 0;
            string nodeToMeasure = "";
            foreach (var node in col.Value.Values)
            {
                double totalPct = node.TotalPct;
                if (totalPct > lastHighestPct)
                {
                    nodeToMeasure = node.ColumnValue;
                    if (totalPct > 40)
                    {
                        break;
                    }
                    lastHighestPct = totalPct;
                }
            }
            perfStats.Add(stopWatch.StopSubTime());

            var selectedNode = string.Format("{0}@{1}:{2}", nodeToMeasure, col.Key, index);
            if (measures.Count > 0)
            {
                colData.Add("col_measures", manga.GetMeasureStats(selectedNode, measures.ToArray()));
            }

            matrixTest.WriteNode(col.Key, colData);
        }
        perfStats.Add(stopWatch.StopSubTime());
        matrixTest.EndNode();

        while (PerformanceQueue.TryDequeue(out var perf))
        {
            perfStats.Add(perf);
        }

        perfStats.Add(stopWatch.Stop());
        return perfStats;
    }

    private static MangaInfo CreateMangaInfo(ResolvedTestCase resolved, string originalName, string mangaName, Dictionary<string, string> extraInfo)
    {
        return new MangaInfo(
            originalName,
            mangaName,
            resolved.HeaderRow,
            resolved.HeaderRows,
            resolved.MaxRows,
            resolved.IgnoreBlankRows,
            resolved.IgnoreTextCase,
            resolved.TrimLeadingWhitespace,
            resolved.TrimTrailingWhitespace,
            resolved.IgnoreCols,
            resolved.Type,
            extraInfo);
    }

    private static Dictionary<string, object> CreateExtraDetails(ResolvedTestCase resolved)
    {
        var details = new Dictionary<string, object>
        {
            { "type", resolved.Type },
        };

        if (resolved.Type == "google")
        {
            details.Add("sheet_id", resolved.SheetId);
            details.Add("range", resolved.Range);
        }
        else
        {
            details.Add("separator", resolved.Separator.ToString());
            details.Add("location", resolved.Path);
            details.Add("file_size", new FileInfo(resolved.Path).Length);
        }

        return details;
    }

    private static async Task<Guid> RunBackgroundAsync(Func<SheetProcessing.RunBackroundMangaProcess, Guid?> start, CancellationToken cancellationToken)
    {
        var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        ActiveCancellation.Value = tokenSource.Token;
        var jobDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        Guid? mangaGuid = start(job =>
        {
            Task.Run(() =>
            {
                try
                {
                    using (job)
                    {
                        job.Process(tokenSource.Token);
                    }
                }
                finally
                {
                    jobDone.TrySetResult(true);
                }
            }, tokenSource.Token);
        });

        if (!mangaGuid.HasValue)
        {
            throw new InvalidOperationException("MatrixEase did not return a work item identifier.");
        }

        await jobDone.Task;
        return mangaGuid.Value;
    }

    private static void PerformanceLogger(string mangaName, MyPerformance perf)
    {
        PerformanceQueue.Enqueue(perf);
    }
}

public sealed class ResolvedTestCase
{
    public string Name { get; init; }
    public string Type { get; init; }
    public string Path { get; init; }
    public char Separator { get; init; }
    public string SheetId { get; init; }
    public string Range { get; init; }
    public string GoogleClientId { get; init; }
    public string GoogleClientSecret { get; init; }
    public int HeaderRow { get; init; }
    public int HeaderRows { get; init; }
    public int MaxRows { get; init; }
    public bool IgnoreBlankRows { get; init; }
    public bool IgnoreTextCase { get; init; }
    public bool TrimLeadingWhitespace { get; init; }
    public bool TrimTrailingWhitespace { get; init; }
    public string IgnoreCols { get; init; }

    public static ResolvedTestCase Resolve(MatrixTestDefaults defaults, MatrixTestCase test, int? maxRowsOverride)
    {
        if (string.IsNullOrWhiteSpace(test.Name))
        {
            throw new CliUsageException("Every test case must have a name.");
        }

        if (string.IsNullOrWhiteSpace(test.Type))
        {
            throw new CliUsageException($"Test '{test.Name}' is missing its type.");
        }

        return new ResolvedTestCase
        {
            Name = test.Name,
            Type = test.Type.ToLowerInvariant(),
            Path = test.Path,
            Separator = test.Separator ?? ',',
            SheetId = test.SheetId,
            Range = test.Range,
            GoogleClientId = test.GoogleClientId,
            GoogleClientSecret = test.GoogleClientSecret,
            HeaderRow = test.HeaderRow ?? defaults.HeaderRow,
            HeaderRows = test.HeaderRows ?? defaults.HeaderRows,
            MaxRows = maxRowsOverride ?? test.MaxRows ?? 0,
            IgnoreBlankRows = test.IgnoreBlankRows ?? defaults.IgnoreBlankRows,
            IgnoreTextCase = test.IgnoreTextCase ?? defaults.IgnoreTextCase,
            TrimLeadingWhitespace = test.TrimLeadingWhitespace ?? defaults.TrimLeadingWhitespace,
            TrimTrailingWhitespace = test.TrimTrailingWhitespace ?? defaults.TrimTrailingWhitespace,
            IgnoreCols = test.IgnoreCols,
        };
    }
}
