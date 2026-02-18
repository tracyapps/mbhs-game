using System;
using System.Collections.Generic;
using MBHS.Data.Models;
using MBHS.Systems.ContentPipeline;
using MBHS.Systems.FormationEditor.Commands;
using MBHS.Systems.MusicConductor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MBHS.Systems.FormationEditor
{
    public class TimelinePanel : VisualElement
    {
        private readonly TimelineState _state;
        private readonly IFormationSystem _formationSystem;
        private readonly IMusicConductor _musicConductor;
        private readonly CommandHistory _commandHistory;
        private readonly IContentCatalog _contentCatalog;

        private readonly TimelineRuler _ruler;
        private readonly TimelineTrack _track;
        private readonly VisualElement _trackScrollContent;
        private readonly VisualElement _transitionLane;

        // Multi-track
        private readonly VisualElement _gutterColumn;
        private VisualElement _trackScrollOuter;
        private AudioTrackLane _musicLane;
        private readonly List<AudioTrackLane> _sfxLanes = new();

        // Transport controls
        private readonly Button _btnPlayPause;
        private readonly Button _btnStop;
        private readonly Label _timeDisplay;
        private readonly Slider _zoomSlider;
        private readonly Label _zoomLabel;
        private readonly SongSelectorButton _songSelector;

        // Overlays
        private readonly SongBrowserOverlay _songBrowser;
        private readonly SfxPickerOverlay _sfxPicker;

        private bool _rulerNeedsRefresh;
        private AudioTrackLane _sfxPickerTargetLane;

        public TimelineState State => _state;

        public event Action<string> OnSongChanged;
        public event Action<float> OnPlayheadMoved;

        public TimelinePanel(
            IFormationSystem formationSystem,
            IMusicConductor musicConductor,
            CommandHistory commandHistory,
            IContentCatalog contentCatalog)
        {
            _formationSystem = formationSystem;
            _musicConductor = musicConductor;
            _commandHistory = commandHistory;
            _contentCatalog = contentCatalog;
            _state = new TimelineState();

            AddToClassList("timeline-host");

            // =================================================================
            // Transport Bar
            // =================================================================
            var transport = new VisualElement();
            transport.AddToClassList("timeline-transport");

            _btnPlayPause = new Button(OnPlayPause) { text = "Play" };
            _btnPlayPause.AddToClassList("timeline-transport-btn");
            transport.Add(_btnPlayPause);

            _btnStop = new Button(OnStop) { text = "Stop" };
            _btnStop.AddToClassList("timeline-transport-btn");
            transport.Add(_btnStop);

            var sep1 = new VisualElement();
            sep1.AddToClassList("timeline-transport-separator");
            transport.Add(sep1);

            _timeDisplay = new Label("Beat 0 / 0 | M1");
            _timeDisplay.AddToClassList("timeline-time-display");
            transport.Add(_timeDisplay);

            var sep2 = new VisualElement();
            sep2.AddToClassList("timeline-transport-separator");
            transport.Add(sep2);

            // Song selector
            _songSelector = new SongSelectorButton();
            _songSelector.OnChangeRequested += () => _songBrowser.Open();
            transport.Add(_songSelector);

            var spacer = new VisualElement();
            spacer.AddToClassList("timeline-transport-spacer");
            transport.Add(spacer);

            // Zoom controls
            var btnZoomOut = new Button(() => _state.ZoomOut()) { text = "-" };
            btnZoomOut.AddToClassList("timeline-transport-btn");
            transport.Add(btnZoomOut);

            _zoomSlider = new Slider(
                TimelineState.MinPixelsPerBeat,
                TimelineState.MaxPixelsPerBeat);
            _zoomSlider.value = _state.PixelsPerBeat;
            _zoomSlider.AddToClassList("timeline-zoom-slider");
            _zoomSlider.RegisterValueChangedCallback(evt => _state.SetZoom(evt.newValue));
            transport.Add(_zoomSlider);

            var btnZoomIn = new Button(() => _state.ZoomIn()) { text = "+" };
            btnZoomIn.AddToClassList("timeline-transport-btn");
            transport.Add(btnZoomIn);

            _zoomLabel = new Label("20px/b");
            _zoomLabel.AddToClassList("timeline-zoom-label");
            transport.Add(_zoomLabel);

            var sep3 = new VisualElement();
            sep3.AddToClassList("timeline-transport-separator");
            transport.Add(sep3);

            var btnZoomFit = new Button(OnZoomToFit) { text = "Fit" };
            btnZoomFit.AddToClassList("timeline-transport-btn");
            transport.Add(btnZoomFit);

            Add(transport);

            // =================================================================
            // Ruler
            // =================================================================
            var rulerRow = new VisualElement();
            rulerRow.AddToClassList("timeline-ruler-row");
            rulerRow.style.flexDirection = FlexDirection.Row;

            // Gutter spacer for ruler alignment
            var rulerGutterSpacer = new VisualElement();
            rulerGutterSpacer.AddToClassList("timeline-track-gutter-spacer");
            rulerRow.Add(rulerGutterSpacer);

            _ruler = new TimelineRuler(_state);
            _ruler.style.flexGrow = 1;
            _ruler.OnScrub += OnScrub;
            rulerRow.Add(_ruler);

            Add(rulerRow);

            // =================================================================
            // Multi-Track Area (gutter + scrollable tracks)
            // =================================================================
            var multiTrackArea = new VisualElement();
            multiTrackArea.AddToClassList("timeline-multi-track");

            // Music track data (created early so it can be shared with header)
            var musicTrackData = new AudioTrackData
            {
                Id = "music_main",
                Label = "Music",
                Volume = 1f
            };

            // Left gutter (track headers)
            _gutterColumn = new VisualElement();
            _gutterColumn.AddToClassList("timeline-track-gutter");

            // Formation track header
            var formationHeader = CreateTrackHeader("Formations", null, false);
            formationHeader.AddToClassList("formation-track-header");
            _gutterColumn.Add(formationHeader);

            // Transition/keyframe track header
            var transitionHeader = CreateTrackHeader("Transitions", null, false);
            transitionHeader.AddToClassList("transition-track-header");
            _gutterColumn.Add(transitionHeader);

            // Music track header (with mute/volume controls)
            var musicHeader = CreateTrackHeader("Music", musicTrackData, true);
            musicHeader.AddToClassList("audio-track-header");
            _gutterColumn.Add(musicHeader);

            // "Add SFX Track" button
            var addTrackBtn = new Button(AddSfxTrack) { text = "+ SFX Track" };
            addTrackBtn.AddToClassList("add-track-btn");
            _gutterColumn.Add(addTrackBtn);

            multiTrackArea.Add(_gutterColumn);

            // Right: scrollable track content
            _trackScrollOuter = new VisualElement();
            _trackScrollOuter.AddToClassList("timeline-track-scroll");

            _trackScrollContent = new VisualElement();
            _trackScrollContent.AddToClassList("timeline-track-scroll-content");

            // Formation track (existing TimelineTrack)
            _track = new TimelineTrack(_state, _formationSystem, _commandHistory);
            _track.Playhead.OnScrub += OnScrub;
            _track.OnScrub += OnScrub;
            _trackScrollContent.Add(_track);

            // Transition/keyframe lane (placeholder for future bezier curves)
            _transitionLane = new VisualElement();
            _transitionLane.AddToClassList("transition-lane");
            var transitionPlaceholder = new Label("Drag keyframes here to customize transitions");
            transitionPlaceholder.AddToClassList("transition-lane-placeholder");
            _transitionLane.Add(transitionPlaceholder);
            _trackScrollContent.Add(_transitionLane);

            // Music track lane
            _musicLane = new AudioTrackLane(
                _state, musicTrackData, AudioTrackType.Music,
                _formationSystem, _commandHistory);
            _trackScrollContent.Add(_musicLane);

            _trackScrollOuter.Add(_trackScrollContent);
            multiTrackArea.Add(_trackScrollOuter);

            Add(multiTrackArea);

            // =================================================================
            // Overlays (added last so they render on top)
            // =================================================================
            _songBrowser = new SongBrowserOverlay(_contentCatalog);
            _songBrowser.OnSongSelected += OnSongSelected;
            Add(_songBrowser);

            _sfxPicker = new SfxPickerOverlay();
            _sfxPicker.OnSfxSelected += OnSfxEntrySelected;
            Add(_sfxPicker);

            // =================================================================
            // State Events
            // =================================================================
            _state.OnZoomChanged += ppb =>
            {
                _zoomSlider.SetValueWithoutNotify(ppb);
                _zoomLabel.text = $"{ppb:F0}px/b";
                _rulerNeedsRefresh = true;
            };

            _state.OnScrollChanged += _ => _rulerNeedsRefresh = true;
            _state.OnPlayheadChanged += _ => UpdateTimeDisplay();
            _state.OnSongDataChanged += () =>
            {
                _rulerNeedsRefresh = true;
                _track.RebuildBlocks();
            };

            _formationSystem.OnCurrentFormationChanged += index =>
            {
                _track.SetSelectedFormation(index);
            };

            // Keyboard
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            focusable = true;

            // Initialize
            InitializeFromChart();
        }

        // =====================================================================
        // Track Header Factory
        // =====================================================================

        private VisualElement CreateTrackHeader(
            string label, AudioTrackData trackData, bool showMuteVolume)
        {
            var header = new VisualElement();
            header.AddToClassList("timeline-track-header");

            var nameLabel = new Label(label);
            nameLabel.AddToClassList("timeline-track-header-label");
            header.Add(nameLabel);

            if (showMuteVolume && trackData != null)
            {
                var controls = new VisualElement();
                controls.AddToClassList("timeline-track-header-controls");

                Button muteBtn = null;
                muteBtn = new Button(() =>
                {
                    trackData.IsMuted = !trackData.IsMuted;
                    muteBtn.EnableInClassList("muted", trackData.IsMuted);
                }) { text = "M" };
                muteBtn.AddToClassList("timeline-track-mute-btn");
                controls.Add(muteBtn);

                var volumeSlider = new Slider(0f, 1f);
                volumeSlider.value = trackData.Volume;
                volumeSlider.AddToClassList("timeline-track-volume");
                volumeSlider.RegisterValueChangedCallback(evt =>
                {
                    trackData.Volume = evt.newValue;
                });
                controls.Add(volumeSlider);

                header.Add(controls);
            }

            return header;
        }

        // =====================================================================
        // SFX Track Management
        // =====================================================================

        private void AddSfxTrack()
        {
            var chart = _formationSystem.ActiveChart;
            if (chart == null) return;

            int trackNum = chart.AudioTimeline.SfxTracks.Count + 1;
            var trackData = new AudioTrackData
            {
                Id = Guid.NewGuid().ToString(),
                Label = $"SFX {trackNum}",
                Volume = 1f
            };

            chart.AudioTimeline.SfxTracks.Add(trackData);
            CreateSfxLane(trackData);
            _state.NotifyAudioTimelineChanged();
        }

        private void CreateSfxLane(AudioTrackData trackData)
        {
            // Gutter header
            var header = CreateTrackHeader(trackData.Label, trackData, true);
            header.AddToClassList("audio-track-header");
            header.AddToClassList("sfx-track-header");

            // Add SFX button to header
            var addSfxBtn = new Button(() => OpenSfxPicker(trackData)) { text = "+" };
            addSfxBtn.AddToClassList("sfx-add-region-btn");
            header.Add(addSfxBtn);

            // Insert before the "Add SFX Track" button
            int insertIndex = _gutterColumn.childCount - 1; // before the add-track-btn
            _gutterColumn.Insert(insertIndex, header);

            // Track lane
            var lane = new AudioTrackLane(
                _state, trackData, AudioTrackType.Sfx,
                _formationSystem, _commandHistory);
            lane.OnAddRegionRequested += l => OpenSfxPicker(l.TrackData);

            _sfxLanes.Add(lane);
            _trackScrollContent.Add(lane);
        }

        private void OpenSfxPicker(AudioTrackData targetTrack)
        {
            // Find the lane for this track
            foreach (var lane in _sfxLanes)
            {
                if (lane.TrackData.Id == targetTrack.Id)
                {
                    _sfxPickerTargetLane = lane;
                    break;
                }
            }
            _sfxPicker.Open();
        }

        private void OnSfxEntrySelected(SfxCatalogEntry entry)
        {
            if (_sfxPickerTargetLane == null) return;

            float beatDuration = _state.BPM > 0
                ? entry.DurationSeconds * _state.BPM / 60f
                : 4f;

            var region = new AudioRegionData
            {
                Id = Guid.NewGuid().ToString(),
                SfxId = entry.Id,
                Label = entry.Title,
                StartBeat = _state.PlayheadBeat,
                DurationBeats = Mathf.Max(1f, Mathf.Round(beatDuration * 2f) / 2f),
                Volume = 1f
            };

            var cmd = new AddAudioRegionCommand(
                _formationSystem, _state,
                _sfxPickerTargetLane.TrackData.Id, region);
            _commandHistory.Execute(cmd);

            _sfxPickerTargetLane = null;
        }

        // =====================================================================
        // Song Selection
        // =====================================================================

        private async void OnSongSelected(string songId)
        {
            var chart = _formationSystem.ActiveChart;
            if (chart == null) return;

            string oldSongId = chart.SongId ?? "";

            var cmd = new ChangeSongCommand(
                _formationSystem, _state, oldSongId, songId);
            _commandHistory.Execute(cmd);

            // Load song data
            if (_contentCatalog != null)
            {
                try
                {
                    var songData = await _contentCatalog.LoadSongDataAsync(songId);
                    if (songData != null)
                    {
                        _state.CurrentSongTitle = songData.Title;
                        _songSelector.SetSong(songData.Title, songData.Composer);
                        LoadSongData(songData);

                        // Update music lane
                        _musicLane.SetMusicRegion(0f, songData.TotalBeats, songData.Title);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"TimelinePanel: Failed to load song data: {e.Message}");
                }
            }

            OnSongChanged?.Invoke(songId);
        }

        // =====================================================================
        // Initialization
        // =====================================================================

        private void InitializeFromChart()
        {
            var chart = _formationSystem.ActiveChart;
            if (chart == null) return;

            // Calculate total beats from formations
            float maxBeat = 64f;
            foreach (var formation in chart.Formations)
            {
                float end = formation.StartBeat + formation.DurationBeats;
                if (end > maxBeat) maxBeat = end;
            }

            _state.TotalBeats = maxBeat + 16f;
            _state.AudioTimeline = chart.AudioTimeline;

            // Restore existing SFX tracks
            foreach (var sfxTrack in chart.AudioTimeline.SfxTracks)
                CreateSfxLane(sfxTrack);

            // Restore song selector display
            if (!string.IsNullOrEmpty(chart.SongId))
                _state.CurrentSongId = chart.SongId;

            _track.RebuildBlocks();
            UpdateTimeDisplay();
        }

        public void LoadSongData(SongData songData)
        {
            if (songData == null) return;

            _state.SetSongData(songData.BPM, songData.BeatsPerMeasure, songData.TotalBeats);
            _state.CurrentSongId = songData.Id;
            _state.CurrentSongTitle = songData.Title;

            _songSelector.SetSong(songData.Title, songData.Composer);

            // Set up music region to match song duration
            var chart = _formationSystem.ActiveChart;
            if (chart != null)
            {
                chart.AudioTimeline.SongId = songData.Id;
                if (chart.AudioTimeline.SongEndBeat <= 0f)
                    chart.AudioTimeline.SongEndBeat = songData.TotalBeats;
            }

            _musicLane.SetMusicRegion(
                chart?.AudioTimeline.SongStartBeat ?? 0f,
                chart?.AudioTimeline.SongEndBeat ?? songData.TotalBeats,
                songData.Title);

            UpdateTimeDisplay();
        }

        // =====================================================================
        // Update (called from FormationEditorView.Update)
        // =====================================================================

        public void Update(float deltaTime)
        {
            if (_musicConductor != null && _musicConductor.IsPlaying)
            {
                _state.IsPlaying = true;
                _state.PlayheadBeat = _musicConductor.CurrentBeat;
                _btnPlayPause.text = "Pause";
                _btnPlayPause.AddToClassList("playing");

                float viewportWidth = _trackScrollOuter != null
                    ? _trackScrollOuter.resolvedStyle.width
                    : resolvedStyle.width - 120f;
                if (viewportWidth > 0)
                    _state.EnsureBeatVisible(_state.PlayheadBeat, viewportWidth);

                OnPlayheadMoved?.Invoke(_state.PlayheadBeat);
            }
            else
            {
                if (_state.IsPlaying)
                {
                    _state.IsPlaying = false;
                    _btnPlayPause.text = "Play";
                    _btnPlayPause.RemoveFromClassList("playing");
                }
            }

            if (_rulerNeedsRefresh)
            {
                _rulerNeedsRefresh = false;
                _ruler.RefreshLabels();
            }

            UpdateTimeDisplay();
        }

        // =====================================================================
        // Transport Controls
        // =====================================================================

        private void OnPlayPause()
        {
            if (_musicConductor == null)
            {
                Debug.LogWarning("TimelinePanel: No MusicConductor available.");
                return;
            }

            if (_musicConductor.IsPlaying)
                _musicConductor.Pause();
            else if (_musicConductor.IsPaused)
                _musicConductor.Resume();
            else
            {
                _musicConductor.SeekToBeat(_state.PlayheadBeat);
                _musicConductor.Play();
            }
        }

        private void OnStop()
        {
            if (_musicConductor != null)
                _musicConductor.Stop();

            _state.PlayheadBeat = 0f;
            _state.IsPlaying = false;
            _btnPlayPause.text = "Play";
            _btnPlayPause.RemoveFromClassList("playing");
        }

        private void OnScrub(float beat)
        {
            if (_musicConductor != null)
                _musicConductor.SeekToBeat(beat);

            var chart = _formationSystem.ActiveChart;
            if (chart != null)
            {
                int index = chart.GetFormationIndexAtBeat(beat);
                if (index >= 0 && index != _formationSystem.CurrentFormationIndex)
                    _formationSystem.SetCurrentFormation(index);
            }

            OnPlayheadMoved?.Invoke(beat);
        }

        private void OnZoomToFit()
        {
            float viewportWidth = resolvedStyle.width - 120f; // minus gutter
            if (viewportWidth > 0)
            {
                _state.ZoomToFit(viewportWidth);
                _state.ScrollOffsetBeats = 0f;
            }
        }

        // =====================================================================
        // Keyboard
        // =====================================================================

        private void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Space:
                    OnPlayPause();
                    evt.StopPropagation();
                    break;
                case KeyCode.Home:
                    OnStop();
                    evt.StopPropagation();
                    break;
            }
        }

        // =====================================================================
        // Display
        // =====================================================================

        private void UpdateTimeDisplay()
        {
            float beat = _state.PlayheadBeat;
            int measure = _state.BeatsPerMeasure > 0
                ? Mathf.FloorToInt(beat / _state.BeatsPerMeasure) + 1
                : 0;
            float beatInMeasure = _state.BeatsPerMeasure > 0
                ? beat % _state.BeatsPerMeasure
                : beat;

            _timeDisplay.text = $"Beat {beat:F1} / {_state.TotalBeats:F0} | " +
                                $"M{measure}:{beatInMeasure:F1} | " +
                                $"{_state.BPM:F0} BPM";
        }
    }
}
