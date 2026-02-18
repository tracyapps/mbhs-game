using System;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class SongSelectorButton : VisualElement
    {
        private readonly Label _titleLabel;
        private readonly Button _changeBtn;

        public event Action OnChangeRequested;

        public SongSelectorButton()
        {
            AddToClassList("song-selector");

            _titleLabel = new Label("No Song");
            _titleLabel.AddToClassList("song-selector-title");
            Add(_titleLabel);

            _changeBtn = new Button(() => OnChangeRequested?.Invoke()) { text = "Change" };
            _changeBtn.AddToClassList("song-selector-btn");
            Add(_changeBtn);
        }

        public void SetSong(string title, string composer)
        {
            if (string.IsNullOrEmpty(title))
            {
                _titleLabel.text = "No Song";
                return;
            }

            _titleLabel.text = string.IsNullOrEmpty(composer)
                ? title
                : $"{title} - {composer}";
        }

        public void ClearSong()
        {
            _titleLabel.text = "No Song";
        }
    }
}
