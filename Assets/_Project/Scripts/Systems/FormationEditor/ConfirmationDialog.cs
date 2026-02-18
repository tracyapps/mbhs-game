using System;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class ConfirmationDialog : VisualElement
    {
        private readonly Label _titleLabel;
        private readonly Label _messageLabel;
        private readonly Button _confirmBtn;
        private readonly Button _cancelBtn;

        private Action _onConfirm;

        public ConfirmationDialog()
        {
            AddToClassList("confirmation-overlay");
            pickingMode = PickingMode.Position;

            RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == this)
                {
                    Close();
                    evt.StopPropagation();
                }
            });

            var panel = new VisualElement();
            panel.AddToClassList("confirmation-panel");

            _titleLabel = new Label();
            _titleLabel.AddToClassList("confirmation-title");
            panel.Add(_titleLabel);

            _messageLabel = new Label();
            _messageLabel.AddToClassList("confirmation-message");
            panel.Add(_messageLabel);

            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("confirmation-buttons");

            _cancelBtn = new Button(Close);
            _cancelBtn.AddToClassList("confirmation-cancel-btn");
            buttonRow.Add(_cancelBtn);

            _confirmBtn = new Button(() =>
            {
                var callback = _onConfirm;
                Close();
                callback?.Invoke();
            });
            _confirmBtn.AddToClassList("confirmation-confirm-btn");
            buttonRow.Add(_confirmBtn);

            panel.Add(buttonRow);
            Add(panel);

            style.display = DisplayStyle.None;
        }

        public void Show(string title, string message, Action onConfirm,
            string confirmText = "OK", string cancelText = "Cancel", bool isDanger = false)
        {
            _titleLabel.text = title;
            _messageLabel.text = message;
            _confirmBtn.text = confirmText;
            _cancelBtn.text = cancelText;
            _onConfirm = onConfirm;

            _confirmBtn.EnableInClassList("confirmation-danger-btn", isDanger);

            style.display = DisplayStyle.Flex;
        }

        private void Close()
        {
            style.display = DisplayStyle.None;
            _onConfirm = null;
        }
    }
}
