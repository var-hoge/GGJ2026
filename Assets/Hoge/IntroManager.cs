using UnityEngine;
using KanKikuchi.AudioManager;

public class IntroManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BGMManager.Instance.Play(BGMPath.MUSIC_GAME_LOOP);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
