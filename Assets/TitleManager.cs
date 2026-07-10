using DG.Tweening;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    [SerializeField] GameObject _startButton;

    void Start()
    {
        Application.targetFrameRate = 30;
        BGMManager.Instance.Play(BGMPath.MUSIC_MENU_LOOP);
    }

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
            EventSystem.current.SetSelectedGameObject(_startButton);
            return;
        }

        // スペースキーでも決定できるようにする (標準の Submit は Enter のみ)
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ExecuteEvents.Execute(selected, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
        }
    }

    public void OnStart()
    {
        SceneManager.LoadScene("IntroStory");
        SEManager.Instance.Play(SEPath.UI_SELECT);
    }

    public void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}