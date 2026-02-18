namespace MBHS.Systems.FormationEditor.Commands
{
    public class MoveFormationCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly string _formationId;
        private readonly float _oldStartBeat;
        private readonly float _newStartBeat;

        public string Description => $"Move formation to beat {_newStartBeat:F1}";

        public MoveFormationCommand(
            IFormationSystem formationSystem,
            string formationId,
            float oldStartBeat,
            float newStartBeat)
        {
            _formationSystem = formationSystem;
            _formationId = formationId;
            _oldStartBeat = oldStartBeat;
            _newStartBeat = newStartBeat;
        }

        public void Execute()
        {
            _formationSystem.UpdateFormation(_formationId, startBeat: _newStartBeat);
        }

        public void Undo()
        {
            _formationSystem.UpdateFormation(_formationId, startBeat: _oldStartBeat);
        }
    }
}
