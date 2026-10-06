using UnityEngine;
using UnityEngine.InputSystem;

// コントローラー入力(キーボードはテスト用)。Input Systemのアクションをコードで定義するので設定ファイル不要。
// X:通常攻撃 Y:強攻撃 A:回避 B:必殺技 LB/RB:属性切り替え
// キーボード: WASD移動 / 矢印キー視点 / J,K,Space,L / Q,E
public class PlayerInputs : MonoBehaviour
{
    InputAction move, look, attack, heavy, dodge, special, next, prev;

    public Vector2 Move => move.ReadValue<Vector2>();
    public Vector2 Look => look.ReadValue<Vector2>();
    public bool Attack => attack.WasPressedThisFrame();
    public bool Heavy => heavy.WasPressedThisFrame();
    public bool Dodge => dodge.WasPressedThisFrame();
    public bool Special => special.WasPressedThisFrame();
    public bool NextElement => next.WasPressedThisFrame();
    public bool PrevElement => prev.WasPressedThisFrame();

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
        look.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

        attack = Btn("Attack", "<Gamepad>/buttonWest", "<Keyboard>/j");
        heavy = Btn("Heavy", "<Gamepad>/buttonNorth", "<Keyboard>/k");
        dodge = Btn("Dodge", "<Gamepad>/buttonSouth", "<Keyboard>/space");
        special = Btn("Special", "<Gamepad>/buttonEast", "<Keyboard>/l");
        next = Btn("NextElement", "<Gamepad>/rightShoulder", "<Keyboard>/e");
        prev = Btn("PrevElement", "<Gamepad>/leftShoulder", "<Keyboard>/q");
    }

    void OnEnable()
    {
        move.Enable(); look.Enable(); attack.Enable(); heavy.Enable();
        dodge.Enable(); special.Enable(); next.Enable(); prev.Enable();
    }

    void OnDisable()
    {
        move.Disable(); look.Disable(); attack.Disable(); heavy.Disable();
        dodge.Disable(); special.Disable(); next.Disable(); prev.Disable();
    }
}
