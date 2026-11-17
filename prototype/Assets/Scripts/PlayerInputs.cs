using UnityEngine;
using UnityEngine.InputSystem;

// PC / ゲームパッド入力。Input Systemのアクションをコードで定義。
public class PlayerInputs : MonoBehaviour
{
    InputAction move, look, mouseLook, attack, heavy, dodge, special, next, previous;
    bool ownsCursor;
    int mouseCaptureFrame=-100;

    public Vector2 Move => move.ReadValue<Vector2>();
    public Vector2 Look => look.ReadValue<Vector2>();
    // Mouse delta is pixels per frame. Keep it separate from stick velocity.
    public Vector2 MouseLook => ownsCursor && Cursor.lockState==CursorLockMode.Locked &&
        Time.frameCount>mouseCaptureFrame+1 ? mouseLook.ReadValue<Vector2>() : Vector2.zero;
    public bool Attack => attack.WasPressedThisFrame();
    public bool Heavy => heavy.WasPressedThisFrame();
    public bool Dodge => dodge.WasPressedThisFrame();
    public bool Special => special.WasPressedThisFrame();
    public int ElementStep => (next.WasPressedThisFrame() ? 1 : 0) - (previous.WasPressedThisFrame() ? 1 : 0);

    static InputAction Btn(string name, string pad, string key)
    {
        var a = new InputAction(name, InputActionType.Button);
        a.AddBinding(pad);
        a.AddBinding(key);
        return a;
    }

    void Awake()
    {
        move = new InputAction("Move", InputActionType.Value);
        move.AddBinding("<Gamepad>/leftStick");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

        look = new InputAction("Look", InputActionType.Value);
        look.AddBinding("<Gamepad>/rightStick");
        mouseLook=new InputAction("Mouse Look",InputActionType.PassThrough,"<Mouse>/delta");

        attack = Btn("Attack", "<Gamepad>/buttonWest", "<Keyboard>/f");
        heavy = Btn("Heavy", "<Gamepad>/buttonNorth", "<Keyboard>/g");
        dodge = Btn("Dodge", "<Gamepad>/buttonSouth", "<Keyboard>/space");
        special = Btn("Special", "<Gamepad>/buttonEast", "<Keyboard>/z");
        next = Btn("Next element", "<Gamepad>/rightShoulder", "<Keyboard>/e");
        previous = Btn("Previous element", "<Gamepad>/leftShoulder", "<Keyboard>/q");
    }

    void OnEnable()
    {
        move.Enable(); look.Enable(); mouseLook.Enable(); attack.Enable(); heavy.Enable();
        dodge.Enable(); special.Enable();
        next.Enable(); previous.Enable();
    }

    void OnDisable()
    {
        move.Disable(); look.Disable(); mouseLook.Disable(); attack.Disable(); heavy.Disable();
        dodge.Disable(); special.Disable();
        next.Disable(); previous.Disable();
        ReleaseCursor();
    }

    void Update()
    {
        var game=GameManager.Instance;
        bool capture=Application.isFocused && Mouse.current!=null && game!=null &&
            game.State==GameManager.GameState.Playing && !SettingsMenu.BlocksGameplay;
        if(!capture){ReleaseCursor();return;}
        if(!ownsCursor || Cursor.lockState!=CursorLockMode.Locked)
        {
            Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
            ownsCursor=true;mouseCaptureFrame=Time.frameCount;
        }
    }
    void ReleaseCursor()
    {
        if(!ownsCursor)return;
        Cursor.lockState=CursorLockMode.None;Cursor.visible=true;ownsCursor=false;
    }
    void OnApplicationFocus(bool focus) { if(!focus)ReleaseCursor(); }
    void OnDestroy()
    {
        move?.Dispose();look?.Dispose();mouseLook?.Dispose();attack?.Dispose();heavy?.Dispose();
        dodge?.Dispose();special?.Dispose();
        next?.Dispose(); previous?.Dispose();
    }
}
