using KanKikuchi.AudioManager;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    void Start()
    {
        BGMManager.Instance.Play(BGMPath.MUSIC_MENU_LOOP);
    }

    public void OnStart()
    {
        SceneManager.LoadScene("IntroStory");
        SEManager.Instance.Play(SEPath.UI_SELECT);
    }
}