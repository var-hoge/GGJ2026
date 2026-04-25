using DG.Tweening;
using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = 30;
        BGMManager.Instance.Play(BGMPath.MUSIC_MENU_LOOP);
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