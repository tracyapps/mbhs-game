using System;
using System.Collections.Generic;
using MBHS.Systems.SaveLoad;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class LoadChartOverlay : VisualElement
    {
        private readonly ISaveSystem _saveSystem;
        private readonly ConfirmationDialog _confirmDialog;
        private readonly ScrollView _chartList;

        public event Action<string> OnChartSelected;
        public event Action OnNewChart;
        public event Action OnCancelled;

        public LoadChartOverlay(ISaveSystem saveSystem, ConfirmationDialog confirmDialog)
        {
            _saveSystem = saveSystem;
            _confirmDialog = confirmDialog;

            AddToClassList("load-chart-overlay");
            pickingMode = PickingMode.Position;

            RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == this)
                {
                    Close();
                    evt.StopPropagation();
                }
            });

            var panel = new VisualElement();
            panel.AddToClassList("load-chart-panel");

            // Header
            var header = new VisualElement();
            header.AddToClassList("load-chart-header");

            var title = new Label("Load Chart");
            title.AddToClassList("load-chart-title");
            header.Add(title);

            var closeBtn = new Button(Close) { text = "X" };
            closeBtn.AddToClassList("load-chart-close-btn");
            header.Add(closeBtn);

            panel.Add(header);

            // Chart list
            _chartList = new ScrollView(ScrollViewMode.Vertical);
            _chartList.AddToClassList("load-chart-list");
            panel.Add(_chartList);

            // Footer with New Chart button
            var footer = new VisualElement();
            footer.AddToClassList("load-chart-footer");

            var newBtn = new Button(() =>
            {
                style.display = DisplayStyle.None;
                OnNewChart?.Invoke();
            }) { text = "New Chart" };
            newBtn.AddToClassList("load-chart-new-btn");
            footer.Add(newBtn);

            panel.Add(footer);

            Add(panel);
            style.display = DisplayStyle.None;
        }

        public async void Open()
        {
            style.display = DisplayStyle.Flex;
            _chartList.Clear();

            List<DrillChartSummary> charts;
            try
            {
                charts = await _saveSystem.ListDrillCharts();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LoadChartOverlay: Failed to list charts: {e.Message}");
                charts = new List<DrillChartSummary>();
            }

            if (charts.Count == 0)
            {
                var empty = new Label("No saved charts");
                empty.AddToClassList("load-chart-empty");
                _chartList.Add(empty);
                return;
            }

            foreach (var chart in charts)
            {
                _chartList.Add(CreateChartRow(chart));
            }
        }

        public void Close()
        {
            style.display = DisplayStyle.None;
            OnCancelled?.Invoke();
        }

        private VisualElement CreateChartRow(DrillChartSummary summary)
        {
            var row = new VisualElement();
            row.AddToClassList("load-chart-row");

            var info = new VisualElement();
            info.AddToClassList("load-chart-row-info");

            var name = new Label(string.IsNullOrEmpty(summary.Name) ? "Untitled" : summary.Name);
            name.AddToClassList("load-chart-row-name");
            info.Add(name);

            var detail = new Label($"{summary.FormationCount} formations \u2022 {summary.LastModified}");
            detail.AddToClassList("load-chart-row-detail");
            info.Add(detail);

            row.Add(info);

            var actions = new VisualElement();
            actions.AddToClassList("load-chart-row-actions");

            var loadBtn = new Button(() =>
            {
                OnChartSelected?.Invoke(summary.Id);
                style.display = DisplayStyle.None;
            }) { text = "Load" };
            loadBtn.AddToClassList("load-chart-load-btn");
            actions.Add(loadBtn);

            var deleteBtn = new Button(() =>
            {
                _confirmDialog.Show(
                    "Delete Chart",
                    $"Delete \"{summary.Name}\"? This cannot be undone.",
                    () => DeleteChart(summary.Id),
                    confirmText: "Delete",
                    isDanger: true
                );
            }) { text = "Del" };
            deleteBtn.AddToClassList("load-chart-delete-btn");
            actions.Add(deleteBtn);

            row.Add(actions);
            return row;
        }

        private async void DeleteChart(string chartId)
        {
            try
            {
                await _saveSystem.DeleteDrillChart(chartId);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LoadChartOverlay: Failed to delete chart: {e.Message}");
            }

            Open(); // Refresh list
        }
    }
}
