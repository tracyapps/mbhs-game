using UnityEngine;

namespace MBHS.Systems.FormationEditor.Commands
{
    public class MoveMemberCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly string _formationId;
        private readonly string _memberId;
        private readonly Vector2 _newPosition;
        private readonly float _newFacing;
        private readonly Vector2 _oldPosition;
        private readonly float _oldFacing;

        public string Description => $"Move member {_memberId}";

        public MoveMemberCommand(
            IFormationSystem formationSystem,
            string formationId,
            string memberId,
            Vector2 oldPosition,
            float oldFacing,
            Vector2 newPosition,
            float newFacing)
        {
            _formationSystem = formationSystem;
            _formationId = formationId;
            _memberId = memberId;
            _oldPosition = oldPosition;
            _oldFacing = oldFacing;
            _newPosition = newPosition;
            _newFacing = newFacing;
        }

        public void Execute()
        {
            _formationSystem.SetMemberPosition(
                _formationId, _memberId, _newPosition, _newFacing);
        }

        public void Undo()
        {
            _formationSystem.SetMemberPosition(
                _formationId, _memberId, _oldPosition, _oldFacing);
        }
    }
}
