using System;
using System.Collections.Generic;

namespace MBHS.Systems.FormationEditor.Commands
{
    public class CommandHistory
    {
        private readonly Stack<IEditorCommand> _undoStack = new();
        private readonly Stack<IEditorCommand> _redoStack = new();
        private readonly int _maxHistory;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        public event Action OnHistoryChanged;

        public CommandHistory(int maxHistory = 100)
        {
            _maxHistory = maxHistory;
        }

        public void Execute(IEditorCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();

            // Trim history if it exceeds max
            if (_undoStack.Count > _maxHistory)
            {
                var temp = new Stack<IEditorCommand>();
                int count = 0;
                foreach (var cmd in _undoStack)
                {
                    if (count++ >= _maxHistory) break;
                    temp.Push(cmd);
                }

                _undoStack.Clear();
                foreach (var cmd in temp)
                    _undoStack.Push(cmd);
            }

            OnHistoryChanged?.Invoke();
        }

        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            OnHistoryChanged?.Invoke();
        }

        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);

            OnHistoryChanged?.Invoke();
        }

        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            OnHistoryChanged?.Invoke();
        }
    }
}
