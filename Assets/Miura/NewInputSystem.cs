using UnityEngine;
[DefaultExecutionOrder(-100)]
public class NewInputSystem : MonoBehaviour
{
    public static NewInputSystem Instance;
    public InputSystem_Actions InputAction;
    void Awake() //全スクリプトの中で一番早くに走るAwake
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InputAction = new InputSystem_Actions();
        InputAction.Enable();
    }

    void OnDestroy()
    {
        if (InputAction != null)
        {
            InputAction.Disable();
            InputAction.Dispose();
            InputAction = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
