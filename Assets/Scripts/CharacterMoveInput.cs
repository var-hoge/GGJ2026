using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 操作キャラクターの移動入力。犬と猫のどちらを操作しても同じ操作方法になるよう、両方から使う。
/// </summary>
public static class CharacterMoveInput
{
    static readonly (KeyCode keyCode, Vector3 direction)[] KeyDirections =
    {
        (KeyCode.UpArrow, Vector3.up),
        (KeyCode.LeftArrow, Vector3.left),
        (KeyCode.DownArrow, Vector3.down),
        (KeyCode.RightArrow, Vector3.right),
    };

    /// <summary>
    /// 移動方向を返す。入力が無ければゼロ。
    /// ゲームパッドの左スティックを優先し、無ければ矢印キーを見る。
    /// </summary>
    public static Vector3 Read()
    {
        if (Gamepad.current != null)
        {
            var stick = Gamepad.current.leftStick.ReadValue();
            if (!stick.Equals(Vector2.zero))
            {
                return stick;
            }
        }

        var keyboard = Vector3.zero;
        foreach (var entry in KeyDirections)
        {
            if (Input.GetKey(entry.keyCode))
            {
                keyboard += entry.direction;
            }
        }

        // 斜め入力で速くならないように正規化する
        return keyboard == Vector3.zero ? Vector3.zero : keyboard.normalized;
    }
}
