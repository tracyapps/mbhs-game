using System.Collections.Generic;
using System.Linq;
using MBHS.Data.Enums;
using MBHS.Data.Models;

namespace MBHS.Systems.FormationEditor
{
    /// <summary>
    /// Maps template slots to roster members by family preference and rating.
    /// </summary>
    public static class TemplateAutoMapper
    {
        public static Dictionary<int, string> CreateMapping(
            FormationTemplate template,
            List<BandMemberData> activeMembers)
        {
            var mapping = new Dictionary<int, string>();
            var assigned = new HashSet<string>();

            // Pass 1: Match by PreferredFamily, rarest families first
            var slotsByFamilyRarity = template.Slots
                .GroupBy(s => s.PreferredFamily)
                .OrderBy(g => activeMembers.Count(m => GetFamily(m.AssignedInstrument) == g.Key))
                .SelectMany(g => g)
                .ToList();

            foreach (var slot in slotsByFamilyRarity)
            {
                var bestMatch = activeMembers
                    .Where(m => !assigned.Contains(m.Id)
                                && GetFamily(m.AssignedInstrument) == slot.PreferredFamily)
                    .OrderByDescending(m => m.OverallRating)
                    .FirstOrDefault();

                if (bestMatch != null)
                {
                    mapping[slot.SlotIndex] = bestMatch.Id;
                    assigned.Add(bestMatch.Id);
                }
            }

            // Pass 2: Fill remaining slots with any unassigned members by rating
            var unmappedSlots = template.Slots
                .Where(s => !mapping.ContainsKey(s.SlotIndex))
                .OrderBy(s => s.SlotIndex);

            var availableMembers = activeMembers
                .Where(m => !assigned.Contains(m.Id))
                .OrderByDescending(m => m.OverallRating)
                .ToList();

            int idx = 0;
            foreach (var slot in unmappedSlots)
            {
                if (idx >= availableMembers.Count) break;
                mapping[slot.SlotIndex] = availableMembers[idx].Id;
                idx++;
            }

            return mapping;
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
    }
}
