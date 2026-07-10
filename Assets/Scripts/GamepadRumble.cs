using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// コントローラーを一定時間だけ振動させる。
/// 停止用のコルーチンはシーンをまたいで生き残る専用オブジェクトで回すため、
/// 振動中にシーンが切り替わっても指定時間で必ず止まる。
/// </summary>
public static class GamepadRumble
{
    private static Runner runner;
    private static Gamepad rumblingGamepad;
    private static Coroutine stopCoroutine;

    /// <summary>
    /// strength の強さで duration 秒だけ振動させる。
    /// コントローラーが繋がっていなければ何もしない。
    /// </summary>
    public static void Play(float strength, float duration)
    {
        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        if (runner == null)
        {
            var go = new GameObject(nameof(GamepadRumble));
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }

        if (stopCoroutine != null)
            runner.StopCoroutine(stopCoroutine);
        Stop();

        rumblingGamepad = gamepad;
        gamepad.SetMotorSpeeds(strength, strength);
        stopCoroutine = runner.StartCoroutine(StopAfter(duration));
    }

    /// <summary>振動中であれば止める。</summary>
    public static void Stop()
    {
        if (rumblingGamepad == null) return;

        rumblingGamepad.ResetHaptics();
        rumblingGamepad = null;
    }

    private static IEnumerator StopAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        Stop();
    }

    private class Runner : MonoBehaviour { }
}
