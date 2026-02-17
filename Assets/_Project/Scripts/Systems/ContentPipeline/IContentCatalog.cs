using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MBHS.Data.Enums;
using MBHS.Data.Models;

namespace MBHS.Systems.ContentPipeline
{
    public interface IContentCatalog
    {
        // Browse built-in content
        Task<List<ContentManifestEntry>> GetSongs();
        Task<List<ContentManifestEntry>> GetFormationTemplates();

        // Load content assets
        Task<SongData> LoadSongDataAsync(string songId);
        Task<FormationTemplate> LoadFormationTemplateAsync(string templateId);

        // Content filtering
        Task<List<ContentManifestEntry>> SearchContent(ContentFilter filter);

        // Future marketplace stubs
        Task<List<ContentManifestEntry>> BrowseMarketplace(ContentFilter filter);
        Task<bool> PurchaseContent(string contentId);
        Task<bool> PublishContent(string contentId, ContentManifestEntry metadata);

        event Action<float> OnContentLoadProgress;
    }

    [Serializable]
    public class ContentFilter
    {
        public ContentType? Type;
        public string SearchQuery;
        public string AuthorId;
        public List<string> Tags;
        public int MaxResults = 50;
        public int Offset;
    }
}
