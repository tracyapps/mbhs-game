using MBHS.Data.Enums;
using UnityEngine;

namespace MBHS.Data.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Instrument", menuName = "MBHS/Data/Instrument Definition")]
    public class InstrumentDefinition : ScriptableObject
    {
        [Header("Identity")]
        public InstrumentType Type;
        public InstrumentFamily Family;
        public string DisplayName;
        public Sprite Icon;

        [Header("3D Model")]
        public string ModelPrefabAddressableKey;
        public Vector3 HoldOffset;
        public Quaternion HoldRotation;

        [Header("Animation")]
        public AnimatorOverrideController AnimOverride;

        [Header("Audio")]
        [Range(0f, 1f)]
        public float VolumeContribution = 0.5f;

        [Header("Band Composition")]
        [Tooltip("Minimum recommended count for a balanced 120-member band")]
        public int MinRecommendedCount;
        [Tooltip("Maximum recommended count for a balanced 120-member band")]
        public int MaxRecommendedCount;

        [Header("Gameplay")]
        [Range(1, 10)]
        public int DifficultyToLearn = 5;
        [Range(1, 10)]
        public int PhysicalDemand = 5;
    }
}
