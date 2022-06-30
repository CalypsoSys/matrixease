using Desktop.MatrixEase.Manga.Common;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Desktop.MatrixEase.Manga.Controllers
{
    [ApiController]
    [Route("api/matrixease")]
    public class MatrxiEaseController : ControllerBase
    {
        private readonly ILogger<MatrxiEaseController> _logger;

        public MatrxiEaseController(ILogger<MatrxiEaseController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public object Get(string matrixease_id, string mxes_id)
        {
            try 
            { 
                string mangaName;
                var manga = MangaState.LoadManga(MangaDesktop.MatrixId(mxes_id), true, -1, new MangaLoadOptions(true), out mangaName);

                return new { MangaName = mangaName, MangaData = manga.ReturnMatrixEase() };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Getting MatrixEase {0} {1}", matrixease_id, mxes_id);
                throw;
            }
        }

        [HttpGet("manga_status")]
        public object MangaStatus(string matrixease_id, string status_key)
        {
            try 
            { 
                return MangaFactory.GetStatus(MangaDesktop.UserId, MangaDesktop.MatrixId(status_key).Item2);
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase Status {0} {1}", matrixease_id, status_key);
            }
            return new { Success = false };
        }

        [HttpGet("manga_pickup_status")]
        public object MangaPickup(string matrixease_id, string mxes_id, string pickup_key)
        {
            try 
            { 
                return BackgroundAction.GetPickupJob(MangaDesktop.MatrixId(mxes_id), Guid.Parse(pickup_key));
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase Pickup {0} {1}", matrixease_id, mxes_id);
            }

            return new { Success = false };
        }

        [HttpGet("filter")]
        public object Filter(string matrixease_id, string mxes_id, string selection_expression)
        {
            try 
            { 
                var filterJob = new BackgroundFilter(MangaDesktop.MatrixId(mxes_id), selection_expression);
                MangaDesktop.RunBackroundManagGet(filterJob);
                return new { Success = true, PickupKey = filterJob.PickupKey, StatusData = MangaFactory.StartingStatus("MatrixEase Filter") };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase Filter {0} {1} {2}", matrixease_id, mxes_id, selection_expression);
            }

            return new { Success = false };
        }

        [HttpGet("update_settings")]
        public object UpdateSettings(string matrixease_id, string mxes_id, bool show_low_equal, int show_low_bound, bool show_high_equal, int show_high_bound, string select_operation, string show_percentage, bool col_ascending, string hide_columns)
        {
            try 
            { 
                bool[] hideColumns = null;
                if (string.IsNullOrWhiteSpace(hide_columns) == false)
                {
                    hideColumns = hide_columns.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => bool.Parse(h)).ToArray();
                }
                bool status = MangaState.SaveMangaSettings(MangaDesktop.MatrixId(mxes_id), show_low_equal, show_low_bound, show_high_equal, show_high_bound, select_operation, show_percentage, col_ascending, hideColumns);

                return new { Success = status };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase update {0} {1}", matrixease_id, mxes_id);
            }

            return new { Success = false };
        }

        [HttpGet("bucketize")]
        public object Bucketize(string matrixease_id, string mxes_id, string column_name, int column_index, bool bucketized, int bucket_size, decimal bucket_mod)
        {
            try
            { 
                var bucketJob = new BackgroundBucketize(MangaDesktop.MatrixId(mxes_id), column_name, column_index, bucketized, bucket_size, bucket_mod);
                MangaDesktop.RunBackroundManagGet(bucketJob);
                return new { Success = true, PickupKey = bucketJob.PickupKey, StatusData = MangaFactory.StartingStatus("MatrixEase Bucket") };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase bucket {0} {1} {2} {3} {4} {5} {6}", matrixease_id, mxes_id, column_name, column_index, bucketized, bucket_size, bucket_mod);
            }

            return new { Success = false };
        }

        [HttpGet("delete_manga")]
        public object DeleteManga(string matrixease_id, string mxes_id)
        {
            try
            {
                return new { Success = MangaState.DeleteManga(MangaDesktop.MatrixId(mxes_id)) };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase delete {0} {1}", matrixease_id, mxes_id);
            }

            return new { Success = false };
        }

        [HttpGet("export_csv")]
        public async Task ExportSelectedMangaData(string matrixease_id, string mxes_id)
        {
            try
            { 
                this.Response.StatusCode = 200;
                this.Response.Headers.Add(HeaderNames.ContentDisposition, "attachment; filename=\"mxes_manga.csv\"");
                this.Response.Headers.Add(HeaderNames.ContentType, "application/octet-stream");
                var outputStream = this.Response.Body;

                var manga = MangaState.LoadManga(MangaDesktop.MatrixId(mxes_id), true, -1, new MangaLoadOptions(false));
                int rowIndex = 0;
                foreach (var row in manga.StreamCSV())
                {
                    var data = Encoding.ASCII.GetBytes(row);
                    await outputStream.WriteAsync(data, 0, data.Length);

                    if ((rowIndex % 1000) == 0)
                    {
                        await outputStream.FlushAsync();
                    }

                    ++rowIndex;
                }
                await outputStream.FlushAsync();
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase export {0} {1}", matrixease_id, mxes_id);
            }
        }

        [HttpGet("detailed_col_stats")]
        public object DetailedColumnStats(string matrixease_id, string mxes_id, string column_name, int column_index)
        {
            try
            {
                var manga = MangaState.LoadManga(MangaDesktop.MatrixId(mxes_id), true, column_index, new MangaLoadOptions(false) { InlcudeCols = new int[] { column_index } });

                return new { Success = true, ColStats = manga.ReturnColStats(column_index) };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase col stats {0} {1} {2} {3}", matrixease_id, mxes_id, column_name, column_index);
            }

            return new { Success = false };
        }

        [HttpGet("get_col_measures")]
        public object GetColumnMeasures(string matrixease_id, string mxes_id, int col_index, string selected_node, string col_measure_indexes, bool filtered)
        {
            try
            {
                var colMeasures = col_measure_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray();
                var manga = MangaState.LoadManga(MangaDesktop.MatrixId(mxes_id), filtered, -1, new MangaLoadOptions(false) { InlcudeCols = colMeasures.Append(col_index).ToArray() });

                return new { Success = true, MeasureStats = manga.GetMeasureStats(selected_node, colMeasures) };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase col measures {0} {1} {2} {3}", matrixease_id, mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }

        [HttpGet("get_chart_data")]
        public object GetChartData(string matrixease_id, string mxes_id, string chart_type, string col_dimension_indexes, string col_measure_tot_indexes, string col_measure_avg_indexes, string col_measure_cnt_indexes, bool filtered)
        {
            try
            {
                var colDimensions = col_dimension_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray();
                var colTotMeasures = col_measure_tot_indexes != null ? col_measure_tot_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray() : new int[0];
                var colAvgMeasures = col_measure_avg_indexes != null ? col_measure_avg_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray() : new int[0];
                var colCntMeasures = col_measure_cnt_indexes != null ? col_measure_cnt_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray() : new int[0];

                var inclcudeCols = colDimensions.Union(colTotMeasures).Union(colAvgMeasures).Union(colCntMeasures).Distinct().ToArray();
                var manga = MangaState.LoadManga(MangaDesktop.MatrixId(mxes_id), filtered, -1, new MangaLoadOptions(false) { InlcudeCols = inclcudeCols });

                if (chart_type == "report")
                {
                    return new { Success = true, ReportData = manga.GetReportData(colDimensions, colTotMeasures, colAvgMeasures, colCntMeasures, filtered) };
                }
                else
                {
                    return new { Success = true, ChartData = manga.GetChartData(colDimensions, colTotMeasures, colAvgMeasures, colCntMeasures, filtered) };
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase chart data {0} {1} {2} {3}", matrixease_id, mxes_id, chart_type, col_dimension_indexes);
            }

            return new { Success = false };
        }

        [HttpGet("get_node_rows")]
        public object GetNodeRows(string matrixease_id, string mxes_id, int col_index, string selected_node, bool filtered)
        {
            try
            {
                var manga = MangaState.LoadManga(MangaDesktop.MatrixId(mxes_id), filtered, -1, new MangaLoadOptions(false));
                return new { Success = true, ReportData = manga.GetNodeData(selected_node) };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase node rows {0} {1} {2} {3}", matrixease_id, mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }

        [HttpGet("get_duplicate_entries")]
        public object GetDuplicateEntries(string matrixease_id, string mxes_id, int col_index, string selected_node, bool filtered)
        {
            try
            {
                var manga = MangaState.LoadManga(MangaDesktop.MatrixId(mxes_id), filtered, -1, new MangaLoadOptions(false));
                return new { Success = true, DuplicateEntries = manga.GetDuplicateEntries(selected_node) };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase dup nodes {0} {1} {2} {3}", matrixease_id, mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }

        [HttpGet("get_dependency_diagram")]
        public object GetDependencyDiagram(string matrixease_id, string mxes_id, int col_index, string selected_node, bool filtered)
        {
            try
            {
                var manga = MangaState.LoadManga(MangaDesktop.MatrixId(mxes_id), filtered, -1, new MangaLoadOptions(false));
                return new { Success = true, DependencyDiagram = manga.GetDependencyDiagram(selected_node) };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase dep diagram {0} {1} {2} {3}", matrixease_id, mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }
    }
}
