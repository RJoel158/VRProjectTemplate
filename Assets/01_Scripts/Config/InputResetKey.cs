using UnityEngine;
using UnityEngine.InputSystem;

public class InputResetKey : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (VRInputPersistenceManager.Instance != null)
                VRInputPersistenceManager.Instance.CollectAndEnableInputAssets();

            Debug.Log("Input actions re-enabled");
        }
    }
}