using UnityEngine;
[DefaultExecutionOrder(-100)]
public class NewInputSystem : MonoBehaviour
{
    public static NewInputSystem Instance;
    public InputSystem_Actions InputAction;
    void Awake() //全スクリプトの中で一番早くに走るAwake
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        InputAction = new InputSystem_Actions();
        InputAction.Enable();
    }
}
