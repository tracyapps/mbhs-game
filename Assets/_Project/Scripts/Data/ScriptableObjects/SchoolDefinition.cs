using UnityEngine;

namespace MBHS.Data.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New School", menuName = "MBHS/Data/School Definition")]
    public class SchoolDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string SchoolId;
        public string SchoolName;
        public string Mascot;
        public string City;
        public string State;
        public string Conference;

        [Header("Visuals")]
        public Color PrimaryColor = Color.blue;
        public Color SecondaryColor = Color.white;
        public Color AccentColor = Color.yellow;
        public Sprite Logo;
        public string UniformAddressableKey;
        public string StadiumThemeAddressableKey;

        [Header("Starting Stats")]
        [Range(0, 100)]
        public int StartingReputation = 50;
        public int StartingBudget = 10000;
        [Range(10, 200)]
        public int StartingBandSize = 60;

        [Header("Recruiting")]
        [Range(0f, 1f)]
        [Tooltip("Base quality of recruits. Higher reputation schools attract better talent.")]
        public float RecruitQualityBase = 0.5f;
        [Range(1, 20)]
        public int RecruitsPerSeason = 10;

        [Header("Flavor")]
        [TextArea(2, 4)]
        public string Description;
        public string FightSongId;
    }
}
