using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using MatrixEase.Web.Controllers;
using MatrixEase.Web.Tasks;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using MatrixEase.Manga.Utility;

namespace MatrixEase.Web
{
    [ApiController]
    [Route("api/[controller]")]
    public class MatrixEaseController : ProcessController
    {
        private readonly ILogger<MatrixEaseController> _logger;

        public MatrixEaseController(ILogger<MatrixEaseController> logger, IBackgroundTaskQueue queue) : base(queue)
        {
            _logger = logger;
        }

        [HttpGet]
        public object Get(string matrixease_id, string mxes_id)
        {
            try 
            { 
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    string mangaName;
                    var manga = MangaState.LoadManga(mxesId, true, -1, new MangaLoadOptions(true), out mangaName);

                    return new { MangaName = mangaName,  MangaData = manga.ReturnMatrixEase() };
                }

                return null;
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "Getting MatrixEase {0} {1}", matrixease_id, mxes_id);
                throw;
            }
        }

        [HttpGet("manga_status")]
        public object MangaStatus(string matrixease_id, string status_key)
        {
            try
            { 
                CheckMatrixEaseId(matrixease_id, true);

                var myIds = GetMyIdentities(true);
                MangaAuthType auth = ValidateAccess(null, myIds, true);
                if (auth != MangaAuthType.Invalid)
                {
                    string userId = MyIdentity(myIds, auth);
                    var mxesId = Decrypt(status_key);

                    if (mxesId.Item1 == userId)
                    {
                        return MangaFactory.GetStatus(userId, mxesId.Item2);
                    }
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase Status {0} {1}", matrixease_id, status_key);
            }

            return new { Success = false };
        }

        [HttpGet("manga_pickup_status")]
        public object MangaPickup(string matrixease_id, string mxes_id, string pickup_key)
        {
            try
            { 
                CheckMatrixEaseId(matrixease_id, true);

                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    return BackgroundAction.GetPickupJob(mxesId, Guid.Parse(pickup_key));
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase Pickup {0} {1}", matrixease_id, mxes_id);
            }

            return new { Success = false };
        }

        [HttpGet("filter")]
        public object Filter(string matrixease_id, string mxes_id, string selection_expression)
        {
            try 
            { 
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    var filterJob = new BackgroundFilter(mxesId, selection_expression);
                    RunBackroundManagGet(filterJob);
                    /*
                    DataManga manga = MangaState.LoadManga(mxesId, false, -1, false);
                    if (string.IsNullOrWhiteSpace(selection_expression) == false)
                    {
                        ExpressionCtl exprCtl = new ExpressionCtl(selection_expression);
                        MyBitArray bitmap = exprCtl.ProcessBitmaps(manga);

                        if (bitmap != null)
                        {
                            MangaFilter mangaFilter = MangaState.SaveMangaFilter(mxesId, selection_expression, bitmap);
                            manga.ApplyFilter(mangaFilter, true, -1);
                        }
                    }
                    else
                    {
                        MangaFilter mangafilter = MangaState.SaveMangaFilter(mxesId, "", null);
                        manga.ApplyFilter(mangafilter, false, -1);
                    }

                    return manga.ReturnMatrixEase();
                    */
                    return new { Success = true, PickupKey = filterJob.PickupKey, StatusData = MangaFactory.StartingStatus("MatrixEase Filter") };
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase Filter {0} {1} {2}", matrixease_id, mxes_id, selection_expression);
            }

            return new { Success = false };
        }

        [HttpGet("update_settings")]
        public object UpdateSettings(string matrixease_id, string mxes_id, bool show_low_equal, int show_low_bound, bool show_high_equal, int show_high_bound, string select_operation, string show_percentage, bool col_ascending, string hide_columns)
        {
            try
            { 
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    bool[] hideColumns = null;
                    if (string.IsNullOrWhiteSpace(hide_columns) == false)
                    {
                        hideColumns = hide_columns.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => bool.Parse(h)).ToArray();
                    }
                    bool status = MangaState.SaveMangaSettings(mxesId, show_low_equal, show_low_bound, show_high_equal, show_high_bound, select_operation, show_percentage, col_ascending, hideColumns);

                    return new { Success = status };
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase update {0} {1}", matrixease_id, mxes_id);
            }

            return new { Success = false };
        }

        [HttpGet("bucketize")]
        public object Bucketize(string matrixease_id, string mxes_id, string column_name, int column_index, bool bucketized, int bucket_size, decimal bucket_mod)
        {
            try 
            { 
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    var bucketJob = new BackgroundBucketize(mxesId, column_name, column_index, bucketized, bucket_size, bucket_mod);
                    RunBackroundManagGet(bucketJob);
                    /*
                    var manga = MangaState.LoadManga(mxesId, true, column_index, false);

                    ColumnDefBucket column = manga.ReBucketize(column_index, bucketized, bucket_size);
                    MangaState.SaveMangaBuckets(mxesId, column_index, column);
                    if (column != null)
                    {
                        manga.ApplyFilterToBucket(column);
                    }

                    return manga.ReturnMatrixEase();
                    */

                    return new { Success = true,  PickupKey = bucketJob.PickupKey, StatusData = MangaFactory.StartingStatus("MatrixEase Bucket") };
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase bucket {0} {1} {2} {3} {4} {5} {6}", matrixease_id, mxes_id, column_name, column_index, bucketized, bucket_size, bucket_mod);
            }

            return new { Success = false };
        }

        [HttpGet("delete_manga")]
        public object DeleteManga(string matrixease_id, string mxes_id)
        {
            try 
            { 
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    return new { Success = MangaState.DeleteManga(mxesId) };
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase delete {0} {1}", matrixease_id, mxes_id);
            }

            return new { Success = false };
        }

        [HttpGet("export_csv")]
        public async Task ExportSelectedMangaData(string matrixease_id, string mxes_id)
        {
            try
            { 
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    this.Response.StatusCode = 200;
                    this.Response.Headers.Add(HeaderNames.ContentDisposition, "attachment; filename=\"mxes_manga.csv\"");
                    this.Response.Headers.Add(HeaderNames.ContentType, "application/octet-stream");
                    var outputStream = this.Response.Body;

                    var manga = MangaState.LoadManga(mxesId, true, -1, new MangaLoadOptions(false));
                    int rowIndex = 0;
                    foreach (var row in manga.StreamCSV())
                    {
                        var data = Encoding.ASCII.GetBytes(row);
                        await outputStream.WriteAsync(data, 0, data.Length);

                        if ((rowIndex % 1000) == 0 )
                        {
                            await outputStream.FlushAsync();
                        }

                        ++rowIndex;
                    }
                    await outputStream.FlushAsync();
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase export {0} {1}", matrixease_id, mxes_id);
            }
        }

        [HttpGet("detailed_col_stats")]
        public object DetailedColumnStats(string matrixease_id, string mxes_id, string column_name, int column_index)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    var manga = MangaState.LoadManga(mxesId, true, column_index, new MangaLoadOptions(false) { InlcudeCols = new int[] { column_index } });

                    return new { Success = true, ColStats = manga.ReturnColStats(column_index) };
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase col stats {0} {1} {2} {3}", matrixease_id, mxes_id, column_name, column_index);
            }

            return new { Success = false };
        }

        [HttpGet("get_col_measures")]
        public object GetColumnMeasures(string matrixease_id, string mxes_id, int col_index, string selected_node, string col_measure_indexes, bool filtered)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    var colMeasures = col_measure_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray();
                    var manga = MangaState.LoadManga(mxesId, filtered, -1, new MangaLoadOptions(false) { InlcudeCols = colMeasures.Append(col_index).ToArray() });

                    return new { Success = true, MeasureStats = manga.GetMeasureStats(selected_node, colMeasures) };
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase col measures {0} {1} {2} {3}", matrixease_id, mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }

        [HttpGet("get_chart_data")]
        public object GetChartData(string matrixease_id, string mxes_id, string chart_type, string col_dimension_indexes, string col_measure_tot_indexes, string col_measure_avg_indexes, string col_measure_cnt_indexes, bool filtered)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    var colDimensions = col_dimension_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray();
                    var colTotMeasures = col_measure_tot_indexes != null ? col_measure_tot_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray() : new int[0];
                    var colAvgMeasures = col_measure_avg_indexes != null ? col_measure_avg_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray() : new int[0];
                    var colCntMeasures = col_measure_cnt_indexes != null ? col_measure_cnt_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray() : new int[0];

                    var inclcudeCols = colDimensions.Union(colTotMeasures).Union(colAvgMeasures).Union(colCntMeasures).Distinct().ToArray();
                    var manga = MangaState.LoadManga(mxesId, filtered, -1, new MangaLoadOptions(false) { InlcudeCols = inclcudeCols });

                    if (chart_type == "report")
                    {
                        return new { Success = true, ReportData = manga.GetReportData(colDimensions, colTotMeasures, colAvgMeasures, colCntMeasures, filtered) };
                    }
                    else
                    {
                        return new { Success = true, ChartData = manga.GetChartData(colDimensions, colTotMeasures, colAvgMeasures, colCntMeasures, filtered) };
                    }
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase chart data {0} {1} {2} {3}", matrixease_id, mxes_id, chart_type, col_dimension_indexes);
            }

            return new { Success = false };
        }
        
        [HttpGet("get_node_rows")]
        public object GetNodeRows(string matrixease_id, string mxes_id, int col_index, string selected_node, bool filtered)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    var manga = MangaState.LoadManga(mxesId, filtered, -1, new MangaLoadOptions(false));
                    return new { Success = true, ReportData = manga.GetNodeData(selected_node) };
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase node rows {0} {1} {2} {3}", matrixease_id, mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }

        [HttpGet("get_duplicate_entries")]
        public object GetDuplicateEntries(string matrixease_id, string mxes_id, int col_index, string selected_node, bool filtered)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    var manga = MangaState.LoadManga(mxesId, filtered, -1, new MangaLoadOptions(false));
                    return new { Success = true, DuplicateEntries = manga.GetDuplicateEntries(selected_node) };
                }
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase dup nodes {0} {1} {2} {3}", matrixease_id, mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }
        
        [HttpGet("get_dependency_diagram")]
        public object GetDependencyDiagram(string matrixease_id, string mxes_id, int col_index, string selected_node, bool filtered)
        {
            try
            {
                CheckMatrixEaseId(matrixease_id, true);
                var mxesId = Decrypt(mxes_id);
                if (ValidateAccess(mxesId, null, true) != MangaAuthType.Invalid)
                {
                    var manga = MangaState.LoadManga(mxesId, filtered, -1, new MangaLoadOptions(false));
                    return new { Success = true, DependencyDiagram = manga.GetDependencyDiagram(selected_node) };
                }

                return new { Success = false };
            }
            catch (Exception excp)
            {
                MyLogger.LogError(excp, "MatrixEase dep diagram {0} {1} {2} {3}", matrixease_id, mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }
    }
}
