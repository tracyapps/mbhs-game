using System;
using System.Collections.Generic;
using System.Linq;
using MBHS.Data.Enums;

namespace MBHS.Data.Models
{
    [Serializable]
    public class BandRosterData
    {
        public string SchoolId;
        public List<BandMemberData> Members = new();
        public int Budget;
        public int Reputation; // 0-100, affects recruiting quality

        public int ActiveMemberCount =>
            Members.Count(m => m.Status == MemberStatus.Active);

        public List<BandMemberData> GetMembersByInstrument(InstrumentType type) =>
            Members.Where(m => m.AssignedInstrument == type &&
                              m.Status == MemberStatus.Active).ToList();

        public List<BandMemberData> GetMembersByFamily(InstrumentFamily family) =>
            Members.Where(m => m.Status == MemberStatus.Active &&
                              GetFamily(m.AssignedInstrument) == family).ToList();

        public BandMemberData GetMemberById(string memberId) =>
            Members.FirstOrDefault(m => m.Id == memberId);

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
