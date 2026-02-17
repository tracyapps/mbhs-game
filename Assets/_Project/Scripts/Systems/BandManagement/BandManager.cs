using System;
using System.Collections.Generic;
using System.Linq;
using MBHS.Data.Enums;
using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.BandManagement
{
    public class BandManager : IBandManager
    {
        private BandRosterData _roster;

        // Name pools for generating recruits
        private static readonly string[] FirstNames =
        {
            "Alex", "Jordan", "Taylor", "Morgan", "Casey", "Riley", "Quinn",
            "Avery", "Harper", "Skyler", "Dakota", "Charlie", "Reese", "Finley",
            "Jamie", "Rowan", "Ellis", "Sage", "Hayden", "Parker",
            "Emma", "Liam", "Olivia", "Noah", "Ava", "Ethan", "Sophia", "Mason",
            "Isabella", "James", "Mia", "Benjamin", "Charlotte", "Lucas", "Amelia",
            "Marcus", "Diana", "Carlos", "Yuki", "Priya", "Malik", "Chen", "Fatima"
        };

        private static readonly string[] LastNames =
        {
            "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
            "Rodriguez", "Martinez", "Anderson", "Taylor", "Thomas", "Jackson",
            "White", "Harris", "Martin", "Thompson", "Lee", "Walker", "Allen",
            "Young", "King", "Wright", "Lopez", "Hill", "Scott", "Green", "Adams",
            "Baker", "Nelson", "Carter", "Mitchell", "Perez", "Roberts", "Turner",
            "Kim", "Patel", "Nakamura", "Chen", "Hassan", "Okafor", "Schmidt"
        };

        public BandRosterData Roster => _roster;

        public event Action<BandMemberData> OnMemberRecruited;
        public event Action<BandMemberData> OnMemberUpdated;
        public event Action<string> OnMemberDismissed;
        public event Action<BandRosterData> OnRosterChanged;

        public void LoadRoster(BandRosterData roster)
        {
            _roster = roster;
            OnRosterChanged?.Invoke(_roster);
        }

        public void CreateNewRoster(string schoolId)
        {
            _roster = new BandRosterData
            {
                SchoolId = schoolId,
                Members = new List<BandMemberData>(),
                Budget = 10000,
                Reputation = 50
            };
            OnRosterChanged?.Invoke(_roster);
        }

        public BandMemberData RecruitMember(RecruitCandidate candidate)
        {
            if (_roster == null)
            {
                Debug.LogError("BandManager: No roster loaded.");
                return null;
            }

            if (candidate.RecruitCost > _roster.Budget)
            {
                Debug.LogWarning("BandManager: Insufficient budget for recruitment.");
                return null;
            }

            _roster.Budget -= candidate.RecruitCost;
            _roster.Members.Add(candidate.MemberData);

            OnMemberRecruited?.Invoke(candidate.MemberData);
            OnRosterChanged?.Invoke(_roster);

            return candidate.MemberData;
        }

        public void DismissMember(string memberId)
        {
            if (_roster == null) return;

            var member = _roster.GetMemberById(memberId);
            if (member == null) return;

            _roster.Members.Remove(member);
            OnMemberDismissed?.Invoke(memberId);
            OnRosterChanged?.Invoke(_roster);
        }

        public void AssignInstrument(string memberId, InstrumentType instrument)
        {
            var member = _roster?.GetMemberById(memberId);
            if (member == null) return;

            member.AssignedInstrument = instrument;
            OnMemberUpdated?.Invoke(member);
            OnRosterChanged?.Invoke(_roster);
        }

        public void TrainMember(string memberId, SkillType skill, float amount)
        {
            var member = _roster?.GetMemberById(memberId);
            if (member == null) return;

            float current = member.GetSkill(skill);
            member.SetSkill(skill, current + amount);
            member.Experience += Mathf.RoundToInt(amount * 100f);

            OnMemberUpdated?.Invoke(member);
            OnRosterChanged?.Invoke(_roster);
        }

        public List<RecruitCandidate> GetAvailableRecruits(int count)
        {
            var candidates = new List<RecruitCandidate>();
            float reputationFactor = (_roster?.Reputation ?? 50) / 100f;

            for (int i = 0; i < count; i++)
            {
                candidates.Add(GenerateCandidate(reputationFactor));
            }

            return candidates.OrderByDescending(c => c.MemberData.OverallRating).ToList();
        }

        public BandComposition GetCurrentComposition()
        {
            var composition = new BandComposition
            {
                FamilyCounts = new Dictionary<InstrumentFamily, int>(),
                TotalMembers = 0,
                Warnings = new List<string>()
            };

            if (_roster == null) return composition;

            var activeMembers = _roster.Members
                .Where(m => m.Status == MemberStatus.Active)
                .ToList();

            composition.TotalMembers = activeMembers.Count;

            // Count by family
            foreach (InstrumentFamily family in Enum.GetValues(typeof(InstrumentFamily)))
            {
                composition.FamilyCounts[family] = 0;
            }

            foreach (var member in activeMembers)
            {
                var family = GetFamily(member.AssignedInstrument);
                composition.FamilyCounts[family]++;
            }

            // Check for warnings
            if (!activeMembers.Any(m => m.AssignedInstrument == InstrumentType.DrumMajor))
                composition.Warnings.Add("No drum major assigned");

            if (composition.FamilyCounts[InstrumentFamily.BatteryPercussion] == 0)
                composition.Warnings.Add("No battery percussion section");

            if (composition.FamilyCounts[InstrumentFamily.Brass] == 0)
                composition.Warnings.Add("No brass section");

            // Calculate balance score
            composition.BalanceScore = CalculateBalance(composition);

            return composition;
        }

        private RecruitCandidate GenerateCandidate(float reputationFactor)
        {
            float qualityBase = 0.2f + reputationFactor * 0.5f;
            float qualityRange = 0.3f;

            var member = new BandMemberData
            {
                Id = Guid.NewGuid().ToString(),
                FirstName = FirstNames[UnityEngine.Random.Range(0, FirstNames.Length)],
                LastName = LastNames[UnityEngine.Random.Range(0, LastNames.Length)],
                YearInSchool = UnityEngine.Random.Range(1, 5),
                Status = MemberStatus.Active,
                Musicianship = Mathf.Clamp01(qualityBase + UnityEngine.Random.Range(-qualityRange, qualityRange)),
                Marching = Mathf.Clamp01(qualityBase + UnityEngine.Random.Range(-qualityRange, qualityRange)),
                Stamina = Mathf.Clamp01(qualityBase + UnityEngine.Random.Range(-qualityRange, qualityRange)),
                Showmanship = Mathf.Clamp01(qualityBase + UnityEngine.Random.Range(-qualityRange, qualityRange)),
                Experience = 0,
                Morale = UnityEngine.Random.Range(60, 100),
                AssignedInstrument = GetRandomInstrument()
            };

            int cost = Mathf.RoundToInt(500 + member.OverallRating * 1500);

            return new RecruitCandidate
            {
                MemberData = member,
                RecruitCost = cost,
                InterestLevel = Mathf.Clamp01(reputationFactor + UnityEngine.Random.Range(-0.2f, 0.2f))
            };
        }

        private InstrumentType GetRandomInstrument()
        {
            // Weighted distribution that roughly matches real marching bands
            float roll = UnityEngine.Random.value;

            if (roll < 0.35f) // 35% brass
            {
                var brass = new[] {
                    InstrumentType.Trumpet, InstrumentType.Trumpet,
                    InstrumentType.Trombone, InstrumentType.Mellophone,
                    InstrumentType.Sousaphone, InstrumentType.Baritone
                };
                return brass[UnityEngine.Random.Range(0, brass.Length)];
            }
            else if (roll < 0.55f) // 20% woodwind
            {
                var woodwind = new[] {
                    InstrumentType.Clarinet, InstrumentType.Flute,
                    InstrumentType.Saxophone, InstrumentType.Piccolo
                };
                return woodwind[UnityEngine.Random.Range(0, woodwind.Length)];
            }
            else if (roll < 0.75f) // 20% percussion
            {
                var percussion = new[] {
                    InstrumentType.SnareDrum, InstrumentType.BassDrum,
                    InstrumentType.TenorDrums, InstrumentType.Cymbals
                };
                return percussion[UnityEngine.Random.Range(0, percussion.Length)];
            }
            else if (roll < 0.90f) // 15% color guard
            {
                var guard = new[] {
                    InstrumentType.Flag, InstrumentType.Flag,
                    InstrumentType.Rifle, InstrumentType.Saber
                };
                return guard[UnityEngine.Random.Range(0, guard.Length)];
            }
            else // 10% front ensemble / pit
            {
                var pit = new[] {
                    InstrumentType.Marimba, InstrumentType.Xylophone,
                    InstrumentType.Vibraphone, InstrumentType.Timpani
                };
                return pit[UnityEngine.Random.Range(0, pit.Length)];
            }
        }

        private static InstrumentFamily GetFamily(InstrumentType type)
        {
            return type switch
            {
                InstrumentType.Trumpet or InstrumentType.Trombone or
                InstrumentType.FrenchHorn or InstrumentType.Tuba or
                InstrumentType.Sousaphone or InstrumentType.Baritone or
                InstrumentType.Mellophone => InstrumentFamily.Brass,

                InstrumentType.Flute or InstrumentType.Piccolo or
                InstrumentType.Clarinet or InstrumentType.Saxophone
                    => InstrumentFamily.Woodwind,

                InstrumentType.SnareDrum or InstrumentType.BassDrum or
                InstrumentType.TenorDrums or InstrumentType.Cymbals
                    => InstrumentFamily.BatteryPercussion,

                InstrumentType.Xylophone or InstrumentType.Marimba or
                InstrumentType.Vibraphone or InstrumentType.Timpani
                    => InstrumentFamily.FrontEnsemble,

                InstrumentType.Flag or InstrumentType.Rifle or
                InstrumentType.Saber => InstrumentFamily.ColorGuard,

                InstrumentType.DrumMajor => InstrumentFamily.Leadership,

                _ => InstrumentFamily.Brass
            };
        }

        private float CalculateBalance(BandComposition composition)
        {
            if (composition.TotalMembers == 0) return 0f;

            // Ideal ratios for a balanced marching band
            var idealRatios = new Dictionary<InstrumentFamily, float>
            {
                { InstrumentFamily.Brass, 0.35f },
                { InstrumentFamily.Woodwind, 0.20f },
                { InstrumentFamily.BatteryPercussion, 0.15f },
                { InstrumentFamily.FrontEnsemble, 0.08f },
                { InstrumentFamily.ColorGuard, 0.15f },
                { InstrumentFamily.Leadership, 0.02f }
            };

            float totalDeviation = 0f;
            foreach (var ideal in idealRatios)
            {
                float actualRatio = composition.FamilyCounts.GetValueOrDefault(ideal.Key, 0)
                                    / (float)composition.TotalMembers;
                totalDeviation += Mathf.Abs(actualRatio - ideal.Value);
            }

            // Convert deviation to a 0-1 score (lower deviation = higher score)
            return Mathf.Clamp01(1f - totalDeviation);
        }
    }
}
