namespace MBHS.Data.Enums
{
    public enum SkillType
    {
        Musicianship,
        Marching,
        Stamina,
        Showmanship
    }

    public enum MemberStatus
    {
        Active,
        Injured,
        Benched,
        Graduated
    }

    public enum TransitionType
    {
        Snap,
        LinearMarch,
        CurvedMarch,
        Scatter,
        Custom
    }

    public enum CameraMode
    {
        AudienceView,
        FieldView,
        OverheadView,
        FirstPerson,
        Cinematic
    }

    public enum ShowState
    {
        Idle,
        Preparing,
        Ready,
        Playing,
        Paused,
        Complete
    }

    public enum ContentType
    {
        Song,
        FormationTemplate,
        UniformDesign,
        StadiumTheme,
        ShowPackage
    }
}
