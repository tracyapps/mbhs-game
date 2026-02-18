using System;
using System.Collections.Generic;
using MBHS.Data.Models;
using MBHS.Systems.ContentPipeline;
using MBHS.Systems.SaveLoad;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class TemplateBrowserOverlay : VisualElement
    {
        private readonly IContentCatalog _catalog;
        private readonly ISaveSystem _saveSystem;
        private readonly ConfirmationDialog _confirmDialog;
        private readonly ScrollView _templateList;

        // Inline save fields
        private readonly VisualElement _saveSection;
        private readonly TextField _saveNameField;
        private readonly TextField _saveDescField;

        public event Action<FormationTemplate> OnTemplateSelected;
        public event Action OnCancelled;

        private Func<FormationTemplate> _getCurrentAsTemplate;

        public TemplateBrowserOverlay(IContentCatalog catalog, ISaveSystem saveSystem,
            ConfirmationDialog confirmDialog)
        {
            _catalog = catalog;
            _saveSystem = saveSystem;
            _confirmDialog = confirmDialog;

            AddToClassList("template-browser-overlay");
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
            panel.AddToClassList("template-browser-panel");

            // Header
            var header = new VisualElement();
            header.AddToClassList("template-browser-header");

            var title = new Label("Formation Templates");
            title.AddToClassList("template-browser-title");
            header.Add(title);

            var closeBtn = new Button(Close) { text = "X" };
            closeBtn.AddToClassList("template-browser-close-btn");
            header.Add(closeBtn);

            panel.Add(header);

            // Template list
            _templateList = new ScrollView(ScrollViewMode.Vertical);
            _templateList.AddToClassList("template-browser-list");
            panel.Add(_templateList);

            // Save current section (collapsed by default)
            _saveSection = new VisualElement();
            _saveSection.AddToClassList("template-browser-save-section");
            _saveSection.style.display = DisplayStyle.None;

            var saveHeader = new Label("Save Current Formation as Template");
            saveHeader.AddToClassList("template-browser-save-header");
            _saveSection.Add(saveHeader);

            _saveNameField = new TextField("Name");
            _saveNameField.AddToClassList("template-browser-save-field");
            _saveSection.Add(_saveNameField);

            _saveDescField = new TextField("Description");
            _saveDescField.AddToClassList("template-browser-save-field");
            _saveSection.Add(_saveDescField);

            var saveRow = new VisualElement();
            saveRow.AddToClassList("template-browser-save-row");

            var saveConfirmBtn = new Button(SaveCurrentTemplate) { text = "Save" };
            saveConfirmBtn.AddToClassList("template-browser-save-confirm-btn");
            saveRow.Add(saveConfirmBtn);

            var saveCancelBtn = new Button(() => _saveSection.style.display = DisplayStyle.None)
                { text = "Cancel" };
            saveCancelBtn.AddToClassList("template-browser-save-cancel-btn");
            saveRow.Add(saveCancelBtn);

            _saveSection.Add(saveRow);
            panel.Add(_saveSection);

            // Footer
            var footer = new VisualElement();
            footer.AddToClassList("template-browser-footer");

            var saveCurrentBtn = new Button(() =>
            {
                _saveNameField.value = "";
                _saveDescField.value = "";
                _saveSection.style.display = DisplayStyle.Flex;
            }) { text = "Save Current as Template" };
            saveCurrentBtn.AddToClassList("template-browser-save-btn");
            footer.Add(saveCurrentBtn);

            panel.Add(footer);

            Add(panel);
            style.display = DisplayStyle.None;
        }

        public void SetTemplateProvider(Func<FormationTemplate> provider)
        {
            _getCurrentAsTemplate = provider;
        }

        public async void Open()
        {
            style.display = DisplayStyle.Flex;
            _saveSection.style.display = DisplayStyle.None;
            _templateList.Clear();

            // Built-in section
            var builtInHeader = new Label("Built-In");
            builtInHeader.AddToClassList("template-browser-section-header");
            _templateList.Add(builtInHeader);

            try
            {
                var entries = await _catalog.GetFormationTemplates();
                foreach (var entry in entries)
                {
                    _templateList.Add(CreateTemplateRow(entry.Id, entry.Title,
                        entry.Description, $"{24} slots", false));
                }

                if (entries.Count == 0)
                {
                    var empty = new Label("No built-in templates");
                    empty.AddToClassList("template-browser-empty");
                    _templateList.Add(empty);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"TemplateBrowserOverlay: Failed to load built-in templates: {e.Message}");
            }

            // User section
            var userHeader = new Label("My Templates");
            userHeader.AddToClassList("template-browser-section-header");
            _templateList.Add(userHeader);

            try
            {
                var userTemplates = await _saveSystem.ListUserTemplates();
                foreach (var tmpl in userTemplates)
                {
                    _templateList.Add(CreateTemplateRow(tmpl.Id, tmpl.Name,
                        tmpl.Description, $"{tmpl.SlotCount} slots", true));
                }

                if (userTemplates.Count == 0)
                {
                    var empty = new Label("No saved templates");
                    empty.AddToClassList("template-browser-empty");
                    _templateList.Add(empty);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"TemplateBrowserOverlay: Failed to load user templates: {e.Message}");
            }
        }

        public void Close()
        {
            style.display = DisplayStyle.None;
            OnCancelled?.Invoke();
        }

        private VisualElement CreateTemplateRow(string id, string name,
            string description, string slotInfo, bool isUserTemplate)
        {
            var row = new VisualElement();
            row.AddToClassList("template-browser-row");

            var info = new VisualElement();
            info.AddToClassList("template-browser-row-info");

            var nameLabel = new Label(name);
            nameLabel.AddToClassList("template-browser-row-name");
            info.Add(nameLabel);

            var descLabel = new Label($"{description} \u2022 {slotInfo}");
            descLabel.AddToClassList("template-browser-row-detail");
            info.Add(descLabel);

            row.Add(info);

            var actions = new VisualElement();
            actions.AddToClassList("template-browser-row-actions");

            var applyBtn = new Button(() => ApplyTemplate(id, isUserTemplate)) { text = "Apply" };
            applyBtn.AddToClassList("template-browser-apply-btn");
            actions.Add(applyBtn);

            if (isUserTemplate)
            {
                var deleteBtn = new Button(() =>
                {
                    _confirmDialog.Show(
                        "Delete Template",
                        $"Delete \"{name}\"? This cannot be undone.",
                        () => DeleteUserTemplate(id),
                        confirmText: "Delete",
                        isDanger: true
                    );
                }) { text = "Del" };
                deleteBtn.AddToClassList("template-browser-delete-btn");
                actions.Add(deleteBtn);
            }

            row.Add(actions);
            return row;
        }

        private async void ApplyTemplate(string templateId, bool isUserTemplate)
        {
            FormationTemplate template;
            try
            {
                if (isUserTemplate)
                {
                    var userTemplates = await _saveSystem.ListUserTemplates();
                    template = userTemplates.Find(t => t.Id == templateId);
                }
                else
                {
                    template = await _catalog.LoadFormationTemplateAsync(templateId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"TemplateBrowserOverlay: Failed to load template: {e.Message}");
                return;
            }

            if (template == null) return;

            style.display = DisplayStyle.None;
            OnTemplateSelected?.Invoke(template);
        }

        private async void SaveCurrentTemplate()
        {
            var name = _saveNameField.value?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                _saveNameField.Focus();
                return;
            }

            var template = _getCurrentAsTemplate?.Invoke();
            if (template == null) return;

            template.Name = name;
            template.Description = _saveDescField.value?.Trim() ?? "";
            template.AuthorId = "player";

            try
            {
                await _saveSystem.SaveFormationTemplate(template);
                _saveSection.style.display = DisplayStyle.None;
                Open(); // Refresh
            }
            catch (Exception e)
            {
                Debug.LogWarning($"TemplateBrowserOverlay: Failed to save template: {e.Message}");
            }
        }

        private async void DeleteUserTemplate(string templateId)
        {
            try
            {
                await _saveSystem.DeleteFormationTemplate(templateId);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"TemplateBrowserOverlay: Failed to delete template: {e.Message}");
            }

            Open(); // Refresh
        }
    }
}
