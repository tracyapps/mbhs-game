using MBHS.Data.Enums;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class MemberAvatarElement : VisualElement
    {
        private string _initial = "?";
        private Color _bgColor = new(0.4f, 0.4f, 0.4f);

        public MemberAvatarElement()
        {
            AddToClassList("member-avatar");
            generateVisualContent += OnGenerateVisualContent;
        }

        public void SetMember(string displayName, InstrumentType instrument)
        {
            _initial = !string.IsNullOrEmpty(displayName)
                ? displayName[0].ToString().ToUpperInvariant()
                : "?";
            _bgColor = GetFamilyColor(instrument);
            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext ctx)
        {
            float w = resolvedStyle.width;
            float h = resolvedStyle.height;
            if (w <= 0 || h <= 0) return;

            var painter = ctx.painter2D;

            // Filled circle background
            float cx = w / 2f;
            float cy = h / 2f;
            float r = Mathf.Min(cx, cy);

            painter.fillColor = _bgColor;
            painter.BeginPath();
            painter.Arc(new Vector2(cx, cy), r, 0f, 360f);
            painter.Fill();

            // Draw initial letter using a centered label (MeshGenerationContext
            // doesn't have text drawing, so we use a child label instead)
        }

        public void UpdateInitialLabel()
        {
            // Remove existing label if any
            var existing = this.Q<Label>("avatar-initial");
            if (existing != null)
            {
                existing.text = _initial;
                return;
            }

            var label = new Label(_initial);
            label.name = "avatar-initial";
            label.AddToClassList("member-avatar-initial");
            label.style.position = Position.Absolute;
            label.style.left = 0;
            label.style.right = 0;
            label.style.top = 0;
            label.style.bottom = 0;
            label.pickingMode = PickingMode.Ignore;
            Add(label);
        }

        private static Color GetFamilyColor(InstrumentType type)
        {
            return type switch
            {
                InstrumentType.Trumpet or InstrumentType.Trombone or
                InstrumentType.FrenchHorn or InstrumentType.Tuba or
                InstrumentType.Sousaphone or InstrumentType.Baritone or
                InstrumentType.Mellophone
                    => new Color(0.86f, 0.71f, 0.16f), // gold

                InstrumentType.Flute or InstrumentType.Piccolo or
                InstrumentType.Clarinet or InstrumentType.Saxophone
                    => new Color(0.24f, 0.63f, 0.24f), // green

                InstrumentType.SnareDrum or InstrumentType.BassDrum or
                InstrumentType.TenorDrums or InstrumentType.Cymbals
                    => new Color(0.78f, 0.24f, 0.24f), // red

                InstrumentType.Xylophone or InstrumentType.Marimba or
                InstrumentType.Vibraphone or InstrumentType.Timpani
                    => new Color(0.78f, 0.39f, 0.16f), // orange

                InstrumentType.Flag or InstrumentType.Rifle or
                InstrumentType.Saber
                    => new Color(0.63f, 0.24f, 0.78f), // purple

                InstrumentType.DrumMajor
                    => new Color(0.9f, 0.9f, 0.9f), // white

                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }
    }
}
