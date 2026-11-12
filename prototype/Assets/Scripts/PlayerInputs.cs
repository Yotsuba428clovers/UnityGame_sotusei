using UnityEngine;
using UnityEngine.InputSystem;

// PC / ゲームパッド入力。Input Systemのアクションをコードで定義。
public class PlayerInputs : MonoBehaviour
{
    InputAction move, look, mouseLook, attack, heavy, dodge, special;
    readonly InputAction[] elements = new InputAction[4];
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
    public Element? SelectedElement
    {
        get
        {
            for (int i = 0; i < elements.Length; i++)
                if (elements[i].WasPressedThisFrame()) return (Element)i;
            return null;
        }
    }

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

        attack = Btn("Attack", "<Gamepad>/buttonWest", "<Mouse>/leftButton");
        heavy = Btn("Heavy", "<Gamepad>/buttonNorth", "<Mouse>/rightButton");
        dodge = Btn("Dodge", "<Gamepad>/buttonSouth", "<Keyboard>/space");
        special = Btn("Special", "<Gamepad>/rightShoulder", "<Keyboard>/f");
        elements[(int)Element.Fire] = Btn("Fire", "<Gamepad>/dpad/up", "<Keyboard>/1");
        elements[(int)Element.Wind] = Btn("Wind", "<Gamepad>/dpad/right", "<Keyboard>/2");
        elements[(int)Element.Ice] = Btn("Ice", "<Gamepad>/dpad/down", "<Keyboard>/3");
        elements[(int)Element.Lightning] = Btn("Lightning", "<Gamepad>/dpad/left", "<Keyboard>/4");
    }

    void OnEnable()
    {
        move.Enable(); look.Enable(); mouseLook.Enable(); attack.Enable(); heavy.Enable();
        dodge.Enable(); special.Enable();
        foreach (var element in elements) element.Enable();
    }

    void OnDisable()
    {
        move.Disable(); look.Disable(); mouseLook.Disable(); attack.Disable(); heavy.Disable();
        dodge.Disable(); special.Disable();
        foreach (var element in elements) element.Disable();
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
        foreach (var element in elements) element?.Dispose();
    }
}
