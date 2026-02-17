using UnityEngine;

namespace MBHS.Systems.FormationEditor.Commands
{
    public class PlaceMemberCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly string _formationId;
        private readonly string _memberId;
        private readonly Vector2 _position;
        private readonly float _facing;

        public string Description => $"Place member {_memberId}";

        public PlaceMemberCommand(
            IFormationSystem formationSystem,
            string formationId,
            string memberId,
            Vector2 position,
            float facing)
        {
            _formationSystem = formationSystem;
            _formationId = formationId;
            _memberId = memberId;
            _position = position;
            _facing = facing;
        }

        public void Execute()
        {
            _formationSystem.SetMemberPosition(
                _formationId, _memberId, _position, _facing);
        }

        public void Undo()
        {
            _formationSystem.RemoveMemberFromFormation(_formationId, _memberId);
        }
    }
}
