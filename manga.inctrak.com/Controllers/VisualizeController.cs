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
using manga.inctrak.com.Controllers;
using manga.inctrak.com.Tasks;
using Manga.IncTrak.Manga;
using Manga.IncTrak.Processing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace manga.inctrak.com
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisualizeController : ProcessController
    {
        private readonly ILogger<VisualizeController> _logger;

        public VisualizeController(ILogger<VisualizeController> logger, IBackgroundTaskQueue queue) : base(queue)
        {
            _logger = logger;
        }

        [HttpGet]
        public object Get(string inctrak_id, string vis_id)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                string mangaName;
                var manga = MangaState.LoadManga(visId, true, -1, new MangaLoadOptions(true), out mangaName);

                return new { MangaName = mangaName,  MangaData = manga.ReturnVis() };
            }

            return null;
        }

        [HttpGet("manga_status")]
        public object MangaStatus(string inctrak_id, string status_key)
        {
            CheckIncTrakId(inctrak_id, true);

            var myIds = GetMyIdentities(true);
            MangaAuthType auth = ValidateAccess(null, myIds, true);
            if (auth != MangaAuthType.Invalid)
            {
                string userId = MyIdentity(myIds, auth);
                var visId = Decrypt(status_key);

                if (visId.Item1 == userId)
                {
                    return MangaFactory.GetStatus(userId, visId.Item2);
                }
            }

            return new { Success = false };
        }

        [HttpGet("manga_pickup_status")]
        public object MangaPickup(string inctrak_id, string vis_id, string pickup_key)
        {
            CheckIncTrakId(inctrak_id, true);

            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                return BackgroundAction.GetPickupJob(visId, Guid.Parse(pickup_key));
            }

            return new { Success = false };
        }

        [HttpGet("filter")]
        public object Filter(string inctrak_id, string vis_id, string selection_expression)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                var filterJob = new BackgroundFilter(visId, selection_expression);
                RunBackroundManagGet(filterJob);
                /*
                DataManga manga = MangaState.LoadManga(visId, false, -1, false);
                if (string.IsNullOrWhiteSpace(selection_expression) == false)
                {
                    ExpressionCtl exprCtl = new ExpressionCtl(selection_expression);
                    MyBitArray bitmap = exprCtl.ProcessBitmaps(manga);

                    if (bitmap != null)
                    {
                        MangaFilter mangaFilter = MangaState.SaveMangaFilter(visId, selection_expression, bitmap);
                        manga.ApplyFilter(mangaFilter, true, -1);
                    }
                }
                else
                {
                    MangaFilter mangafilter = MangaState.SaveMangaFilter(visId, "", null);
                    manga.ApplyFilter(mangafilter, false, -1);
                }

                return manga.ReturnVis();
                */
                return new { Success = true, PickupKey = filterJob.PickupKey, StatusData = MangaFactory.StartingStatus("Manga Filter") };
            }

            return new { Success = false };
        }

        [HttpGet("update_settings")]
        public object UpdateSettings(string inctrak_id, string vis_id, bool show_low_equal, int show_low_bound, bool show_high_equal, int show_high_bound, string select_operation, string show_percentage, bool col_ascending, string hide_columns)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                bool[] hideColumns = null;
                if (string.IsNullOrWhiteSpace(hide_columns) == false)
                {
                    hideColumns = hide_columns.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => bool.Parse(h)).ToArray();
                }
                bool status = MangaState.SaveMangaSettings(visId, show_low_equal, show_low_bound, show_high_equal, show_high_bound, select_operation, show_percentage, col_ascending, hideColumns);

                return new { Success = status };
            }

            return new { Success = false };
        }

        [HttpGet("bucketize")]
        public object Bucketize(string inctrak_id, string vis_id, string column_name, int column_index, bool bucketized, int bucket_size, decimal bucket_mod)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                var bucketJob = new BackgroundBucketize(visId, column_name, column_index, bucketized, bucket_size, bucket_mod);
                RunBackroundManagGet(bucketJob);
                /*
                var manga = MangaState.LoadManga(visId, true, column_index, false);

                ColumnDefBucket column = manga.ReBucketize(column_index, bucketized, bucket_size);
                MangaState.SaveMangaBuckets(visId, column_index, column);
                if (column != null)
                {
                    manga.ApplyFilterToBucket(column);
                }

                return manga.ReturnVis();
                */

                return new { Success = true,  PickupKey = bucketJob.PickupKey, StatusData = MangaFactory.StartingStatus("Manga Bucket") };
            }

            return new { Success = false };
        }

        [HttpGet("delete_manga")]
        public object DeleteManga(string inctrak_id, string vis_id)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                bool status = false;
                try
                {
                    status = MangaState.DeleteManga(visId);
                }
                catch
                {

                }
                return new { Success = status };
            }

            return null;
        }

        [HttpGet("export_csv")]
        public async Task ExportSelectedMangaData(string inctrak_id, string vis_id)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                this.Response.StatusCode = 200;
                this.Response.Headers.Add(HeaderNames.ContentDisposition, "attachment; filename=\"vis_manga.csv\"");
                this.Response.Headers.Add(HeaderNames.ContentType, "application/octet-stream");
                var outputStream = this.Response.Body;

                var manga = MangaState.LoadManga(visId, true, -1, new MangaLoadOptions(false));
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

        [HttpGet("detailed_col_stats")]
        public object DetailedColumnStats(string inctrak_id, string vis_id, string column_name, int column_index)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                var manga = MangaState.LoadManga(visId, true, column_index, new MangaLoadOptions(false) { InlcudeCols = new int[] { column_index } } );

                return new { Success = true, ColStats = manga.ReturnColStats(column_index) };
            }

            return new { Success = false };
        }

        [HttpGet("get_col_measures")]
        public object GetColumnMeasures(string inctrak_id, string vis_id, int col_index, string selected_node, string col_measure_indexes, bool filtered)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                var colMeasures = col_measure_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray();
                var manga = MangaState.LoadManga(visId, filtered, -1, new MangaLoadOptions(false) { InlcudeCols = colMeasures.Append(col_index).ToArray() });

                return new { Success = true, MeasureStats = manga.GetMeasureStats(selected_node, colMeasures) };
            }

            return new { Success = false };
        }

        [HttpGet("get_chart_data")]
        public object GetChartData(string inctrak_id, string vis_id, string chart_type, string col_dimension_indexes, string col_measure_tot_indexes, string col_measure_avg_indexes, string col_measure_cnt_indexes, bool filtered)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                var colDimensions = col_dimension_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray();
                var colTotMeasures = col_measure_tot_indexes != null ? col_measure_tot_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray() : new int[0];
                var colAvgMeasures = col_measure_avg_indexes != null ? col_measure_avg_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray() : new int[0];
                var colCntMeasures = col_measure_cnt_indexes != null ? col_measure_cnt_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray() : new int[0];
                
                var inclcudeCols = colDimensions.Union(colTotMeasures).Union(colAvgMeasures).Union(colCntMeasures).Distinct().ToArray();
                var manga = MangaState.LoadManga(visId, filtered, -1, new MangaLoadOptions(false) { InlcudeCols = inclcudeCols });

                if (chart_type == "report")
                {
                    return new { Success = true, ReportData = manga.GetReportData(colDimensions, colTotMeasures, colAvgMeasures, colCntMeasures, filtered) };
                }
                else
                {
                    return new { Success = true, ChartData = manga.GetChartData(colDimensions, colTotMeasures, colAvgMeasures, colCntMeasures, filtered) };
                }
            }

            return new { Success = false };
        }

        
        [HttpGet("get_node_rows")]
        public object GetNodeRows(string inctrak_id, string vis_id, int col_index, string selected_node, bool filtered)
        {
            CheckIncTrakId(inctrak_id, true);
            var visId = Decrypt(vis_id);
            if (ValidateAccess(visId, null, true) != MangaAuthType.Invalid)
            {
                var manga = MangaState.LoadManga(visId, filtered, -1, new MangaLoadOptions(false) );
                return new { Success = true, ReportData = manga.GetNodeData(selected_node) };
            }

            return new { Success = false };
        }
    }
}
