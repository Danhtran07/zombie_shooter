using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ZombieAutoShooterSceneSetup
{
    private const string ScenePath = "Assets/Scenes/main_sence.unity";
    private const string GeneratedFolder = "Assets/Generated/ZombieAutoShooter";
    private const string ZombiePrefabPath = GeneratedFolder + "/ZombieRuntime.prefab";

    [MenuItem("Tools/Zombie Auto Shooter/Build Main Scene")]
    public static void SetupMainScene()
    {
        EnsureFolder("Assets/Generated");
        EnsureFolder(GeneratedFolder);

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject player = FindPlayer();
        if (player == null)
        {
            throw new InvalidOperationException("No Player object found in main_sence.");
        }

        SetupPlayer(player);

        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        SetupCamera(camera, player.transform);

        GameObject bulletPrefab = LoadBulletPrefab();
        Gun gun = SetupWeapon(player, bulletPrefab);

        GameObject zombiePrefab = SetupZombiePrefab(player.transform);
        GameObject systems = FindOrCreateRoot("ZombieAutoShooter_Setup");
        ObjectPool bulletPool = SetupPool(systems.transform, "BulletPool", bulletPrefab, 64, 256);
        ObjectPool zombiePool = SetupPool(systems.transform, "ZombiePool", zombiePrefab, 24, 160);

        if (gun != null)
        {
            SetSerialized(gun, "bulletPool", bulletPool);
            SetSerialized(gun, "bulletSpeed", 45f);
            SetSerialized(gun, "fireRate", 0.08f);
        }

        Transform[] spawnPoints = SetupSpawnPoints(systems.transform, player.transform.position);
        ZombieSpawner spawner = SetupSpawner(systems, player.transform, zombiePrefab, zombiePool, spawnPoints);
        ZombieGameSession session = SetupGameSession(systems, player, gun);

        SetupHud(session);
        SetupNavMesh(systems, spawner);
        AddSceneToBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[ZombieAutoShooterSceneSetup] main_sence setup complete.");
    }

    [MenuItem("Tools/Zombie Auto Shooter/Validate Main Scene")]
    public static void ValidateMainSceneForPlay()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        if (FindPlayer() == null)
        {
            throw new InvalidOperationException("Validation failed: Player missing.");
        }

        if (UnityEngine.Object.FindFirstObjectByType<ZombieSpawner>() == null)
        {
            throw new InvalidOperationException("Validation failed: ZombieSpawner missing.");
        }

        if (UnityEngine.Object.FindFirstObjectByType<ZombieGameSession>() == null)
        {
            throw new InvalidOperationException("Validation failed: ZombieGameSession missing.");
        }

        if (UnityEngine.Object.FindFirstObjectByType<SimpleGameplayHUD>() == null)
        {
            throw new InvalidOperationException("Validation failed: SimpleGameplayHUD missing.");
        }

        Gun gun = UnityEngine.Object.FindFirstObjectByType<Gun>();
        if (gun == null)
        {
            throw new InvalidOperationException("Validation failed: Gun missing.");
        }

        Debug.Log("[ZombieAutoShooterSceneSetup] main_sence validation passed.");
    }

    private static GameObject FindPlayer()
    {
        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
        {
            return tagged;
        }

        return Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene.IsValid())
            .FirstOrDefault(go => go.name == "Player");
    }

    private static void SetupPlayer(GameObject player)
    {
        player.tag = "Player";
        player.SetActive(true);

        CharacterController characterController = player.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        PlayerHealth health = EnsureComponent<PlayerHealth>(player);
        SetSerialized(health, "maxHealth", 100f);

        Rigidbody rb = EnsureComponent<Rigidbody>(player);
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationY |
            RigidbodyConstraints.FreezeRotationZ;

        CapsuleCollider capsule = EnsureComponent<CapsuleCollider>(player);
        float scaleX = Mathf.Max(0.0001f, player.transform.lossyScale.x);
        float scaleY = Mathf.Max(0.0001f, player.transform.lossyScale.y);
        capsule.isTrigger = false;
        capsule.radius = 0.4f / scaleX;
        capsule.height = 1.8f / scaleY;
        capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

        PlayerCombat combat = EnsureComponent<PlayerCombat>(player);
        SetSerialized(combat, "attackRange", 16f);
        SetSerialized(combat, "targetRefreshRate", 0.08f);
    }

    private static void SetupCamera(Camera camera, Transform target)
    {
        if (camera == null)
        {
            GameObject cameraObject = new GameObject("Camera");
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        ThirdPersonCamera follow = EnsureComponent<ThirdPersonCamera>(camera.gameObject);
        SetSerialized(follow, "target", target);
        SetSerialized(follow, "distance", 15f);
        SetSerialized(follow, "height", 2f);
    }

    private static Gun SetupWeapon(GameObject player, GameObject bulletPrefab)
    {
        Gun gun = player.GetComponentInChildren<Gun>(true);
        if (gun == null)
        {
            GameObject akPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weapon/AK47/AK47.fbx");
            GameObject weaponRoot = new GameObject("AutoWeapon");
            weaponRoot.transform.SetParent(player.transform);
            weaponRoot.transform.localPosition = new Vector3(0.35f, 1.1f, 0.55f);
            weaponRoot.transform.localRotation = Quaternion.identity;

            if (akPrefab != null)
            {
                GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(akPrefab);
                model.name = "AK47";
                model.transform.SetParent(weaponRoot.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                model.transform.localScale = Vector3.one * 20f;
            }

            gun = weaponRoot.AddComponent<Gun>();
        }

        gun.gameObject.SetActive(true);

        Transform muzzle = GetSerializedTransform(gun, "muzzle");
        if (muzzle == null)
        {
            muzzle = FindOrCreateChild(gun.transform, "Muzzle");
            muzzle.localPosition = new Vector3(0f, 0f, 0.9f);
            muzzle.localRotation = Quaternion.identity;
        }

        SetSerialized(gun, "muzzle", muzzle);
        SetSerialized(gun, "bulletPrefab", bulletPrefab);
        SetSerialized(gun, "bulletSpeed", 45f);
        SetSerialized(gun, "fireRate", 0.08f);
        SetSerialized(gun, "damage", 12f);

        PlayerCombat combat = EnsureComponent<PlayerCombat>(player);
        SetSerialized(combat, "gun", gun);
        SetSerialized(combat, "weaponPivot", gun.transform);

        return gun;
    }

    private static GameObject LoadBulletPrefab()
    {
        GameObject bulletPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Weapon/Bullet.prefab");

        if (bulletPrefab == null)
        {
            throw new InvalidOperationException("Bullet prefab is missing at Assets/Weapon/Bullet.prefab.");
        }

        EnsurePrefabComponent<Bullet>(bulletPrefab);
        Rigidbody rb = EnsurePrefabComponent<Rigidbody>(bulletPrefab);
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        Collider collider = bulletPrefab.GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = true;
        }

        EditorUtility.SetDirty(bulletPrefab);
        return bulletPrefab;
    }

    private static GameObject SetupZombiePrefab(Transform player)
    {
        GameObject existing =
            AssetDatabase.LoadAssetAtPath<GameObject>(ZombiePrefabPath);

        if (existing != null)
        {
            SetupZombieObject(existing, player);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        GameObject sceneZombie = FindSceneZombie();
        GameObject zombieRoot;

        if (sceneZombie != null)
        {
            zombieRoot = UnityEngine.Object.Instantiate(sceneZombie);
            zombieRoot.name = "ZombieRuntime";
            sceneZombie.SetActive(false);
        }
        else
        {
            GameObject zombieModel =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Enemy/Zombie.fbx");

            if (zombieModel == null)
            {
                throw new InvalidOperationException("Zombie model missing at Assets/Enemy/Zombie.fbx.");
            }

            zombieRoot = (GameObject)PrefabUtility.InstantiatePrefab(zombieModel);
            zombieRoot.name = "ZombieRuntime";
        }

        zombieRoot.tag = "Enemy";
        zombieRoot.SetActive(true);

        SetupZombieObject(zombieRoot, player);

        GameObject prefab =
            PrefabUtility.SaveAsPrefabAsset(zombieRoot, ZombiePrefabPath);

        UnityEngine.Object.DestroyImmediate(zombieRoot);
        return prefab;
    }

    private static GameObject FindSceneZombie()
    {
        foreach (EnemyAI ai in Resources.FindObjectsOfTypeAll<EnemyAI>())
        {
            if (ai.gameObject.scene.IsValid())
            {
                return ai.gameObject;
            }
        }

        return null;
    }

    private static void SetupZombieObject(GameObject zombie, Transform player)
    {
        zombie.tag = "Enemy";

        Animator animator = EnsureComponent<Animator>(zombie);
        RuntimeAnimatorController controller =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Enemy/Zombie.controller");
        Avatar avatar =
            AssetDatabase.LoadAssetAtPath<Avatar>("Assets/Enemy/ZombieAvatar.asset");

        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }

        if (avatar != null)
        {
            animator.avatar = avatar;
        }

        NavMeshAgent agent = EnsureComponent<NavMeshAgent>(zombie);
        agent.radius = 0.45f;
        agent.height = 1.8f;
        agent.speed = 2.3f;
        agent.angularSpeed = 480f;
        agent.acceleration = 12f;
        agent.stoppingDistance = 1.35f;

        CapsuleCollider capsule = EnsureComponent<CapsuleCollider>(zombie);
        capsule.isTrigger = false;
        capsule.radius = 0.45f;
        capsule.height = 1.8f;
        capsule.center = new Vector3(0f, 0.9f, 0f);

        Rigidbody rb = EnsureComponent<Rigidbody>(zombie);
        rb.useGravity = true;
        rb.isKinematic = true;

        EnemyAI ai = EnsureComponent<EnemyAI>(zombie);
        SetSerialized(ai, "player", player);
        SetSerialized(ai, "moveSpeed", 2.3f);
        SetSerialized(ai, "attackRange", 1.45f);
        SetSerialized(ai, "attackCooldown", 1.2f);
        SetSerialized(ai, "damage", 8f);

        EnemyHealth health = EnsureComponent<EnemyHealth>(zombie);
        SetSerialized(health, "maxHealth", 35f);
        SetSerialized(health, "destroyOnDeath", true);
        SetSerialized(health, "destroyDelay", 0.4f);
        SetSerialized(health, "xpReward", 1);
    }

    private static ObjectPool SetupPool(
        Transform parent,
        string name,
        GameObject prefab,
        int preload,
        int maxSize)
    {
        GameObject poolObject = FindOrCreateChild(parent, name).gameObject;
        ObjectPool pool = EnsureComponent<ObjectPool>(poolObject);
        SetSerialized(pool, "prefab", prefab);
        SetSerialized(pool, "preloadCount", preload);
        SetSerialized(pool, "maxSize", maxSize);
        return pool;
    }

    private static Transform[] SetupSpawnPoints(Transform parent, Vector3 center)
    {
        Transform root = FindOrCreateChild(parent, "SpawnPoints");
        float radius = 28f;
        List<Transform> points = new List<Transform>();

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            Vector3 position =
                center +
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;

            if (NavMesh.SamplePosition(position, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            {
                position = hit.position;
            }

            Transform point = FindOrCreateChild(root, "SpawnPoint_" + (i + 1));
            point.position = position;
            points.Add(point);
        }

        return points.ToArray();
    }

    private static ZombieSpawner SetupSpawner(
        GameObject systems,
        Transform player,
        GameObject zombiePrefab,
        ObjectPool zombiePool,
        Transform[] spawnPoints)
    {
        ZombieSpawner spawner = EnsureComponent<ZombieSpawner>(systems);
        SetSerialized(spawner, "player", player);
        SetSerialized(spawner, "zombiePrefabs", new[] { zombiePrefab });
        SetSerialized(spawner, "zombiePools", new[] { zombiePool });
        SetSerialized(spawner, "spawnPoints", spawnPoints);
        SetSerialized(spawner, "maxZombies", 60);
        SetSerialized(spawner, "spawnBurstCount", 3);
        SetSerialized(spawner, "spawnDistanceMin", 10f);
        SetSerialized(spawner, "spawnDistanceMax", 36f);
        SetSerialized(spawner, "spawnRate", 1.25f);
        SetSerialized(spawner, "maxSpawnRate", 6.5f);
        return spawner;
    }

    private static ZombieGameSession SetupGameSession(
        GameObject systems,
        GameObject player,
        Gun gun)
    {
        ZombieGameSession session = EnsureComponent<ZombieGameSession>(systems);
        SetSerialized(session, "playerHealth", player.GetComponent<PlayerHealth>());
        SetSerialized(session, "playerCombat", player.GetComponent<PlayerCombat>());
        SetSerialized(session, "weapon", gun);
        SetSerialized(session, "secondsPerWave", 30f);
        return session;
    }

    private static void SetupHud(ZombieGameSession session)
    {
        GameObject canvasObject = GameObject.Find("GameplayHUD");
        if (canvasObject == null)
        {
            canvasObject = new GameObject("GameplayHUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        SimpleGameplayHUD hud = EnsureComponent<SimpleGameplayHUD>(canvasObject);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        Text hp = CreateHudText(canvasObject.transform, "HPText", font, new Vector2(12f, -12f));
        Slider xp = CreateXpSlider(canvasObject.transform);
        Text level = CreateHudText(canvasObject.transform, "LevelText", font, new Vector2(12f, -72f));
        Text kill = CreateHudText(canvasObject.transform, "KillText", font, new Vector2(12f, -102f));
        Text wave = CreateHudText(canvasObject.transform, "WaveText", font, new Vector2(12f, -132f));
        Text timer = CreateHudText(canvasObject.transform, "TimerText", font, new Vector2(12f, -162f));

        SetSerialized(hud, "session", session);
        SetSerialized(hud, "hpText", hp);
        SetSerialized(hud, "xpBar", xp);
        SetSerialized(hud, "levelText", level);
        SetSerialized(hud, "killText", kill);
        SetSerialized(hud, "waveText", wave);
        SetSerialized(hud, "timerText", timer);
    }

    private static Text CreateHudText(
        Transform parent,
        string name,
        Font font,
        Vector2 anchoredPosition)
    {
        Transform existing = parent.Find(name);
        GameObject textObject =
            existing != null ? existing.gameObject : new GameObject(name);

        textObject.transform.SetParent(parent, false);
        Text text = EnsureComponent<Text>(textObject);
        text.font = font;
        text.fontSize = 20;
        text.alignment = TextAnchor.UpperLeft;
        text.color = Color.white;

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(260f, 28f);

        return text;
    }

    private static Slider CreateXpSlider(Transform parent)
    {
        Transform existing = parent.Find("XPBar");
        GameObject root =
            existing != null ? existing.gameObject : new GameObject("XPBar");

        root.transform.SetParent(parent, false);
        Slider slider = EnsureComponent<Slider>(root);
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0f;

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(12f, -42f);
        rect.sizeDelta = new Vector2(220f, 20f);

        Image background = EnsureImageChild(root.transform, "Background", new Color(0f, 0f, 0f, 0.55f));
        RectTransform bgRect = background.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        Image fill = EnsureImageChild(root.transform, "Fill", new Color(0.2f, 0.75f, 1f, 0.9f));
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.targetGraphic = fill;
        slider.fillRect = fillRect;

        return slider;
    }

    private static Image EnsureImageChild(Transform parent, string name, Color color)
    {
        Transform child = parent.Find(name);
        GameObject go = child != null ? child.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        Image image = EnsureComponent<Image>(go);
        image.color = color;
        return image;
    }

    private static void SetupNavMesh(GameObject systems, ZombieSpawner spawner)
    {
        NavMeshSurface surface = EnsureComponent<NavMeshSurface>(systems);
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        surface.defaultArea = 0;
        surface.BuildNavMesh();
        EditorUtility.SetDirty(surface);
        EditorUtility.SetDirty(spawner);
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes =
            new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        if (scenes.Any(scene => scene.path == ScenePath))
        {
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == ScenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
                }
            }
        }
        else
        {
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static GameObject FindOrCreateRoot(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null)
        {
            return existing;
        }

        return new GameObject(name);
    }

    private static Transform FindOrCreateChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static T EnsureComponent<T>(GameObject go)
        where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    private static T EnsurePrefabComponent<T>(GameObject prefab)
        where T : Component
    {
        T component = prefab.GetComponent<T>();
        return component != null ? component : prefab.AddComponent<T>();
    }

    private static Transform GetSerializedTransform(UnityEngine.Object target, string propertyName)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);
        return property != null ? property.objectReferenceValue as Transform : null;
    }

    private static void SetSerialized(UnityEngine.Object target, string propertyName, object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"Property '{propertyName}' not found on {target.name}.");
        }

        AssignProperty(property, value);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }

    private static void AssignProperty(SerializedProperty property, object value)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Integer:
                property.intValue = Convert.ToInt32(value);
                break;
            case SerializedPropertyType.Boolean:
                property.boolValue = Convert.ToBoolean(value);
                break;
            case SerializedPropertyType.Float:
                property.floatValue = Convert.ToSingle(value);
                break;
            case SerializedPropertyType.ObjectReference:
                property.objectReferenceValue = (UnityEngine.Object)value;
                break;
            case SerializedPropertyType.Vector3:
                property.vector3Value = (Vector3)value;
                break;
            default:
                if (property.isArray && value is Array array)
                {
                    property.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                    {
                        AssignProperty(property.GetArrayElementAtIndex(i), array.GetValue(i));
                    }
                    break;
                }

                throw new NotSupportedException(
                    $"Unsupported serialized property type: {property.propertyType}");
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }
}
