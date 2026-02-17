using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.ContentPipeline
{
    public class LocalContentCatalog : IContentCatalog
    {
        private ContentManifest _manifest;

        public event Action<float> OnContentLoadProgress;

        public LocalContentCatalog()
        {
            LoadLocalManifest();
        }

        public Task<List<ContentManifestEntry>> GetSongs()
        {
            var songs = _manifest.Entries
                .Where(e => e.Type == ContentType.Song)
                .ToList();
            return Task.FromResult(songs);
        }

        public Task<List<ContentManifestEntry>> GetFormationTemplates()
        {
            var templates = _manifest.Entries
                .Where(e => e.Type == ContentType.FormationTemplate)
                .ToList();
            return Task.FromResult(templates);
        }

        public Task<SongData> LoadSongDataAsync(string songId)
        {
            // In Phase 1, load from StreamingAssets or Resources
            // In Phase 5, this will use Addressables.LoadAssetAsync
            var json = Resources.Load<TextAsset>($"Songs/{songId}");
            if (json != null)
            {
                var songData = JsonUtility.FromJson<SongData>(json.text);
                OnContentLoadProgress?.Invoke(1f);
                return Task.FromResult(songData);
            }

            Debug.LogWarning($"LocalContentCatalog: Song not found: {songId}");
            return Task.FromResult<SongData>(null);
        }

        public Task<FormationTemplate> LoadFormationTemplateAsync(string templateId)
        {
            var json = Resources.Load<TextAsset>($"Templates/{templateId}");
            if (json != null)
            {
                var template = JsonUtility.FromJson<FormationTemplate>(json.text);
                return Task.FromResult(template);
            }

            Debug.LogWarning($"LocalContentCatalog: Template not found: {templateId}");
            return Task.FromResult<FormationTemplate>(null);
        }

        public Task<List<ContentManifestEntry>> SearchContent(ContentFilter filter)
        {
            var results = _manifest.Entries.AsEnumerable();

            if (filter.Type.HasValue)
                results = results.Where(e => e.Type == filter.Type.Value);

            if (!string.IsNullOrEmpty(filter.SearchQuery))
            {
                var query = filter.SearchQuery.ToLowerInvariant();
                results = results.Where(e =>
                    e.Title.ToLowerInvariant().Contains(query) ||
                    e.Description.ToLowerInvariant().Contains(query));
            }

            if (filter.Tags != null && filter.Tags.Count > 0)
                results = results.Where(e =>
                    e.Tags.Any(t => filter.Tags.Contains(t)));

            return Task.FromResult(
                results.Skip(filter.Offset).Take(filter.MaxResults).ToList());
        }

        // Marketplace stubs - not implemented until Phase 5
        public Task<List<ContentManifestEntry>> BrowseMarketplace(ContentFilter filter)
        {
            Debug.Log("LocalContentCatalog: Marketplace not yet implemented.");
            return Task.FromResult(new List<ContentManifestEntry>());
        }

        public Task<bool> PurchaseContent(string contentId)
        {
            Debug.Log("LocalContentCatalog: Purchases not yet implemented.");
            return Task.FromResult(false);
        }

        public Task<bool> PublishContent(string contentId, ContentManifestEntry metadata)
        {
            Debug.Log("LocalContentCatalog: Publishing not yet implemented.");
            return Task.FromResult(false);
        }

        private void LoadLocalManifest()
        {
            // Try loading from StreamingAssets, fall back to empty manifest
            _manifest = new ContentManifest
            {
                Version = "0.1.0",
                LastUpdated = DateTime.UtcNow.ToString("o"),
                Entries = new List<ContentManifestEntry>()
            };

            var manifestAsset = Resources.Load<TextAsset>("content_manifest");
            if (manifestAsset != null)
            {
                _manifest = JsonUtility.FromJson<ContentManifest>(manifestAsset.text);
            }
            else
            {
                Debug.Log("LocalContentCatalog: No content manifest found. Using empty catalog.");
            }
        }
    }
}
