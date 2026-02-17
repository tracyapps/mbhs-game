using System;
using System.Collections.Generic;
using MBHS.Data.Enums;

namespace MBHS.Data.Models
{
    [Serializable]
    public class SongData
    {
        public string Id;
        public string Title;
        public string Composer;
        public string Arranger;
        public float BPM;
        public int BeatsPerMeasure; // time signature numerator
        public int BeatUnit;        // time signature denominator
        public float TotalBeats;
        public float DurationSeconds;
        public int Difficulty; // 1-10
        public List<TempoChange> TempoChanges = new();

        // Addressable keys for audio assets
        public string FullMixAddressableKey;
        public List<StemReference> Stems = new();

        public float GetBPMAtBeat(float beat)
        {
            float currentBPM = BPM;

            foreach (var change in TempoChanges)
            {
                if (beat >= change.AtBeat + change.TransitionBeats)
                {
                    currentBPM = change.NewBPM;
                }
                else if (beat >= change.AtBeat)
                {
                    float progress = (beat - change.AtBeat) / change.TransitionBeats;
                    float previousBPM = currentBPM;
                    currentBPM = previousBPM + (change.NewBPM - previousBPM) * progress;
                    break;
                }
            }

            return currentBPM;
        }
    }

    [Serializable]
    public class StemReference
    {
        public InstrumentFamily Family;
        public string AddressableKey;
    }

    [Serializable]
    public class TempoChange
    {
        public float AtBeat;
        public float NewBPM;
        public float TransitionBeats; // 0 = instant change
    }
}
