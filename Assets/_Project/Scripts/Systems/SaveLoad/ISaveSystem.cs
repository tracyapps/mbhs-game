using System.Collections.Generic;
using System.Threading.Tasks;
using MBHS.Data.Models;

namespace MBHS.Systems.SaveLoad
{
    public interface ISaveSystem
    {
        bool HasSaveData { get; }

        Task SaveAsync();
        Task LoadAsync();

        // Band roster
        Task SaveBandRoster(BandRosterData roster);
        Task<BandRosterData> LoadBandRoster();

        // Drill charts
        Task SaveDrillChart(DrillChart chart);
        Task<DrillChart> LoadDrillChart(string chartId);
        Task<List<DrillChartSummary>> ListDrillCharts();
        Task DeleteDrillChart(string chartId);

        // Player progress
        Task SavePlayerProgress(PlayerProgress progress);
        Task<PlayerProgress> LoadPlayerProgress();
    }

    public class DrillChartSummary
    {
        public string Id;
        public string Name;
        public string SongId;
        public int FormationCount;
        public string LastModified;
    }
}
