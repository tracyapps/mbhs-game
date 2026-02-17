using MBHS.Data.Models;
using UnityEngine;

namespace MBHS.Systems.FormationEditor.Commands
{
    public class RemoveMemberCommand : IEditorCommand
    {
        private readonly IFormationSystem _formationSystem;
        private readonly string _formationId;
        private readonly string _memberId;
        private readonly Vector2 _savedPosition;
        private readonly float _savedFacing;

        public string Description => $"Remove member {_memberId}";

        public RemoveMemberCommand(
            IFormationSystem formationSystem,
            string formationId,
            string memberId,
            Vector2 savedPosition,
            float savedFacing)
        {
            _formationSystem = formationSystem;
            _formationId = formationId;
            _memberId = memberId;
            _savedPosition = savedPosition;
            _savedFacing = savedFacing;
        }

        public void Execute()
        {
            _formationSystem.RemoveMemberFromFormation(_formationId, _memberId);
        }

        public void Undo()
        {
            _formationSystem.SetMemberPosition(
                _formationId, _memberId, _savedPosition, _savedFacing);
        }
    }
}
