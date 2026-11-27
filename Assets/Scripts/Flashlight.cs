using UnityEngine;
using UnityEngine.InputSystem;

public class Flashlight : MonoBehaviour
{
    [SerializeField] Light flashlight;

    void Update()
    {
        if (Keyboard.current.fKey.wasPressedThisFrame)
            flashlight.enabled = !flashlight.enabled;
    }
}