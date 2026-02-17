using System;
using System.Collections.Generic;
using MBHS.Data.Enums;
using MBHS.Data.Models;

namespace MBHS.Systems.BandManagement
{
    public interface IBandManager
    {
        BandRosterData Roster { get; }

        void LoadRoster(BandRosterData roster);
        void CreateNewRoster(string schoolId);

        BandMemberData RecruitMember(RecruitCandidate candidate);
        void DismissMember(string memberId);
        void AssignInstrument(string memberId, InstrumentType instrument);
        void TrainMember(string memberId, SkillType skill, float amount);

        List<RecruitCandidate> GetAvailableRecruits(int count);
        BandComposition GetCurrentComposition();

        event Action<BandMemberData> OnMemberRecruited;
        event Action<BandMemberData> OnMemberUpdated;
        event Action<string> OnMemberDismissed;
        event Action<BandRosterData> OnRosterChanged;
    }
}
