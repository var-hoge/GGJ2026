using TMPro;
using UnityEngine;

public class RoomTextField : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;

    public void SetTitleText(string text)
    {
        titleText.text = text;
    }

}
