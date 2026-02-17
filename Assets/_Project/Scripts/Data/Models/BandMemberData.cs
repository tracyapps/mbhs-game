using System;
using System.Collections.Generic;
using MBHS.Data.Enums;

namespace MBHS.Data.Models
{
    [Serializable]
    public class BandMemberData
    {
        public string Id;
        public string FirstName;
        public string LastName;
        public InstrumentType AssignedInstrument;
        public int YearInSchool; // 1-4 (Freshman to Senior)
        public MemberStatus Status;

        // Skills: 0.0 to 1.0
        public float Musicianship;
        public float Marching;
        public float Stamina;
        public float Showmanship;

        public int Experience;
        public int Morale; // 0-100

        public string DisplayName => $"{FirstName} {LastName}";

        public float GetSkill(SkillType skill)
        {
            return skill switch
            {
                SkillType.Musicianship => Musicianship,
                SkillType.Marching => Marching,
                SkillType.Stamina => Stamina,
                SkillType.Showmanship => Showmanship,
                _ => 0f
            };
        }

        public void SetSkill(SkillType skill, float value)
        {
            value = Math.Clamp(value, 0f, 1f);
            switch (skill)
            {
                case SkillType.Musicianship: Musicianship = value; break;
                case SkillType.Marching: Marching = value; break;
                case SkillType.Stamina: Stamina = value; break;
                case SkillType.Showmanship: Showmanship = value; break;
            }
        }

        public float OverallRating =>
            (Musicianship + Marching + Stamina + Showmanship) / 4f;
    }
}
