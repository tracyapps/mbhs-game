using System;
using MBHS.Data.Enums;
using UnityEngine;

namespace MBHS.Systems.CameraSystem
{
    public interface ICameraDirector
    {
        CameraMode ActiveMode { get; }

        void SetMode(CameraMode mode);
        void SetFirstPersonTarget(string memberId);
        void SetFocusPoint(Vector3 worldPosition);
        void ResetToDefault();

        event Action<CameraMode> OnModeChanged;
    }
}
