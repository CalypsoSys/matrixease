using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MatrixEase.Manga.Manga;
using MatrixEase.Manga.Processing;
using MatrixEase.Manga.Utility;
using MatrixEase.Web.Common;
using MatrixEase.Web.Controllers;
using MatrixEase.Web.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace MatrixEase.Web
{
    [ApiController]
    [Route("api/matrixease")]
    public class MatrixEaseController : ProcessController
    {
        private readonly ILogger<MatrixEaseController> _logger;
        private readonly RequestContextAccessor _requestContextAccessor;
        private readonly AppSettings _settings;

        public MatrixEaseController(ILogger<MatrixEaseController> logger, IBackgroundTaskQueue queue, RequestContextAccessor requestContextAccessor, IOptions<AppSettings> options) : base(queue)
        {
            _logger = logger;
            _requestContextAccessor = requestContextAccessor;
            _settings = options.Value ?? new AppSettings();
        }

        [HttpGet("projects")]
        public IActionResult Projects()
        {
            SupabaseIdentity identity = _requestContextAccessor.GetSupabaseIdentity(HttpContext);
            if (identity.IsAuthenticated() == false)
            {
                return Unauthorized(new MatrixEaseProjectsResponse
                {
                    Success = false,
                    Message = "You must sign in before loading MatrixEase projects."
                });
            }

            try
            {
                return Ok(BuildProjectsResponse(identity));
            }
            catch (Exception excp)
            {
                string message = MatrixEaseErrors.LogError(_settings, excp, "MatrixEase projects");
                return StatusCode(500, new MatrixEaseProjectsResponse
                {
                    Success = false,
                    Message = message
                });
            }
        }

        private MatrixEaseProjectsResponse BuildProjectsResponse(SupabaseIdentity identity)
        {
            string userId = identity.ExternalIdentity;
            MangaCatalog catalog = MangaState.LoadUserMangaCatalog(userId, new MangaLoadOptions(true));
            var projects = new List<MatrixEaseProjectDto>();
            var loadedProjectIds = new HashSet<Guid>();

            foreach (MangaInfo manga in catalog.MyMangas.OrderByDescending(manga => manga.Created))
            {
                projects.Add(BuildProjectDto(userId, manga, false));
                loadedProjectIds.Add(manga.ManagGuid);
            }

            foreach (MangaInfo manga in MangaFactory.GetPending(userId).OrderByDescending(manga => manga.Created))
            {
                if (loadedProjectIds.Contains(manga.ManagGuid))
                {
                    continue;
                }

                projects.Add(BuildProjectDto(userId, manga, true));
            }

            return new MatrixEaseProjectsResponse
            {
                Success = true,
                Projects = projects
            };
        }

        private MatrixEaseProjectDto BuildProjectDto(string userId, MangaInfo manga, bool isPending)
        {
            return new MatrixEaseProjectDto
            {
                ProjectId = Encode(userId, manga.ManagGuid),
                Name = manga.MangaName,
                OriginalName = manga.OriginalName,
                SheetType = manga.SheetType,
                Created = manga.Created,
                MaxRows = manga.MaxRows,
                TotalRows = isPending ? null : manga.TotalRows,
                Status = manga.Status,
                IsPending = isPending
            };
        }

        [HttpGet]
        public object Get(string mxes_id)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId) == false)
                {
                    return UnauthorizedProject();
                }

                string mangaName;
                var manga = MangaState.LoadManga(mxesId, true, -1, new MangaLoadOptions(true), out mangaName);

                return new { MangaName = mangaName, MangaData = manga.ReturnMatrixEase() };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Getting MatrixEase {0}", mxes_id);
                throw;
            }
        }

        [HttpGet("manga_status")]
        public object MangaStatus(string status_key)
        {
            try
            {
                if (TryAuthorizeProject(status_key, out var mxesId))
                {
                    return MangaFactory.GetStatus(mxesId.Item1, mxesId.Item2);
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase Status {0}", status_key);
            }

            return new { Success = false };
        }

        [HttpGet("manga_pickup_status")]
        public object MangaPickup(string mxes_id, string pickup_key)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
                {
                    return BackgroundAction.GetPickupJob(mxesId, Guid.Parse(pickup_key));
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase Pickup {0}", mxes_id);
            }

            return new { Success = false };
        }

        [HttpGet("filter")]
        public object Filter(string mxes_id, string selection_expression)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
                {
                    var filterJob = new BackgroundFilter(mxesId, selection_expression);
                    RunBackroundManagGet(filterJob);
                    return new { Success = true, PickupKey = filterJob.PickupKey, StatusData = MangaFactory.StartingStatus("MatrixEase Filter") };
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase Filter {0} {1}", mxes_id, selection_expression);
            }

            return new { Success = false };
        }

        [HttpGet("update_settings")]
        public object UpdateSettings(string mxes_id, bool show_low_equal, int show_low_bound, bool show_high_equal, int show_high_bound, string select_operation, string show_percentage, bool col_ascending, string hide_columns)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
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
                SimpleLogger.LogError(excp, "MatrixEase update {0}", mxes_id);
            }

            return new { Success = false };
        }

        [HttpGet("bucketize")]
        public object Bucketize(string mxes_id, string column_name, int column_index, bool bucketized, int bucket_size, decimal bucket_mod)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
                {
                    var bucketJob = new BackgroundBucketize(mxesId, column_name, column_index, bucketized, bucket_size, bucket_mod);
                    RunBackroundManagGet(bucketJob);

                    return new { Success = true, PickupKey = bucketJob.PickupKey, StatusData = MangaFactory.StartingStatus("MatrixEase Bucket") };
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase bucket {0} {1} {2} {3} {4} {5}", mxes_id, column_name, column_index, bucketized, bucket_size, bucket_mod);
            }

            return new { Success = false };
        }

        [HttpGet("delete_manga")]
        public object DeleteManga(string mxes_id)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
                {
                    return new { Success = MangaState.DeleteManga(mxesId) };
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase delete {0}", mxes_id);
            }

            return new { Success = false };
        }

        [HttpGet("export_csv")]
        public async Task ExportSelectedMangaData(string mxes_id)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId) == false)
                {
                    Response.StatusCode = 401;
                    return;
                }

                this.Response.StatusCode = 200;
                this.Response.Headers[HeaderNames.ContentDisposition] = "attachment; filename=\"mxes_manga.csv\"";
                this.Response.ContentType = "application/octet-stream";
                var outputStream = this.Response.Body;

                var manga = MangaState.LoadManga(mxesId, true, -1, new MangaLoadOptions(false));
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
                SimpleLogger.LogError(excp, "MatrixEase export {0}", mxes_id);
            }
        }

        [HttpGet("detailed_col_stats")]
        public object DetailedColumnStats(string mxes_id, string column_name, int column_index)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
                {
                    var manga = MangaState.LoadManga(mxesId, true, column_index, new MangaLoadOptions(false) { InlcudeCols = new int[] { column_index } });

                    return new { Success = true, ColStats = manga.ReturnColStats(column_index) };
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase col stats {0} {1} {2}", mxes_id, column_name, column_index);
            }

            return new { Success = false };
        }

        [HttpGet("get_col_measures")]
        public object GetColumnMeasures(string mxes_id, int col_index, string selected_node, string col_measure_indexes, bool filtered)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
                {
                    var colMeasures = col_measure_indexes.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(h => int.Parse(h)).ToArray();
                    var manga = MangaState.LoadManga(mxesId, filtered, -1, new MangaLoadOptions(false) { InlcudeCols = colMeasures.Append(col_index).ToArray() });

                    return new { Success = true, MeasureStats = manga.GetMeasureStats(selected_node, colMeasures) };
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase col measures {0} {1} {2}", mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }

        [HttpGet("get_chart_data")]
        public object GetChartData(string mxes_id, string chart_type, string col_dimension_indexes, string col_measure_tot_indexes, string col_measure_avg_indexes, string col_measure_cnt_indexes, bool filtered)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
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
                SimpleLogger.LogError(excp, "MatrixEase chart data {0} {1} {2}", mxes_id, chart_type, col_dimension_indexes);
            }

            return new { Success = false };
        }

        [HttpGet("get_node_rows")]
        public object GetNodeRows(string mxes_id, int col_index, string selected_node, bool filtered)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
                {
                    var manga = MangaState.LoadManga(mxesId, filtered, -1, new MangaLoadOptions(false));
                    return new { Success = true, ReportData = manga.GetNodeData(selected_node) };
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase node rows {0} {1} {2}", mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }

        [HttpGet("get_duplicate_entries")]
        public object GetDuplicateEntries(string mxes_id, int col_index, string selected_node, bool filtered)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
                {
                    var manga = MangaState.LoadManga(mxesId, filtered, -1, new MangaLoadOptions(false));
                    return new { Success = true, DuplicateEntries = manga.GetDuplicateEntries(selected_node) };
                }
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase dup nodes {0} {1} {2}", mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }

        [HttpGet("get_dependency_diagram")]
        public object GetDependencyDiagram(string mxes_id, int col_index, string selected_node, bool filtered)
        {
            try
            {
                if (TryAuthorizeProject(mxes_id, out var mxesId))
                {
                    var manga = MangaState.LoadManga(mxesId, filtered, -1, new MangaLoadOptions(false));
                    return new { Success = true, DependencyDiagram = manga.GetDependencyDiagram(selected_node) };
                }

                return new { Success = false };
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "MatrixEase dep diagram {0} {1} {2}", mxes_id, col_index, selected_node);
            }

            return new { Success = false };
        }

        private bool TryAuthorizeProject(string mxesId, out Tuple<string, Guid> decodedProjectId)
        {
            decodedProjectId = null;
            SupabaseIdentity identity = _requestContextAccessor.GetSupabaseIdentity(HttpContext);
            if (identity.IsAuthenticated() == false || string.IsNullOrWhiteSpace(mxesId))
            {
                return false;
            }

            try
            {
                decodedProjectId = Decrypt(mxesId);
            }
            catch (Exception excp)
            {
                SimpleLogger.LogError(excp, "Error decoding MatrixEase project id");
                return false;
            }

            if (string.Equals(decodedProjectId.Item1, identity.ExternalIdentity, StringComparison.Ordinal) == false)
            {
                decodedProjectId = null;
                return false;
            }

            return true;
        }

        private UnauthorizedObjectResult UnauthorizedProject()
        {
            return Unauthorized(new
            {
                Success = false,
                Message = "You must sign in with access to this MatrixEase project."
            });
        }
    }
}
