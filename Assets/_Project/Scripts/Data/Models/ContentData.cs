using System;
using System.Collections.Generic;
using MBHS.Data.Enums;

namespace MBHS.Data.Models
{
    [Serializable]
    public class ContentManifestEntry
    {
        public string Id;
        public string Title;
        public string Author;
        public ContentType Type;
        public string AddressableKey;
        public string ThumbnailAddressableKey;
        public bool IsOwned;
        public bool IsBuiltIn;
        public string Description;
        public List<string> Tags = new();
    }

    [Serializable]
    public class ContentManifest
    {
        public List<ContentManifestEntry> Entries = new();
        public string Version;
        public string LastUpdated;
    }

    [Serializable]
    public class PlayerProgress
    {
        public string ActiveSchoolId;
        public int TotalShowsPerformed;
        public float HighestScore;
        public string BestGrade;
        public List<string> UnlockedSchoolIds = new();
        public List<string> UnlockedSongIds = new();
        public List<string> CompletedTutorialSteps = new();
        public int TotalPlayTimeMinutes;
    }

    [Serializable]
    public class RecruitCandidate
    {
        public BandMemberData MemberData;
        public int RecruitCost;
        public float InterestLevel; // 0-1, how likely they are to join
    }

    [Serializable]
    public class BandComposition
    {
        public Dictionary<InstrumentFamily, int> FamilyCounts = new();
        public int TotalMembers;
        public float BalanceScore; // 0-1, how balanced the instrumentation is
        public List<string> Warnings = new(); // e.g., "No drum major assigned"
    }
}
