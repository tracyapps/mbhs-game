using UnityEngine;

namespace MBHS.Data.ScriptableObjects
{
    [CreateAssetMenu(fileName = "New Scoring Rubric", menuName = "MBHS/Data/Scoring Rubric")]
    public class ScoringRubricDefinition : ScriptableObject
    {
        [Header("Score Weights (must sum to 1.0)")]
        [Range(0f, 1f)]
        public float FormationWeight = 0.4f;
        [Range(0f, 1f)]
        public float MusicWeight = 0.35f;
        [Range(0f, 1f)]
        public float ShowmanshipWeight = 0.15f;
        [Range(0f, 1f)]
        public float DifficultyWeight = 0.1f;

        [Header("Formation Scoring")]
        [Tooltip("Maximum acceptable position error in yards before penalty")]
        public float PositionErrorThreshold = 0.5f;
        [Tooltip("Maximum acceptable facing error in degrees before penalty")]
        public float FacingErrorThreshold = 10f;

        [Header("Music Scoring")]
        [Range(0f, 1f)]
        [Tooltip("How much member skill affects music quality")]
        public float SkillInfluence = 0.7f;
        [Range(0f, 1f)]
        [Tooltip("How much ensemble balance affects music quality")]
        public float BalanceInfluence = 0.3f;

        [Header("Difficulty Bonus")]
        [Tooltip("Maximum bonus points for difficulty")]
        public float MaxDifficultyBonus = 20f;

        public float TotalWeight =>
            FormationWeight + MusicWeight + ShowmanshipWeight + DifficultyWeight;

        private void OnValidate()
        {
            if (Mathf.Abs(TotalWeight - 1f) > 0.01f)
            {
                Debug.LogWarning(
                    $"ScoringRubric '{name}': Weights sum to {TotalWeight:F2}, expected 1.0");
            }
        }
    }
}
