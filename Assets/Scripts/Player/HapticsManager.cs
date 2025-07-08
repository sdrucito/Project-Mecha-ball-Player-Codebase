using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HapticsManager : Singleton<HapticsManager>
{
    [SerializeField] private string ResourceFolderPath = "HapticPresets";
    [SerializeField] private bool HapticsEnabled = true;

    private Dictionary<string, HapticPreset> _presetMap;
    private Coroutine _currentVibration;
    private int _currentPriority = int.MinValue;
    private Gamepad _gamepad;

    protected override void Awake()
    {
        base.Awake();

        _presetMap = new Dictionary<string, HapticPreset>();
        var loadedPresets = Resources.LoadAll<HapticPreset>(ResourceFolderPath);
        foreach (var preset in loadedPresets)
        {
            if (!_presetMap.TryAdd(preset.IdName, preset))
            {
                Debug.LogWarning($"Duplicate haptic preset ID: {preset.IdName}");
            }
        }
        //Debug.Log("Loaded haptic presets: " + _presetMap.Count);
    }

    public void Play(string id)
    {
        if (_presetMap.TryGetValue(id, out var preset))
        {
            Play(preset);
        }
        else
        {
            Debug.LogWarning($"HapticPreset not found: {id}");
        }
    }

    public void Play(HapticPreset preset)
    {
        if (!HapticsEnabled || !IsControllerActive())
            return;

        if (_currentVibration != null && preset.Priority < _currentPriority)
            return;

        if (_currentVibration != null)
            StopCoroutine(_currentVibration);

        _currentVibration = StartCoroutine(VibrateCoroutine(preset));
    }

    private IEnumerator VibrateCoroutine(HapticPreset preset)
    {
        _currentPriority = preset.Priority;

        _gamepad = Gamepad.current;
        if (_gamepad != null)
        {
            _gamepad.SetMotorSpeeds(preset.LowFrequency, preset.HighFrequency);
            yield return new WaitForSecondsRealtime(preset.Duration);
            _gamepad.SetMotorSpeeds(0f, 0f);
        }

        _currentPriority = int.MinValue;
        _currentVibration = null;
    }

    private void Stop()
    {
        if (_gamepad != null)
        {
            _gamepad.SetMotorSpeeds(0f, 0f);
        }
        if (_currentVibration != null)
        {
            StopCoroutine(_currentVibration);
            _currentVibration = null;
        }
        _currentPriority = int.MinValue;
    }

    private static bool IsControllerActive()
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) return false;

        var keyboard = Keyboard.current;
        return keyboard == null || keyboard.lastUpdateTime < gamepad.lastUpdateTime;
    }

    public void SetHapticsEnabled(bool isEnabled)
    {
        HapticsEnabled = isEnabled;
        if (!isEnabled) Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }
}
