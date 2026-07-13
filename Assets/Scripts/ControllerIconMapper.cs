using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Switch;
using UnityEngine.InputSystem.XInput;
using UnityEngine.UI;

/// <summary>
/// 接続中のコントローラーの種類に応じて、操作説明のアイコンを切り替える。
/// コントローラー未接続時はキーボード用のアイコンを表示する。
/// </summary>
public class ControllerIconMapper : MonoBehaviour
{
    [SerializeField] private Image iconImage = null;

    [Header("コントローラー種別ごとのアイコン")]
    [SerializeField] private Sprite switchSprite = null;
    [SerializeField] private Sprite xboxSprite = null;
    [SerializeField] private Sprite ps5Sprite = null;
    [SerializeField] private Sprite keyboardSprite = null;

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        UpdateIcon();
    }

    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        iconImage.sprite = SelectSprite(Gamepad.current);
    }

    private Sprite SelectSprite(Gamepad gamepad)
    {
        switch (gamepad)
        {
            case null:                     return keyboardSprite;
            case SwitchProControllerHID _: return switchSprite;
            case XInputController _:       return xboxSprite;
            case DualShockGamepad _:       return ps5Sprite;
            default:                       return ps5Sprite; // 不明なパッドは従来通りPS5表記
        }
    }
}
