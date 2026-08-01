using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// キャラクター選択画面の遷移とキー操作を管理する。
/// </summary>
public class CharacterSelectionScreenManager : MonoBehaviour
{
    [SerializeField] GameObject _defaultSelectedButton;

    [Header("戻る先")]
    [Tooltip("直前の画面が分からないとき (このシーンを直接再生したときなど) に戻る画面")]
    [SerializeField] string _fallbackBackScene = "LocalMultiplayerScreen";

    [Header("決定したときのコントローラーの振動")]
    [SerializeField, Range(0f, 1f)] float _submitRumbleStrength = 0.3f;
    [SerializeField] float _submitRumbleDuration = 0.1f;

    void Update()
    {
        if (EventSystem.current == null)
        {
            return;
        }

        // 背景クリック等で選択が外れてもパッド操作が効くように復帰させる
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
        {
            EventSystem.current.SetSelectedGameObject(_defaultSelectedButton);
            return;
        }

        // スペースキーでも決定できるようにする (標準の Submit は Enter のみ)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }

    public void OnSelectPhantomCat()
    {
        SelectCharacter(PlayableCharacter.PhantomCat);
    }

    public void OnSelectPoliceDog()
    {
        SelectCharacter(PlayableCharacter.PoliceDog);
    }

    /// <summary>選んだキャラクターは後続の画面から参照されるので記録してから次へ進む。</summary>
    void SelectCharacter(PlayableCharacter character)
    {
        CharacterSelection.Select(character);
        LoadScene("IntroStory");
    }

    /// <summary>
    /// この画面はローカル対戦とオンライン対戦の両方から来るので、遷移元の画面に戻る。
    /// </summary>
    public void OnBack()
    {
        var previous = ScreenHistory.PreviousSceneName;
        LoadScene(string.IsNullOrEmpty(previous) ? _fallbackBackScene : previous);
    }

    void LoadScene(string sceneName)
    {
        ScreenHistory.LoadScene(sceneName);
        SEManager.Instance.Play(SEPath.UI_SELECT);
        GamepadRumble.Play(_submitRumbleStrength, _submitRumbleDuration);
    }
}
