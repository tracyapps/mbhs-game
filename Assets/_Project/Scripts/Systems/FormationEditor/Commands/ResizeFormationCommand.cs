namespace MBHS.Systems.FormationEditor.Commands
{
    public class ResizeFormationCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly string _formationId;
        private readonly float _oldStartBeat;
        private readonly float _oldDuration;
        private readonly float _newStartBeat;
        private readonly float _newDuration;

        public string Description => $"Resize formation to {_newDuration:F1} beats";

        public ResizeFormationCommand(
            IFormationSystem formationSystem,
            string formationId,
            float oldStartBeat,
            float oldDuration,
            float newStartBeat,
            float newDuration)
        {
            _formationSystem = formationSystem;
            _formationId = formationId;
            _oldStartBeat = oldStartBeat;
            _oldDuration = oldDuration;
            _newStartBeat = newStartBeat;
            _newDuration = newDuration;
        }

        public void Execute()
        {
            _formationSystem.UpdateFormation(_formationId,
                startBeat: _newStartBeat, durationBeats: _newDuration);
        }

        public void Undo()
        {
            _formationSystem.UpdateFormation(_formationId,
                startBeat: _oldStartBeat, durationBeats: _oldDuration);
        }
    }
}
