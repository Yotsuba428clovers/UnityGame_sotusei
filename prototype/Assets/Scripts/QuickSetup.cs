using UnityEngine;

// 空のシーンの空オブジェクトにこれを付けて再生するだけで、
// 地面・プレイヤー・カメラ・敵スポナー・HUDを自動生成する。
// 自作モデルを使う場合は playerModelPrefab / enemyPrefab / bossPrefab を割り当てる。
public class QuickSetup : MonoBehaviour
{
    public GameObject playerModelPrefab;
    public GameObject enemyPrefab;
    public GameObject bossPrefab;
    public GameObject[] additionalEnemyPrefabs;
    public bool enhanceBattlePresentation=true;
    public bool firstPersonView = true;
    public int startingEnemies=6;
    public int maxAliveEnemies=18;
    [Header("戦闘サウンド（未設定なら合成音）")]
    public AudioClip swingSound, hitSound, warningSound, slamSound, specialSound;
    [Range(0,1)] public float combatSoundVolume=.35f;
    public bool createPlaceholderSword = true;
    public int killsToBoss = 50;

    [Header("エフェクト(Epic Toon FXのPrefabを割り当て)")]
    public ElementFx[] elementFx = new ElementFx[]
    {
        new ElementFx { name = "Fire (炎)" },
        new ElementFx { name = "Ice (氷)" },
        new ElementFx { name = "Lightning (雷)" },
        new ElementFx { name = "Wind (風)" }
    };
    public GameObject enemyDeathFx;
    public float fxScale = 1f;
    [Header("UI")]
    public Font hudFont;   // HUDとダメージ数字のフォント
    public Vector3 fxRotation = new Vector3(0f, 0f, 90f);
    public Vector3 specialRotation = new Vector3(0f, 0f, 90f);
    public Vector3 switchRotation = new Vector3(0f, 0f, 90f);

    void Awake()
    {
        // ライト
        if (FindFirstObjectByType<Light>() == null)
        {
            var l = new GameObject("Directional Light").AddComponent<Light>();
            l.type = LightType.Directional;
            l.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        // 地面(300m四方)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(30f, 1f, 30f);
        ground.GetComponent<Renderer>().material.color = new Color(0.22f, 0.25f, 0.3f);

        // プレイヤー(足元が原点)
        var player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0.1f, 0f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = Vector3.up;
        var controller = player.AddComponent<PlayerController>();
        var combat = player.AddComponent<PlayerCombat>();
        combat.elementFx = elementFx;
        combat.enemyDeathFx = enemyDeathFx;
        combat.fxScale = fxScale;
        combat.fxRotation = fxRotation;
        combat.specialRotation = specialRotation;
        combat.switchRotation = switchRotation;

        if (playerModelPrefab != null)
        {
            Instantiate(playerModelPrefab, player.transform);
        }
        else
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = Vector3.up;
            body.GetComponent<Renderer>().material.color = new Color(0.85f, 0.9f, 1f);
        }

        if (createPlaceholderSword)
        {
            var sword = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sword.name = "MagicSword";
            Destroy(sword.GetComponent<Collider>());
            sword.transform.SetParent(player.transform, false);
            sword.transform.localPosition = new Vector3(0.55f, 1.1f, 0.7f);
            sword.transform.localScale = new Vector3(0.12f, 0.12f, 1.4f);
            combat.swordRenderers = new[] { sword.GetComponent<Renderer>() };
            combat.ApplySwordColor();
        }

        // カメラ
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
        }
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        follow.firstPerson = firstPersonView;
        follow.target = player.transform;
        controller.cameraTransform = cam.transform;

        // ゲーム管理
        var sys = new GameObject("GameSystems");
        var presentation=sys.AddComponent<CombatPresentation>();
        presentation.swingSound=swingSound;presentation.hitSound=hitSound;
        presentation.warningSound=warningSound;presentation.slamSound=slamSound;
        presentation.specialSound=specialSound;presentation.soundVolume=combatSoundVolume;
        if(enhanceBattlePresentation)sys.AddComponent<BattleEnvironment>();
        var gm = sys.AddComponent<GameManager>();
        gm.killsToBoss = killsToBoss;
        var spawner = sys.AddComponent<EnemySpawner>();
        spawner.player = player.transform;
        spawner.enemyPrefab = enemyPrefab;
        spawner.bossPrefab = bossPrefab;
        spawner.additionalEnemyPrefabs=additionalEnemyPrefabs;
        spawner.startingEnemies=startingEnemies;
        spawner.maxAlive=maxAliveEnemies;
        spawner.spawnInterval=1.2f;
        spawner.batchSize=2;
        var hud = sys.AddComponent<HUD>();
        sys.AddComponent<SettingsMenu>();
        hud.font = hudFont;
        DamagePopup.CustomFont = hudFont;
    }
}
