using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.SaveLoad
{
    public class LocalSaveSystem : ISaveSystem
    {
        private const string RosterFileName = "roster.json";
        private const string ProgressFileName = "progress.json";
        private const string ChartsDirName = "charts";
        private const string TemplatesDirName = "templates";

        private string _savePath;
        private string _chartsPath;
        private string _templatesPath;

        private BandRosterData _cachedRoster;
        private PlayerProgress _cachedProgress;

        public bool HasSaveData { get; private set; }

        public LocalSaveSystem()
        {
            _savePath = Path.Combine(Application.persistentDataPath, "saves");
            _chartsPath = Path.Combine(_savePath, ChartsDirName);
            _templatesPath = Path.Combine(_savePath, TemplatesDirName);
        }

        public Task LoadAsync()
        {
            EnsureDirectoriesExist();
            HasSaveData = File.Exists(Path.Combine(_savePath, ProgressFileName));
            Debug.Log($"LocalSaveSystem: Save path = {_savePath}, has data = {HasSaveData}");
            return Task.CompletedTask;
        }

        public Task SaveAsync()
        {
            if (_cachedRoster != null)
                SaveBandRoster(_cachedRoster);
            if (_cachedProgress != null)
                SavePlayerProgress(_cachedProgress);
            return Task.CompletedTask;
        }

        // Band Roster
        public Task SaveBandRoster(BandRosterData roster)
        {
            _cachedRoster = roster;
            var json = JsonUtility.ToJson(roster, true);
            WriteFile(Path.Combine(_savePath, RosterFileName), json);
            return Task.CompletedTask;
        }

        public Task<BandRosterData> LoadBandRoster()
        {
            if (_cachedRoster != null)
                return Task.FromResult(_cachedRoster);

            var path = Path.Combine(_savePath, RosterFileName);
            if (!File.Exists(path))
                return Task.FromResult<BandRosterData>(null);

            var json = File.ReadAllText(path);
            _cachedRoster = JsonUtility.FromJson<BandRosterData>(json);
            return Task.FromResult(_cachedRoster);
        }

        // Drill Charts
        public Task SaveDrillChart(DrillChart chart)
        {
            EnsureDirectoriesExist();

            if (string.IsNullOrEmpty(chart.Id))
                chart.Id = Guid.NewGuid().ToString();

            chart.LastModifiedDate = DateTime.UtcNow.ToString("o");

            if (string.IsNullOrEmpty(chart.CreatedDate))
                chart.CreatedDate = chart.LastModifiedDate;

            var json = JsonUtility.ToJson(chart, true);
            var fileName = $"chart_{chart.Id}.json";
            WriteFile(Path.Combine(_chartsPath, fileName), json);
            return Task.CompletedTask;
        }

        public Task<DrillChart> LoadDrillChart(string chartId)
        {
            var fileName = $"chart_{chartId}.json";
            var path = Path.Combine(_chartsPath, fileName);

            if (!File.Exists(path))
            {
                Debug.LogWarning($"LocalSaveSystem: Chart not found: {chartId}");
                return Task.FromResult<DrillChart>(null);
            }

            var json = File.ReadAllText(path);
            var chart = JsonUtility.FromJson<DrillChart>(json);
            return Task.FromResult(chart);
        }

        public Task<List<DrillChartSummary>> ListDrillCharts()
        {
            var summaries = new List<DrillChartSummary>();
            EnsureDirectoriesExist();

            var files = Directory.GetFiles(_chartsPath, "chart_*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var chart = JsonUtility.FromJson<DrillChart>(json);
                    summaries.Add(new DrillChartSummary
                    {
                        Id = chart.Id,
                        Name = chart.Name,
                        SongId = chart.SongId,
                        FormationCount = chart.Formations?.Count ?? 0,
                        LastModified = chart.LastModifiedDate
                    });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"LocalSaveSystem: Failed to read chart {file}: {e.Message}");
                }
            }

            return Task.FromResult(
                summaries.OrderByDescending(s => s.LastModified).ToList());
        }

        public Task DeleteDrillChart(string chartId)
        {
            var fileName = $"chart_{chartId}.json";
            var path = Path.Combine(_chartsPath, fileName);

            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }

        // Player Progress
        public Task SavePlayerProgress(PlayerProgress progress)
        {
            _cachedProgress = progress;
            var json = JsonUtility.ToJson(progress, true);
            WriteFile(Path.Combine(_savePath, ProgressFileName), json);
            HasSaveData = true;
            return Task.CompletedTask;
        }

        public Task<PlayerProgress> LoadPlayerProgress()
        {
            if (_cachedProgress != null)
                return Task.FromResult(_cachedProgress);

            var path = Path.Combine(_savePath, ProgressFileName);
            if (!File.Exists(path))
                return Task.FromResult(new PlayerProgress());

            var json = File.ReadAllText(path);
            _cachedProgress = JsonUtility.FromJson<PlayerProgress>(json);
            return Task.FromResult(_cachedProgress);
        }

        // Formation Templates
        public Task SaveFormationTemplate(FormationTemplate template)
        {
            EnsureDirectoriesExist();

            if (string.IsNullOrEmpty(template.Id))
                template.Id = Guid.NewGuid().ToString();

            var json = JsonUtility.ToJson(template, true);
            var fileName = $"template_{template.Id}.json";
            WriteFile(Path.Combine(_templatesPath, fileName), json);
            return Task.CompletedTask;
        }

        public Task<List<FormationTemplate>> ListUserTemplates()
        {
            var templates = new List<FormationTemplate>();
            EnsureDirectoriesExist();

            var files = Directory.GetFiles(_templatesPath, "template_*.json");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var template = JsonUtility.FromJson<FormationTemplate>(json);
                    templates.Add(template);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"LocalSaveSystem: Failed to read template {file}: {e.Message}");
                }
            }

            return Task.FromResult(templates.OrderBy(t => t.Name).ToList());
        }

        public Task DeleteFormationTemplate(string templateId)
        {
            var fileName = $"template_{templateId}.json";
            var path = Path.Combine(_templatesPath, fileName);

            if (File.Exists(path))
                File.Delete(path);

            return Task.CompletedTask;
        }

        // Helpers
        private void EnsureDirectoriesExist()
        {
            if (!Directory.Exists(_savePath))
                Directory.CreateDirectory(_savePath);
            if (!Directory.Exists(_chartsPath))
                Directory.CreateDirectory(_chartsPath);
            if (!Directory.Exists(_templatesPath))
                Directory.CreateDirectory(_templatesPath);
        }

        private void WriteFile(string path, string content)
        {
            EnsureDirectoriesExist();
            File.WriteAllText(path, content);
        }
    }
}
