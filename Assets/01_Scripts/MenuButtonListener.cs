using UnityEngine;
using UnityEngine.InputSystem;

// Listens for the "Menu" button press on the VR controller (configured in
// the XRI Default Input Actions asset) and toggles the pause menu.
public class MenuButtonListener : MonoBehaviour
{
    [Tooltip("Drag the 'Menu' Input Action Reference here (from XRI Default Input Actions).")]
    public InputActionReference menuAction;

    [Tooltip("Drag the GameObject that has PauseMenuController here.")]
    public PauseMenuController pauseMenuController;

    private void OnEnable()
    {
        if (menuAction != null)
        {
            menuAction.action.Enable();
            menuAction.action.performed += OnMenuButtonPressed;
        }
    }

    private void OnDisable()
    {
        if (menuAction != null && menuAction.action != null)
        {
            menuAction.action.performed -= OnMenuButtonPressed;
        }
    }

    private void OnMenuButtonPressed(InputAction.CallbackContext context)
    {
        pauseMenuController.TogglePauseMenu();
    }
}