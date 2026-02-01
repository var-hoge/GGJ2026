using System;
using TMPro;
using UnityEngine;

public class TimerTextField : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        timerText.text = TimeSpan.FromSeconds(GameManager.Instance.RemainTimeSecond).ToString("ss\\.ff");
    }
}
