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

        var main = GameObject.Find("Logo");
        main.GetComponent<RectTransform>()
            .DOAnchorPos(new(46.62352f, 54f), 3f)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public void OnStart()
    {
        SceneManager.LoadScene("IntroStory");
        SEManager.Instance.Play(SEPath.UI_SELECT);
    }
}