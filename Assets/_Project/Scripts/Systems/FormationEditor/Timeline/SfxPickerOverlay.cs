using System;
using System.Collections.Generic;
using MBHS.Data.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class SfxPickerOverlay : VisualElement
    {
        private readonly ScrollView _itemList;
        private readonly TextField _searchField;
        private SfxCatalog _catalog;

        public event Action<SfxCatalogEntry> OnSfxSelected;
        public event Action OnCancelled;

        public SfxPickerOverlay()
        {
            AddToClassList("sfx-picker-overlay");
            pickingMode = PickingMode.Position;

            // Backdrop click = cancel
            RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == this)
                {
                    Close();
                    evt.StopPropagation();
                }
            });

            var panel = new VisualElement();
            panel.AddToClassList("sfx-picker-panel");

            // Header
            var header = new VisualElement();
            header.AddToClassList("sfx-picker-header");

            var title = new Label("Add Sound Effect");
            title.AddToClassList("sfx-picker-title");
            header.Add(title);

            var cancelBtn = new Button(Close) { text = "X" };
            cancelBtn.AddToClassList("sfx-picker-close-btn");
            header.Add(cancelBtn);

            panel.Add(header);

            // Search
            _searchField = new TextField();
            _searchField.AddToClassList("sfx-picker-search");
            _searchField.RegisterValueChangedCallback(_ => FilterItems());
            panel.Add(_searchField);

            // Item list
            _itemList = new ScrollView(ScrollViewMode.Vertical);
            _itemList.AddToClassList("sfx-picker-list");
            panel.Add(_itemList);

            Add(panel);

            style.display = DisplayStyle.None;

            LoadCatalog();
        }

        private void LoadCatalog()
        {
            var asset = Resources.Load<TextAsset>("sfx_catalog");
            if (asset != null)
            {
                _catalog = JsonUtility.FromJson<SfxCatalog>(asset.text);
            }
            else
            {
                Debug.LogWarning("SfxPickerOverlay: sfx_catalog.json not found in Resources.");
                _catalog = new SfxCatalog();
            }
        }

        public void Open()
        {
            style.display = DisplayStyle.Flex;
            _searchField.value = "";
            FilterItems();
        }

        public void Close()
        {
            style.display = DisplayStyle.None;
            OnCancelled?.Invoke();
        }

        private void FilterItems()
        {
            _itemList.Clear();

            if (_catalog == null) return;

            string query = _searchField.value?.Trim().ToLowerInvariant() ?? "";
            string lastCategory = "";

            foreach (var entry in _catalog.Entries)
            {
                if (!string.IsNullOrEmpty(query) &&
                    !entry.Title.ToLowerInvariant().Contains(query) &&
                    !entry.Category.ToLowerInvariant().Contains(query))
                    continue;

                // Category header
                if (entry.Category != lastCategory)
                {
                    lastCategory = entry.Category;
                    var catLabel = new Label(entry.Category.ToUpperInvariant());
                    catLabel.AddToClassList("sfx-picker-category");
                    _itemList.Add(catLabel);
                }

                var item = CreateSfxItem(entry);
                _itemList.Add(item);
            }

            if (_itemList.childCount == 0)
            {
                var empty = new Label("No effects found");
                empty.AddToClassList("sfx-picker-empty");
                _itemList.Add(empty);
            }
        }

        private VisualElement CreateSfxItem(SfxCatalogEntry entry)
        {
            var item = new VisualElement();
            item.AddToClassList("sfx-picker-item");

            var info = new VisualElement();
            info.AddToClassList("sfx-picker-item-info");

            var title = new Label(entry.Title);
            title.AddToClassList("sfx-picker-item-title");
            info.Add(title);

            string durationStr = entry.DurationSeconds >= 1f
                ? $"{entry.DurationSeconds:F1}s"
                : $"{entry.DurationSeconds * 1000f:F0}ms";
            string loopStr = entry.IsLoopable ? " (loop)" : "";
            var detail = new Label($"{durationStr}{loopStr}");
            detail.AddToClassList("sfx-picker-item-detail");
            info.Add(detail);

            item.Add(info);

            var addBtn = new Button(() =>
            {
                OnSfxSelected?.Invoke(entry);
                Close();
            }) { text = "+" };
            addBtn.AddToClassList("sfx-picker-add-btn");
            item.Add(addBtn);

            return item;
        }
    }
}
