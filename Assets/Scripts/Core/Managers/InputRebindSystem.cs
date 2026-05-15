using System;
using UnityEngine;
using UnityEngine.InputSystem;

public static class InputRebindSystem
{
    public static InputActionRebindingExtensions.RebindingOperation RebindOperation { get; private set; }

    public static void StartRebinding(InputAction actionToRebind, int bindingIndex, Action onComplete, Action onCancel)
    {
        if (actionToRebind == null || bindingIndex < 0 || bindingIndex >= actionToRebind.bindings.Count) return;

        actionToRebind.Disable();
        RebindOperation?.Cancel();

        // groups가 null일 경우 빈 문자열로 대체하여 NullRef 방지
        string groups = actionToRebind.bindings[bindingIndex].groups ?? "";
        bool isKeyboard = string.IsNullOrEmpty(groups) || groups.Contains("Keyboard") || groups.Contains("Mouse");

        RebindOperation = actionToRebind.PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                operation.Dispose();
                ResolveConflicts(actionToRebind, bindingIndex, isKeyboard);
                actionToRebind.Enable();
                SaveOverridesToSettings();
                onComplete?.Invoke();
            })
            .OnCancel(operation =>
            {
                operation.Dispose();
                actionToRebind.Enable();
                onCancel?.Invoke();
            });

        if (isKeyboard)
        {
            // 키보드 바인딩일 경우 키보드 입력을 명시적으로 기다림
            RebindOperation.WithControlsHavingToMatchPath("<Keyboard>");
        }

        RebindOperation.Start();
    }

    private static void ResolveConflicts(InputAction targetAction, int targetBindingIndex, bool isKeyboard)
    {
        var newBinding = targetAction.bindings[targetBindingIndex];
        string targetScheme = isKeyboard ? "Keyboard" : "Gamepad";
        var allActions = InputRouter.Instance.Actions.asset;
        
        foreach (var action in allActions)
        {
            if (action == targetAction) continue;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var existingBinding = action.bindings[i];
                
                // 경로가 유효하고 서로 같을 때 충돌로 간주
                if (!string.IsNullOrEmpty(newBinding.effectivePath) && existingBinding.effectivePath == newBinding.effectivePath)
                {
                    // groups가 null일 수 있으므로 빈 문자열로 안전하게 처리
                    string groups = existingBinding.groups ?? "";
                    if (groups.Contains(targetScheme) || string.IsNullOrEmpty(groups))
                    {
                        action.ApplyBindingOverride(i, "");
                    }
                }
            }
        }
    }

    private static void SaveOverridesToSettings()
    {
        if (InputRouter.Instance == null || SettingsManager.Instance == null) return;
        string jsonOverrides = InputRouter.Instance.Actions.asset.SaveBindingOverridesAsJson();
        SettingsManager.Instance.Data.Input.KeyboardBindingOverrides = jsonOverrides;
    }

    public static void LoadOverridesFromSettings()
    {
        if (InputRouter.Instance == null || SettingsManager.Instance == null) return;
        string jsonOverrides = SettingsManager.Instance.Data.Input.KeyboardBindingOverrides;
        if (!string.IsNullOrEmpty(jsonOverrides))
        {
            InputRouter.Instance.Actions.asset.LoadBindingOverridesFromJson(jsonOverrides);
        }
    }
    
    public static string GetBindingName(InputAction action, int bindingIndex)
    {
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count) return "None";
        var displayString = action.GetBindingDisplayString(bindingIndex, out _, out _);
        return string.IsNullOrEmpty(displayString) ? "Unbound" : displayString;
    }
}
