using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleUI : MonoBehaviour
{
    public void OnStart()
    {
        SceneManager.LoadScene("StoryBase");
    }
}