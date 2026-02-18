using System;
using System.Collections.Generic;
using MBHS.Data.Models;
using MBHS.Systems.ContentPipeline;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class SongBrowserOverlay : VisualElement
    {
        private readonly IContentCatalog _catalog;
        private readonly ScrollView _songList;
        private readonly TextField _searchField;

        private List<ContentManifestEntry> _allSongs = new();

        public event Action<string> OnSongSelected;
        public event Action OnCancelled;

        public SongBrowserOverlay(IContentCatalog catalog)
        {
            _catalog = catalog;

            AddToClassList("song-browser-overlay");
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

            // Center panel
            var panel = new VisualElement();
            panel.AddToClassList("song-browser-panel");

            // Header
            var header = new VisualElement();
            header.AddToClassList("song-browser-header");

            var title = new Label("Select Song");
            title.AddToClassList("song-browser-title");
            header.Add(title);

            var cancelBtn = new Button(Close) { text = "X" };
            cancelBtn.AddToClassList("song-browser-close-btn");
            header.Add(cancelBtn);

            panel.Add(header);

            // Search
            _searchField = new TextField();
            _searchField.AddToClassList("song-browser-search");
            _searchField.value = "";
            _searchField.RegisterValueChangedCallback(_ => FilterSongs());
            panel.Add(_searchField);

            // Song list
            _songList = new ScrollView(ScrollViewMode.Vertical);
            _songList.AddToClassList("song-browser-list");
            panel.Add(_songList);

            Add(panel);

            style.display = DisplayStyle.None;
        }

        public async void Open()
        {
            style.display = DisplayStyle.Flex;
            _searchField.value = "";

            try
            {
                _allSongs = await _catalog.GetSongs();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SongBrowserOverlay: Failed to load songs: {e.Message}");
                _allSongs = new List<ContentManifestEntry>();
            }

            FilterSongs();
        }

        public void Close()
        {
            style.display = DisplayStyle.None;
            OnCancelled?.Invoke();
        }

        private void FilterSongs()
        {
            _songList.Clear();

            string query = _searchField.value?.Trim().ToLowerInvariant() ?? "";

            foreach (var song in _allSongs)
            {
                if (!string.IsNullOrEmpty(query) &&
                    !song.Title.ToLowerInvariant().Contains(query) &&
                    !song.Author.ToLowerInvariant().Contains(query))
                    continue;

                var row = CreateSongRow(song);
                _songList.Add(row);
            }

            if (_songList.childCount == 0)
            {
                var empty = new Label("No songs found");
                empty.AddToClassList("song-browser-empty");
                _songList.Add(empty);
            }
        }

        private VisualElement CreateSongRow(ContentManifestEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("song-browser-row");

            var info = new VisualElement();
            info.AddToClassList("song-browser-row-info");

            var title = new Label(entry.Title);
            title.AddToClassList("song-browser-row-title");
            info.Add(title);

            var detail = new Label(entry.Author);
            detail.AddToClassList("song-browser-row-detail");
            info.Add(detail);

            row.Add(info);

            var selectBtn = new Button(() =>
            {
                OnSongSelected?.Invoke(entry.Id);
                style.display = DisplayStyle.None;
            }) { text = "Select" };
            selectBtn.AddToClassList("song-browser-select-btn");
            row.Add(selectBtn);

            return row;
        }
    }
}
