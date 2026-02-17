namespace MBHS.Systems.FormationEditor.Commands
{
    public interface IEditorCommand
    {
        string Description { get; }
        void Execute();
        void Undo();
    }
}
