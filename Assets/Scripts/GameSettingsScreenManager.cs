using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームモード選択画面の遷移とキー操作を管理する。
/// </summary>
public class GameSettingsScreenManager : MonoBehaviour
{
    [SerializeField] GameObject _defaultSelectedButton;

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

    public void OnSolo()
    {
        LoadScene("IntroStory");
    }

    public void OnLocalMultiplay()
    {
        LoadScene("LocalMultiplayerScreen");
    }

    public void OnOnlineMultiplay()
    {
        LoadScene("OnlineScreen");
    }

    public void OnBack()
    {
        LoadScene("Title");
    }

    void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
        SEManager.Instance.Play(SEPath.UI_SELECT);
        GamepadRumble.Play(_submitRumbleStrength, _submitRumbleDuration);
    }
}
