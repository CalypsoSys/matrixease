using System;
using System.Text.Json.Serialization;

namespace Manga.IncTrak.Processing
{
    public enum MangaFactoryStatusKey
    {
        PreProcess,
        Queued,
        Processing,
        Analyzing,
        Saving,
        Complete,
    }

    public enum MangaFactoryStatusState
    {
        Starting,
        Started,
        Running,
        Complete
    }

    public class MangaFactoryStatus
    {
        public DateTime Started { get; set; }
        public string Elapsed { get; set; }
        public string Desc { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public MangaFactoryStatusState Status { get; set; }

        public MangaFactoryStatus()
        {
        }

        public MangaFactoryStatus(string desc, MangaFactoryStatusState status)
        {
            Started = DateTime.Now;
            Elapsed = "00:00:00";
            Desc = desc;
            Status = status;
        }

        public void Update(string desc, MangaFactoryStatusState status)
        {
            Elapsed = (DateTime.Now - Started).ToString(@"hh\:mm\:ss");
            Desc = desc;
            Status = status;
        }

        public void UpdateElapsed()
        {
            if ( Status != MangaFactoryStatusState.Complete)
            {
                Elapsed = (DateTime.Now - Started).ToString(@"hh\:mm\:ss");
            }
        }
    }
}
