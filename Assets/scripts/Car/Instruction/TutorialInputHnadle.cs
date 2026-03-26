using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;
using System.Text;

public class TutorialInputHandler
{
    public enum DeviceFamily
    {
        KeyboardMouse,
        Xbox,
        PlayStation
    }

    private CarInputActions actions;
    private Dictionary<string, InputAction> cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> bindingDisplayCache = new();

    public InputDevice LastUsedDevice { get; private set; }

    public TutorialInputHandler()
    {
        actions = new CarInputActions();
        CacheActions();
    }

    public void Enable()
    {
        actions.Enable();
        actions.asset.actionMaps[0].actionTriggered += OnActionTriggered;
    }

    public void Disable()
    {
        actions.asset.actionMaps[0].actionTriggered -= OnActionTriggered;
        actions.Disable();
    }

    private void OnActionTriggered(InputAction.CallbackContext ctx)
    {
        if (ctx.action?.activeControl != null)
        {
            LastUsedDevice = ctx.action.activeControl.device;
            bindingDisplayCache.Clear();
        }
    }

    private void CacheActions()
    {
        foreach (var map in actions.asset.actionMaps)
        {
            foreach (var action in map.actions)
            {
                if (!cache.ContainsKey(action.name))
                    cache[action.name] = action;
            }
        }
    }

    public void Update() { }

    public string GetBindingDisplay(string actionName, string compositePart = null)
    {
        if (string.IsNullOrWhiteSpace(actionName))
            return string.Empty;

        var family = GetCurrentDeviceFamily();
        string key = $"{family}:{actionName}:{compositePart}";
        if (bindingDisplayCache.TryGetValue(key, out var cached))
            return cached;

        if (!cache.TryGetValue(actionName, out var action) || action == null)
            return string.Empty;

        var devicePreferences = new List<string>();
        if (family == DeviceFamily.PlayStation || family == DeviceFamily.Xbox)
            devicePreferences.Add("Gamepad");
        else
            devicePreferences.Add("Keyboard");

        foreach (var deviceType in devicePreferences)
        {
            string display = FindBindingDisplay(action, deviceType, compositePart);
            if (!string.IsNullOrEmpty(display))
            {
                bindingDisplayCache[key] = display;
                return display;
            }
        }

        string fallback = FindBindingDisplay(action, null, compositePart);
        string result = string.IsNullOrEmpty(fallback) ? string.Empty : fallback;
        bindingDisplayCache[key] = result;
        return result;
    }

    public DeviceFamily GetCurrentDeviceFamily()
    {
        var device = LastUsedDevice;
        if (device is Gamepad)
        {
            if (DeviceNameContains(device, "playstation") || DeviceNameContains(device, "wireless controller"))
                return DeviceFamily.PlayStation;

            return DeviceFamily.Xbox;
        }

        return DeviceFamily.KeyboardMouse;
    }

    public IEnumerable<string> GetSpriteLookupCandidates(string actionName, string compositePart = null)
    {
        string display = GetBindingDisplay(actionName, compositePart);
        if (string.IsNullOrEmpty(display))
            yield break;

        string normalized = NormalizeSpriteName(display);
        if (string.IsNullOrEmpty(normalized))
            yield break;

        yield return normalized;

        string aliased = NormalizeBindingAlias(normalized);
        if (!string.Equals(aliased, normalized, StringComparison.Ordinal))
            yield return aliased;

        if (ShouldCollapseLeftRight(aliased))
        {
            string noSide = RemoveSidePrefix(aliased);
            if (!string.Equals(noSide, aliased, StringComparison.Ordinal))
                yield return noSide;
        }
    }

    private string FindBindingDisplay(InputAction action, string deviceType, string compositePart)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];

            if (!string.IsNullOrEmpty(compositePart))
            {
                if (!binding.isPartOfComposite || !binding.name.Equals(compositePart, StringComparison.OrdinalIgnoreCase))
                    continue;
            }
            else if (binding.isPartOfComposite)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(deviceType)
                && !binding.path.Contains(deviceType, StringComparison.OrdinalIgnoreCase))
                continue;

            return InputControlPath.ToHumanReadableString(
                binding.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        return string.Empty;
    }

    public bool IsPressed(string name)
        => TryGetAction(name, out var a) && a.IsPressed();

    public bool WasPressed(string name)
        => TryGetAction(name, out var a) && a.WasPerformedThisFrame();

    public bool CheckCombo(string[] actionsList)
    {
        foreach (var name in actionsList)
            if (!IsPressed(name)) return false;

        return true;
    }

    public bool CheckDirection(string name, Vector2 dir)
    {
        if (!TryGetAction(name, out var action)) return false;

        var input = action.ReadValue<Vector2>();
        return input.sqrMagnitude > 0.2f &&
               Vector2.Dot(input.normalized, dir.normalized) > 0.7f;
    }

    private bool TryGetAction(string name, out InputAction action)
    {
        action = null;
        if (string.IsNullOrWhiteSpace(name)) return false;

        if (cache.TryGetValue(name, out action) && action != null)
            return true;

        foreach (var map in actions.asset.actionMaps)
        {
            foreach (var candidate in map.actions)
            {
                if (!string.Equals(candidate.name, name, StringComparison.OrdinalIgnoreCase))
                    continue;

                cache[name] = candidate;
                action = candidate;
                return true;
            }
        }

        return false;
    }

    private string NormalizeBindingAlias(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        string normalized = value;
        normalized = normalized.Replace("ctrl", "control", StringComparison.Ordinal);
        normalized = normalized.Replace("return", "enter", StringComparison.Ordinal);
        normalized = normalized.Replace("escape", "esc", StringComparison.Ordinal);
        normalized = normalized.Replace("spacebar", "space", StringComparison.Ordinal);
        return normalized;
    }

    private bool ShouldCollapseLeftRight(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        return value.EndsWith("shift", StringComparison.Ordinal)
            || value.EndsWith("control", StringComparison.Ordinal)
            || value.EndsWith("alt", StringComparison.Ordinal)
            || value.EndsWith("meta", StringComparison.Ordinal)
            || value.EndsWith("command", StringComparison.Ordinal)
            || value.EndsWith("windows", StringComparison.Ordinal);
    }

    private string RemoveSidePrefix(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        if (value.StartsWith("left", StringComparison.Ordinal) && value.Length > 4)
            return value.Substring(4);

        if (value.StartsWith("right", StringComparison.Ordinal) && value.Length > 5)
            return value.Substring(5);

        return value;
    }

    private string NormalizeSpriteName(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
        }
        return builder.ToString();
    }

    private bool DeviceNameContains(InputDevice device, string value)
    {
        if (device == null || string.IsNullOrEmpty(value)) return false;

        string displayName = device.displayName ?? string.Empty;
        string productName = device.description.product ?? string.Empty;

        return displayName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0
            || productName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}