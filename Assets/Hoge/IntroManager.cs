using UnityEngine;
using KanKikuchi.AudioManager;

public class IntroManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BGMManager.Instance.Play(BGMPath.BATTLE27);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
