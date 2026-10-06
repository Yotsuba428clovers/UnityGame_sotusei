using UnityEngine;
using UnityEngine.InputSystem;

public class InputDebug : MonoBehaviour
{
    void OnGUI()
    {
        GUI.skin.label.fontSize = 22;
        var pad = Gamepad.current;
        string s = "Gamepad: " + (pad != null ? pad.displayName : "NONE") + "\n";
        s += "Joystick: " + (Joystick.current != null ? Joystick.current.displayName : "NONE") + "\n";
        s += "Devices: " + InputSystem.devices.Count + "\n";
        if (pad != null)
            s += "LeftStick: " + pad.leftStick.ReadValue() + "   A: " + pad.buttonSouth.isPressed;
        GUI.Label(new Rect(20, 120, 900, 200), s);
    }
}
