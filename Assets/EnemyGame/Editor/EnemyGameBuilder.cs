using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One-time setup tool: imports the art, creates all prefabs and the scenes
// Menu, Level1, Level2, Random. After it has run, everything lives in scenes/prefabs
// and this file can be deleted.
public static class EnemyGameBuilder
{
    private const string Root = "Assets/EnemyGame";
    private const string ArtDir = Root + "/Art/";
    private const string AudioDir = Root + "/Audio/";
    private const string PrefabDir = Root + "/Prefabs/";
    private const string SceneDir = Root + "/Scenes/";
    private const string GenDir = Root + "/Generated/";

    private static Material mat;
    private static Font font;
    private static int wallLayer;
    private static LayerMask wallMask;

    // prefabs
    private static GameObject pSlash, pEnemySlash, pMuzzle, pSpark, pDust, pDeath, pImpact, pPopup;
    private static GameObject pBullet, pWall, pCrate, pBarrel, pExit, pPlayer, pPatroller, pShooter, pCharger;

    [MenuItem("Tools/Enemy Game/Build Everything")]
    public static void BuildAll()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Directory.CreateDirectory(PrefabDir);
        Directory.CreateDirectory(PrefabDir + "FX");
        Directory.CreateDirectory(SceneDir);
        Directory.CreateDirectory(GenDir);

        wallLayer = EnsureLayer("Walls");
        wallMask = 1 << wallLayer;
        ImportArt();
        mat = MakeMaterial();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildFxPrefabs();
        BuildWorldPrefabs();
        BuildCharacterPrefabs();

        BuildMenuScene();
        BuildLevel1();
        BuildLevel2();
        BuildRandomScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(SceneDir + "Menu.unity", true),
            new EditorBuildSettingsScene(SceneDir + "Level1.unity", true),
            new EditorBuildSettingsScene(SceneDir + "Level2.unity", true),
            new EditorBuildSettingsScene(SceneDir + "Random.unity", true),
        };
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(SceneDir + "Menu.unity");
        Debug.Log("EnemyGameBuilder: done. Scenes are in " + SceneDir);
    }

    // =====================================================================  ART
    private static void ImportArt()
    {
        foreach (string file in Directory.GetFiles(ArtDir, "*.png", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/');
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) continue;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = 32f;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spritePixelsPerUnit = 32f;
            if (path.Contains("/UI/")) settings.spriteBorder = path.EndsWith("panel.png") ? new Vector4(10, 10, 10, 10) : Vector4.zero;
            imp.SetTextureSettings(settings);
            imp.wrapMode = path.Contains("/Tiles/") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
    }

    private static Sprite S(string rel) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + rel + ".png");
    private static Sprite[] Frames(string rel, int count)
    {
        var list = new Sprite[count];
        for (int i = 0; i < count; i++) list[i] = S(rel + "_" + i);
        return list;
    }
    private static AudioClip A(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + name + ".wav");

    private static Material MakeMaterial()
    {
        string path = GenDir + "SpriteUnlit.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m != null) return m;
        Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        m = new Material(sh);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    // =====================================================================  PREFABS
    private static void BuildFxPrefabs()
    {
        pSlash = FxPrefab("Slash", Frames("FX/slash", 4), 22f, 1f);
        pEnemySlash = FxPrefab("EnemySlash", Frames("FX/enemyslash", 4), 22f, 1.1f);
        pMuzzle = FxPrefab("MuzzleFlash", Frames("FX/muzzle", 3), 24f, 1.2f);
        pSpark = FxPrefab("HitSpark", Frames("FX/spark", 4), 20f, 1.4f);
        pImpact = FxPrefab("Impact", Frames("FX/spark", 4), 24f, 0.8f);
        pDust = FxPrefab("Dust", Frames("FX/dust", 3), 12f, 1f, 2);
        pDeath = FxPrefab("DeathPuff", Frames("FX/dust", 3), 8f, 2.5f);

        var go = new GameObject("Popup");
        var tm = go.AddComponent<TextMesh>();
        tm.font = font;
        tm.fontSize = 48;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Color.yellow;
        tm.text = "В спину!";
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = 60;
        var popup = go.AddComponent<FloatingPopup>();
        Set(popup, "text", tm);
        pPopup = SavePrefab(go, "FX/Popup");
    }

    private static GameObject FxPrefab(string name, Sprite[] frames, float fps, float scale, int order = 15)
    {
        var go = new GameObject(name);
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sharedMaterial = mat;
        sr.sortingOrder = order;
        var anim = go.AddComponent<SpriteFrameAnimation>();
        Set(anim, "spriteRenderer", sr);
        Set(anim, "frames", frames);
        Set(anim, "fps", fps);
        return SavePrefab(go, "FX/" + name);
    }

    private static void BuildWorldPrefabs()
    {
        // Bullet
        var b = new GameObject("Bullet");
        var rb = b.AddComponent<Rigidbody2D>();
        SetupBody(rb, 0.1f);
        var bc = b.AddComponent<CircleCollider2D>();
        bc.radius = 0.12f;
        bc.isTrigger = true;
        Child(b.transform, "Sprite", S("FX/bullet"), Vector2.zero, 1.6f, 12);
        var proj = b.AddComponent<Projectile>();
        Set(proj, "wallMask", wallMask);
        Set(proj, "impactEffect", pImpact);
        pBullet = SavePrefab(b, "Bullet");

        // Wall (tiled - stretch it with Size in the SpriteRenderer)
        var w = new GameObject("Wall");
        w.layer = wallLayer;
        var wsr = w.AddComponent<SpriteRenderer>();
        wsr.sprite = S("Tiles/wall");
        wsr.drawMode = SpriteDrawMode.Tiled;
        wsr.size = Vector2.one;
        wsr.sharedMaterial = mat;
        wsr.sortingOrder = 0;
        w.AddComponent<BoxCollider2D>().size = Vector2.one;
        pWall = SavePrefab(w, "Wall");

        // Cover props
        pCrate = Prop("Crate", S("Props/crate"), false);
        pBarrel = Prop("Barrel", S("Props/barrel"), true);

        // Exit
        var e = new GameObject("Exit");
        var ec = e.AddComponent<BoxCollider2D>();
        ec.isTrigger = true;
        ec.size = new Vector2(1.4f, 1.4f);
        Child(e.transform, "Visual", S("Props/exit"), Vector2.zero, 1f, -10);
        e.AddComponent<ExitZone>();
        pExit = SavePrefab(e, "Exit");
    }

    private static GameObject Prop(string name, Sprite sprite, bool round)
    {
        var go = new GameObject(name);
        go.layer = wallLayer;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = mat;
        sr.sortingOrder = 1;
        if (round) go.AddComponent<CircleCollider2D>().radius = 0.45f;
        else go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
        return SavePrefab(go, name);
    }

    private static void BuildCharacterPrefabs()
    {
        // ---------- Player ----------
        var go = new GameObject("Player");
        go.tag = "Player";
        var rb = go.AddComponent<Rigidbody2D>();
        SetupBody(rb, 1f);
        go.AddComponent<CircleCollider2D>().radius = 0.35f;
        Child(go.transform, "Shadow", S("Characters/shadow"), new Vector2(0, -0.2f), 1f, -5);
        var body = Child(go.transform, "Body", S("Characters/player_0"), Vector2.zero, 1f, 6);

        var health = go.AddComponent<Health>();
        Set(health, "maxHealth", 3);
        Set(health, "invincibleTime", 1f);
        Set(health, "body", body.GetComponent<SpriteRenderer>());
        Set(health, "hitEffect", pSpark);
        Set(health, "hurtSound", A("hurt"));

        var anim = go.AddComponent<CharacterVisual>();
        SetupAnimator(anim, body, "player");

        var pc = go.AddComponent<PlayerController>();
        Set(pc, "visual", anim);
        Set(pc, "body", body.GetComponent<SpriteRenderer>());
        Set(pc, "slashEffect", pSlash);
        Set(pc, "popupPrefab", pPopup);
        Set(pc, "slashSound", A("slash"));
        Set(pc, "hitSound", A("hit"));
        Set(pc, "backstabSound", A("backstab"));
        pPlayer = SavePrefab(go, "Player");

        // ---------- Enemies ----------
        var pat = Enemy<PatrollerEnemy>("Patroller", "patroller", 0.38f, 1f, 2, 6f, 100f, 1.2f);
        Set(pat, "moveSpeed", 2f);
        Set(pat, "slashEffect", pEnemySlash);
        Set(pat, "slashSound", A("slash"));
        pPatroller = SavePrefab(pat.gameObject, "Patroller");

        var sh = Enemy<ShooterEnemy>("Shooter", "shooter", 0.36f, 1f, 1, 7f, 70f, 1.6f);
        Set(sh, "moveSpeed", 1.6f);
        Set(sh, "bulletPrefab", pBullet.GetComponent<Projectile>());
        Set(sh, "aimLine", Line(sh.transform, new Color(1f, 0.15f, 0.15f, 1f), 0.06f, 2));
        Set(sh, "wallMask", wallMask);
        Set(sh, "muzzleFlash", pMuzzle);
        Set(sh, "shotSound", A("shot"));
        pShooter = SavePrefab(sh.gameObject, "Shooter");

        var ch = Enemy<ChargerEnemy>("Charger", "charger", 0.5f, 1.3f, 3, 6f, 120f, 0.7f);
        Set(ch, "moveSpeed", 1.6f);
        Set(ch, "pathLine", Line(ch.transform, new Color(1f, 0.15f, 0.15f, 0.35f), 0.7f, 2));
        Set(ch, "wallMask", wallMask);
        Set(ch, "dustEffect", pDust);
        Set(ch, "chargeSound", A("charge"));
        Set(ch, "wallHitSound", A("hit"));
        pCharger = SavePrefab(ch.gameObject, "Charger");
    }

    private static T Enemy<T>(string name, string art, float radius, float scale, int hp,
        float viewDistance, float viewAngle, float pitch) where T : EnemyBase
    {
        var go = new GameObject(name);
        var rb = go.AddComponent<Rigidbody2D>();
        SetupBody(rb, 5f);
        go.AddComponent<CircleCollider2D>().radius = radius;

        var sight = Line(go.transform, new Color(1f, 0.9f, 0.2f, 0.45f), 0.04f, 3);
        sight.enabled = true;
        sight.sortingOrder = -4;

        Child(go.transform, "Shadow", S("Characters/shadow"), new Vector2(0, -0.2f), scale, -5);
        var glow = Child(go.transform, "TelegraphGlow", S("FX/glow"), Vector2.zero, 2f * scale, 4);
        var body = Child(go.transform, "Body", S("Characters/" + art + "_0"), Vector2.zero, scale, 5);
        var icon = Child(go.transform, "StatusIcon", null, new Vector2(0, 0.75f * scale + 0.2f), 1.2f, 20);
        var label = Label(go.transform, new Vector2(0, -0.6f * scale - 0.15f));

        var health = go.AddComponent<Health>();
        Set(health, "maxHealth", hp);
        Set(health, "invincibleTime", 0.15f);
        Set(health, "body", body.GetComponent<SpriteRenderer>());
        Set(health, "hitEffect", pSpark);
        Set(health, "deathEffect", pDeath);
        Set(health, "hurtSound", A("hit"));
        Set(health, "deathSound", A("enemy_die"));

        var vision = go.AddComponent<VisionSensor>();
        Set(vision, "viewDistance", viewDistance);
        Set(vision, "viewAngle", viewAngle);
        Set(vision, "wallMask", wallMask);
        Set(vision, "sightLine", sight);

        var tel = go.AddComponent<Telegraph>();
        Set(tel, "glow", glow.GetComponent<SpriteRenderer>());
        Set(tel, "windUpSound", A("windup"));

        var anim = go.AddComponent<CharacterVisual>();
        SetupAnimator(anim, body, art);

        var enemy = go.AddComponent<T>();
        Set(enemy, "visual", anim);
        Set(enemy, "icon", icon.GetComponent<SpriteRenderer>());
        Set(enemy, "alertIcon", S("FX/icon_alert"));
        Set(enemy, "searchIcon", S("FX/icon_search"));
        Set(enemy, "stunIcon", S("FX/icon_stun"));
        Set(enemy, "stateLabel", label);
        return enemy;
    }

    private static void SetupAnimator(CharacterVisual anim, GameObject body, string art)
    {
        Set(anim, "body", body.transform);
        Set(anim, "bodyRenderer", body.GetComponent<SpriteRenderer>());
        Set(anim, "walkFrames", new Object[] { S("Characters/" + art + "_0"), S("Characters/" + art + "_1"), S("Characters/" + art + "_0"), S("Characters/" + art + "_2") });
    }

    // =====================================================================  SCENES
    private class LevelRefs
    {
        public Transform level;
        public GameObject player;
        public GameObject exit;
        public SpriteRenderer floor;
        public Text seedText;
        public Transform waypoints;
    }

    private static LevelRefs NewLevelScene(string sceneName, string nextScene, Vector2 floorSize, Vector2 playerPos, Vector2 exitPos)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var r = new LevelRefs();
        r.level = new GameObject("Level").transform;
        r.waypoints = new GameObject("Waypoints").transform;

        var floorGo = new GameObject("Floor");
        floorGo.transform.SetParent(r.level, false);
        r.floor = floorGo.AddComponent<SpriteRenderer>();
        r.floor.sprite = S("Tiles/floor");
        r.floor.drawMode = SpriteDrawMode.Tiled;
        r.floor.size = floorSize;
        r.floor.sharedMaterial = mat;
        r.floor.sortingOrder = -20;

        r.player = Place(pPlayer, playerPos, null);
        r.exit = Place(pExit, exitPos, r.level);

        // Camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(playerPos.x, playerPos.y, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.1f);
        camGo.AddComponent<AudioListener>();
        Set(camGo.AddComponent<CameraFollow>(), "target", r.player.transform);

        // Alerts (siren + nearest enemies run to you)
        var alert = new GameObject("AlertSystem").AddComponent<AlertSystem>();
        Set(alert, "sirenSound", A("siren"));

        // UI + GameManager
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        Set(gm, "nextScene", nextScene);
        Set(gm, "winSound", A("win"));
        Set(gm, "loseSound", A("lose"));
        BuildHud(gm, r);
        return r;
    }

    private static void BuildHud(GameManager gm, LevelRefs r)
    {
        Canvas canvas = NewCanvas();
        Transform c = canvas.transform;

        var joyBase = UIImage(c, "Joystick", S("UI/joy_base"), Color.white, new Vector2(0, 0), new Vector2(250, 250), new Vector2(320, 320));
        var knob = UIImage(joyBase.transform, "Knob", S("UI/joy_knob"), Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140));
        knob.GetComponent<Image>().raycastTarget = false;
        var joy = joyBase.AddComponent<VirtualJoystick>();
        Set(joy, "background", joyBase.GetComponent<RectTransform>());
        Set(joy, "handle", knob.GetComponent<RectTransform>());
        Set(r.player.GetComponent<PlayerController>(), "joystick", joy);

        var hit = UIImage(c, "HitButton", S("UI/btn_attack"), Color.white, new Vector2(1, 0), new Vector2(-240, 240), new Vector2(250, 250));
        hit.AddComponent<AttackButton>();

        Set(gm, "triesText", UIText(c, "Tries", "Попытка 1", 44, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(240, -50), new Vector2(420, 70)));
        Set(gm, "hpText", UIText(c, "HP", "Жизни 3/3", 44, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(240, -112), new Vector2(420, 70)));
        Set(gm, "timerText", UIText(c, "Timer", "0.0 с", 56, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(400, 80)));
        r.seedText = UIText(c, "Seed", "", 30, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(400, 50));

        var message = UIText(c, "Message", "", 80, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(1400, 300));
        message.raycastTarget = false;
        Set(gm, "messageText", message);

        Button tester = UIButton(c, "Новый тестер", new Vector2(1, 1), new Vector2(-200, -60), new Vector2(340, 90), 34);
        UnityEventTools.AddPersistentListener(tester.onClick, gm.NewTester);
        Button menu = UIButton(c, "Меню", new Vector2(1, 1), new Vector2(-200, -165), new Vector2(340, 90), 34);
        UnityEventTools.AddPersistentListener(menu.onClick, gm.LoadMenu);

        // Level clear panel
        var panel = UIImage(c, "ClearPanel", S("UI/panel"), new Color(0.05f, 0.06f, 0.1f, 0.92f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 640));
        panel.GetComponent<Image>().type = Image.Type.Sliced;
        Set(gm, "winPanel", panel);
        Set(gm, "winText", UIText(panel.transform, "ClearText", "Уровень пройден", 54, TextAnchor.UpperCenter, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(860, 300)));
        Button next = UIButton(panel.transform, "Дальше", new Vector2(0.5f, 0), new Vector2(-280, 90), new Vector2(240, 100), 40);
        UnityEventTools.AddPersistentListener(next.onClick, gm.LoadNext);
        Set(gm, "nextButton", next.gameObject);
        Button retry = UIButton(panel.transform, "Заново", new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(240, 100), 40);
        UnityEventTools.AddPersistentListener(retry.onClick, gm.Restart);
        Button menu2 = UIButton(panel.transform, "Меню", new Vector2(0.5f, 0), new Vector2(280, 90), new Vector2(240, 100), 40);
        UnityEventTools.AddPersistentListener(menu2.onClick, gm.LoadMenu);
        panel.SetActive(false);
    }

    private static void SaveScene(string name)
    {
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), SceneDir + name + ".unity");
    }

    // ---------- Menu ----------
    private static void BuildMenuScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 0, -10);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.07f, 0.07f, 0.1f);
        camGo.AddComponent<AudioListener>();

        var floor = new GameObject("Floor").AddComponent<SpriteRenderer>();
        floor.sprite = S("Tiles/floor");
        floor.drawMode = SpriteDrawMode.Tiled;
        floor.size = new Vector2(30, 14);
        floor.sharedMaterial = mat;
        floor.color = new Color(0.6f, 0.6f, 0.7f);
        floor.sortingOrder = -20;

        Canvas canvas = NewCanvas();
        Transform c = canvas.transform;
        var menu = new GameObject("MainMenu").AddComponent<MainMenu>();

        UIText(c, "Title", "Три врага", 110, TextAnchor.MiddleCenter, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1600, 160));

        string[] names = { "patroller", "shooter", "charger" };
        string[] titles = { "Патрульный", "Стрелок", "Таран" };
        for (int i = 0; i < 3; i++)
        {
            var img = UIImage(c, titles[i], S("Characters/" + names[i] + "_0"), Color.white, new Vector2(0.5f, 0.5f), new Vector2(-520 + i * 520, 120), new Vector2(160, 160));
            img.GetComponent<Image>().preserveAspect = true;
            img.transform.localRotation = Quaternion.Euler(0, 0, 90);
            UIText(c, titles[i] + "Label", titles[i], 36, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), new Vector2(-520 + i * 520, 10), new Vector2(400, 60));
        }

        string[] scenes = { "Level1", "Level2", "Random" };
        string[] labels = { "Уровень 1", "Уровень 2", "Случайный" };
        for (int i = 0; i < 3; i++)
        {
            Button b = UIButton(c, labels[i], new Vector2(0.5f, 0.5f), new Vector2(-400 + i * 400, -160), new Vector2(340, 120), 48);
            UnityEventTools.AddStringPersistentListener(b.onClick, menu.LoadScene, scenes[i]);
        }
        SaveScene("Menu");
    }

    // ---------- Level 1: tutorial-sized, one of each + a second patroller ----------
    private static void BuildLevel1()
    {
        var r = NewLevelScene("Level1", "Level2", new Vector2(40, 20), new Vector2(-17, -7), new Vector2(18, -8));
        var L = r.level;

        Wall(L, 0, 10, 41, 1); Wall(L, 0, -10, 41, 1); Wall(L, -20, 0, 1, 21); Wall(L, 20, 0, 1, 21);
        Wall(L, -12, 3, 1, 14);   // gap at the bottom
        Wall(L, -4, -3, 1, 14);   // gap at the top
        Wall(L, 4, 3, 2, 2); Wall(L, 4, -3, 2, 2); Wall(L, 10, 0, 2, 2); Wall(L, 15, -4, 1, 4);

        Place(pCrate, new Vector2(-16, 4), L); Place(pBarrel, new Vector2(-17, 7), L);
        Place(pCrate, new Vector2(0, 5), L); Place(pBarrel, new Vector2(14, 6), L);

        var p1 = Place(pPatroller, new Vector2(-8, -6), null);
        Set(p1.GetComponent<PatrollerEnemy>(), "waypoints", WP(r.waypoints, new Vector2(-8, -7), new Vector2(-8, 7)));
        var p2 = Place(pPatroller, new Vector2(7, 3), null);
        Set(p2.GetComponent<PatrollerEnemy>(), "waypoints", WP(r.waypoints, new Vector2(7, 3), new Vector2(13, 3), new Vector2(13, -3), new Vector2(7, -3)));
        var s1 = Place(pShooter, new Vector2(17, 4), null);
        Set(s1.GetComponent<VisionSensor>(), "facing", new Vector2(-1, -0.3f));
        var c1 = Place(pCharger, new Vector2(1, -7), null);
        Set(c1.GetComponent<ChargerEnemy>(), "waypoints", WP(r.waypoints, new Vector2(1, -7), new Vector2(9, -7)));

        SaveScene("Level1");
    }

    // ---------- Level 2: bigger, 7 enemies, three rooms ----------
    private static void BuildLevel2()
    {
        var r = NewLevelScene("Level2", "Random", new Vector2(48, 26), new Vector2(-20, 0), new Vector2(21, -10));
        var L = r.level;

        Wall(L, 0, 13, 49, 1); Wall(L, 0, -13, 49, 1); Wall(L, -24, 0, 1, 27); Wall(L, 24, 0, 1, 27);
        // wall A at x=-12 with doors at y -9..-6 and 5..8
        Wall(L, -12, -11, 1, 4); Wall(L, -12, -0.5f, 1, 11); Wall(L, -12, 10.5f, 1, 5);
        // wall B at x=6 with a door at y -2..2
        Wall(L, 6, -7.5f, 1, 11); Wall(L, 6, 7.5f, 1, 11);
        // middle cover
        Wall(L, -3, 0, 10, 1);
        // right room pillars
        Wall(L, 12, 5, 2, 2); Wall(L, 12, -5, 2, 2); Wall(L, 18, 0, 2, 4);

        Place(pBarrel, new Vector2(-18, 8), L); Place(pBarrel, new Vector2(-18, -8), L); Place(pCrate, new Vector2(-17, -9), L);
        Place(pCrate, new Vector2(-3, 6.5f), L); Place(pCrate, new Vector2(-3, -6.5f), L);
        Place(pCrate, new Vector2(11, 1), L); Place(pBarrel, new Vector2(22, 11), L);

        var pa = Place(pPatroller, new Vector2(-9, 9), null);
        Set(pa.GetComponent<PatrollerEnemy>(), "waypoints", WP(r.waypoints, new Vector2(-9, 9), new Vector2(3, 9), new Vector2(3, 4), new Vector2(-9, 4)));
        var pb = Place(pPatroller, new Vector2(-9, -4), null);
        Set(pb.GetComponent<PatrollerEnemy>(), "waypoints", WP(r.waypoints, new Vector2(-9, -4), new Vector2(0, -4), new Vector2(0, -10), new Vector2(-9, -10)));
        var sa = Place(pShooter, new Vector2(4, -11), null);
        Set(sa.GetComponent<VisionSensor>(), "facing", new Vector2(-1, 0.5f));

        var ca = Place(pCharger, new Vector2(9, 8), null);
        Set(ca.GetComponent<ChargerEnemy>(), "waypoints", WP(r.waypoints, new Vector2(9, 8), new Vector2(9, -8)));
        var sb = Place(pShooter, new Vector2(22, 8), null);
        Set(sb.GetComponent<VisionSensor>(), "facing", new Vector2(-1, -0.3f));
        var cb = Place(pCharger, new Vector2(14, -9), null);
        Set(cb.GetComponent<ChargerEnemy>(), "waypoints", WP(r.waypoints, new Vector2(14, -9), new Vector2(20, -6)));
        var pc = Place(pPatroller, new Vector2(15, 4), null);
        Set(pc.GetComponent<PatrollerEnemy>(), "waypoints", WP(r.waypoints, new Vector2(15, 4), new Vector2(21, 4), new Vector2(21, -4), new Vector2(15, -4)));

        SaveScene("Level2");
    }

    // ---------- Random: rooms are prefabs, generator puts random ones in a row ----------
    private static GameObject MakeRoom(string name, bool leftDoor, bool rightDoor)
    {
        var room = new GameObject(name);
        var floor = new GameObject("Floor").AddComponent<SpriteRenderer>();
        floor.transform.SetParent(room.transform, false);
        floor.sprite = S("Tiles/floor");
        floor.drawMode = SpriteDrawMode.Tiled;
        floor.size = new Vector2(16, 12);
        floor.sharedMaterial = mat;
        floor.sortingOrder = -20;

        Transform t = room.transform;
        Wall(t, 0, 5.5f, 16, 1);
        Wall(t, 0, -5.5f, 16, 1);
        foreach (float x in new[] { -7.5f, 7.5f })
        {
            bool door = x < 0 ? leftDoor : rightDoor;
            if (door) { Wall(t, x, 3.25f, 1, 3.5f); Wall(t, x, -3.25f, 1, 3.5f); }
            else Wall(t, x, 0, 1, 10);
        }
        return room;
    }

    private static void Spawn(GameObject room, Vector2 pos, float lookAngle, params Vector2[] route)
    {
        var sp = new GameObject("SpawnPoint");
        sp.transform.SetParent(room.transform, false);
        sp.transform.localPosition = pos;
        sp.transform.localRotation = Quaternion.Euler(0, 0, lookAngle);
        var spawn = sp.AddComponent<SpawnPoint>();

        var points = new Object[route.Length];
        for (int i = 0; i < route.Length; i++)
        {
            var wp = new GameObject("Route_" + i).transform;
            wp.SetParent(room.transform, false);
            wp.localPosition = route[i];
            points[i] = wp;
        }
        Set(spawn, "route", points);
    }

    private static GameObject SaveRoom(GameObject room)
    {
        return SavePrefab(room, "Rooms/" + room.name);
    }

    private static void BuildRandomScene()
    {
        Directory.CreateDirectory(PrefabDir + "Rooms");

        var start = MakeRoom("Room_Start", false, true);
        Place(pBarrel, new Vector2(-5, 4), start.transform); Place(pCrate, new Vector2(-6, -4), start.transform);
        var startPrefab = SaveRoom(start);

        var exit = MakeRoom("Room_Exit", true, false);
        Place(pExit, new Vector2(4, 0), exit.transform);
        Place(pCrate, new Vector2(2, 3), exit.transform); Place(pCrate, new Vector2(2, -3), exit.transform);
        var exitPrefab = SaveRoom(exit);

        var a = MakeRoom("Room_Pillars", true, true);
        Wall(a.transform, -3, 2, 2, 2); Wall(a.transform, 3, -2, 2, 2);
        Spawn(a, new Vector2(0, 3.5f), 180, new Vector2(-5, 3.5f), new Vector2(5, 3.5f));
        Spawn(a, new Vector2(5, 3), 200, new Vector2(5, 3), new Vector2(5, -4));
        Spawn(a, new Vector2(-5, -3.5f), 0, new Vector2(-5, -3.5f), new Vector2(5, -3.5f));
        var aPrefab = SaveRoom(a);

        var b = MakeRoom("Room_Cover", true, true);
        Wall(b.transform, 0, 2, 6, 1); Wall(b.transform, 0, -2, 6, 1);
        Place(pCrate, new Vector2(4, 3.5f), b.transform);
        Spawn(b, new Vector2(0, 3.5f), 180, new Vector2(-5, 3.5f), new Vector2(5, 3.5f));
        Spawn(b, new Vector2(0, -3.5f), 180, new Vector2(5, -3.5f), new Vector2(-5, -3.5f));
        Spawn(b, new Vector2(5, 0), 180, new Vector2(5, 0), new Vector2(5, 0.5f));
        var bPrefab = SaveRoom(b);

        var c = MakeRoom("Room_Crates", true, true);
        Place(pCrate, new Vector2(-3, 3), c.transform); Place(pCrate, new Vector2(-2, -3), c.transform);
        Place(pCrate, new Vector2(3, 1), c.transform); Place(pCrate, new Vector2(4, -3), c.transform);
        Place(pBarrel, new Vector2(0, 3), c.transform);
        Spawn(c, new Vector2(0, -1), 180, new Vector2(-5, -1), new Vector2(5, -1));
        Spawn(c, new Vector2(5, 4), 225, new Vector2(5, 4), new Vector2(-5, 4));
        Spawn(c, new Vector2(-5, -4), 0, new Vector2(-5, -4), new Vector2(5, -4));
        var cPrefab = SaveRoom(c);

        var d = MakeRoom("Room_Block", true, true);
        Wall(d.transform, 0, 0, 2, 4);
        Spawn(d, new Vector2(-4, 3.5f), 0, new Vector2(-4, 3.5f), new Vector2(4, 3.5f), new Vector2(4, -3.5f), new Vector2(-4, -3.5f));
        Spawn(d, new Vector2(5, -4), 135, new Vector2(5, -4), new Vector2(5, 4));
        Spawn(d, new Vector2(-5, -4), 45, new Vector2(-5, -4), new Vector2(-5, 4));
        var dPrefab = SaveRoom(d);

        var r = NewLevelScene("Random", "Random", new Vector2(1, 1), new Vector2(-4, 0), new Vector2(1000, 1000));
        Object.DestroyImmediate(r.exit);
        r.floor.enabled = false;

        var gen = new GameObject("LevelGenerator").AddComponent<LevelGenerator>();
        Set(gen, "startRoom", startPrefab);
        Set(gen, "exitRoom", exitPrefab);
        Set(gen, "middleRooms", new Object[] { aPrefab, bPrefab, cPrefab, dPrefab });
        Set(gen, "enemyPrefabs", new Object[] { pPatroller, pShooter, pCharger });
        Set(gen, "seedText", r.seedText);
        SaveScene("Random");
    }

    // =====================================================================  HELPERS
    private static GameObject Place(GameObject prefab, Vector2 pos, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = pos;
        return go;
    }

    private static void Wall(Transform parent, float x, float y, float w, float h)
    {
        var go = Place(pWall, new Vector2(x, y), parent);
        var sr = go.GetComponent<SpriteRenderer>();
        sr.size = new Vector2(w, h);
        go.GetComponent<BoxCollider2D>().size = new Vector2(w, h);
        go.isStatic = true;
    }

    private static Object[] WP(Transform root, params Vector2[] pts)
    {
        var result = new Object[pts.Length];
        for (int i = 0; i < pts.Length; i++)
        {
            var t = new GameObject("WP_" + root.childCount).transform;
            t.SetParent(root, false);
            t.position = pts[i];
            result[i] = t;
        }
        return result;
    }

    private static GameObject SavePrefab(GameObject go, string name)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    private static void SetupBody(Rigidbody2D rb, float mass)
    {
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.mass = mass;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private static GameObject Child(Transform parent, string name, Sprite sprite, Vector2 pos, float scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = mat;
        sr.sortingOrder = order;
        return go;
    }

    private static TextMesh Label(Transform parent, Vector2 pos)
    {
        var go = new GameObject("StateLabel");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var tm = go.AddComponent<TextMesh>();
        tm.font = font;
        tm.fontSize = 40;
        tm.characterSize = 0.06f;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = new Color(1, 1, 1, 0.8f);
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = 50;
        return tm;
    }

    private static LineRenderer Line(Transform parent, Color color, float width, int points)
    {
        var go = new GameObject(points == 3 ? "SightLine" : "TelegraphLine");
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = points;
        lr.useWorldSpace = true;
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = color;
        lr.sharedMaterial = mat;
        lr.sortingOrder = 3;
        lr.numCapVertices = 4;
        lr.enabled = false;
        return lr;
    }

    private static Canvas NewCanvas()
    {
        var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        es.AddComponent<StandaloneInputModule>();
#endif
        return canvas;
    }

    private static GameObject UIImage(Transform parent, string name, Sprite sprite, Color color, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return go;
    }

    private static Text UIText(Transform parent, string name, string text, int size, TextAnchor align, Vector2 anchor, Vector2 pos, Vector2 box)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = box;
        var t = go.GetComponent<Text>();
        t.text = text;
        t.font = font;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(3, -3);
        return t;
    }

    private static Button UIButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize)
    {
        var go = UIImage(parent, "Button_" + label, S("UI/panel"), new Color(0.25f, 0.3f, 0.45f, 0.9f), anchor, pos, size);
        go.GetComponent<Image>().type = Image.Type.Sliced;
        var b = go.AddComponent<Button>();
        UIText(go.transform, "Label", label, fontSize, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return b;
    }

    // Sets a private [SerializeField] so it is saved with the scene / prefab.
    private static void Set(Object target, string field, object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null) { Debug.LogError("EnemyGameBuilder: no field '" + field + "' on " + target); return; }

        switch (value)
        {
            case Object[] arr:
                p.arraySize = arr.Length;
                for (int i = 0; i < arr.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
                break;
            case Object o: p.objectReferenceValue = o; break;
            case float f: p.floatValue = f; break;
            case int i2: p.intValue = i2; break;
            case bool b: p.boolValue = b; break;
            case string s: p.stringValue = s; break;
            case Color c: p.colorValue = c; break;
            case Vector2 v: p.vector2Value = v; break;
            case LayerMask m: p.intValue = m.value; break;
            default: Debug.LogError("EnemyGameBuilder: unsupported type for " + field); break;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tm.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var p = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(p.stringValue))
            {
                p.stringValue = name;
                tm.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        return 0;
    }
}
