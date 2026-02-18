using System;
using System.Collections.Generic;
using System.Linq;
using MBHS.Data.Enums;
using UnityEngine;

namespace MBHS.Data.Models
{
    [Serializable]
    public class DrillChart
    {
        public string Id;
        public string Name;
        public string Description;
        public string SongId;
        public float TotalDurationBeats;
        public List<Formation> Formations = new();
        public AudioTimelineData AudioTimeline = new();
        public string CreatedDate;
        public string LastModifiedDate;

        public Formation GetFormationAtBeat(float beat)
        {
            for (int i = Formations.Count - 1; i >= 0; i--)
            {
                if (beat >= Formations[i].StartBeat)
                    return Formations[i];
            }

            return Formations.Count > 0 ? Formations[0] : null;
        }

        public int GetFormationIndexAtBeat(float beat)
        {
            for (int i = Formations.Count - 1; i >= 0; i--)
            {
                if (beat >= Formations[i].StartBeat)
                    return i;
            }

            return 0;
        }
    }

    [Serializable]
    public class Formation
    {
        public string Id;
        public string Label;
        public float StartBeat;
        public float DurationBeats;
        public TransitionType TransitionIn;
        public List<MemberPosition> Positions = new();

        public MemberPosition GetPositionForMember(string memberId) =>
            Positions.FirstOrDefault(p => p.MemberId == memberId);
    }

    [Serializable]
    public class MemberPosition
    {
        public string MemberId;
        public float FieldX; // 0-100 (yard line, 0 = left end zone)
        public float FieldY; // 0-53.33 (sideline to sideline)
        public float FacingAngle; // degrees, 0 = toward home side

        public Vector2 FieldPosition
        {
            get => new Vector2(FieldX, FieldY);
            set
            {
                FieldX = value.x;
                FieldY = value.y;
            }
        }
    }

    [Serializable]
    public class FormationTemplate
    {
        public string Id;
        public string Name;
        public string Description;
        public string AuthorId;
        public int SlotCount;
        public List<TemplateSlot> Slots = new();
    }

    [Serializable]
    public class TemplateSlot
    {
        public int SlotIndex;
        public float FieldX;
        public float FieldY;
        public float FacingAngle;
        public InstrumentFamily PreferredFamily; // suggestion, not required
    }
}
