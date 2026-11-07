using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BottleFlipSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/BottleFlipVR.unity";

    [MenuItem("Bottle Flip/Build Prototype Scene")]
    public static void Build()
    {
        EnsureFolder("Assets/Scenes");
        EnsureTag("Ground");
        EnsureTag("LandingSurface");
        EnsureTag("Hazard");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientLight = new Color(0.24f, 0.3f, 0.42f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.06f, 0.09f, 0.16f);
        RenderSettings.fogDensity = 0.015f;

        Material floorMaterial = Material("Floor", new Color(0.035f, 0.06f, 0.11f), 0.1f, 0.25f);
        Material platformMaterial = Material("Platform", new Color(0.12f, 0.34f, 0.55f), 0.2f, 0.35f);
        Material accentMaterial = Material("Accent", new Color(0.12f, 0.9f, 1f), 0.15f, 0.1f);
        Material bottleMaterial = Material("Bottle", new Color(0.9f, 0.96f, 1f), 0.05f, 0.18f);
        Material waterMaterial = Material("Water", new Color(0.08f, 0.5f, 0.9f), 0f, 0.05f);
        Material capMaterial = Material("Cap", new Color(0.06f, 0.25f, 0.55f), 0.1f, 0.25f);

        CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -1.3f, 5f), new Vector3(16f, 0.4f, 24f), floorMaterial, "Ground");
        CreatePrimitive("BackWall", PrimitiveType.Cube, new Vector3(0f, 2.6f, 13f), new Vector3(16f, 7f, 0.3f), floorMaterial, "Ground");

        Transform[] platforms = new Transform[4];
        platforms[0] = CreatePlatform("Platform_01", new Vector3(0f, 0f, 0f), new Vector3(3.6f, 0.5f, 3.2f), platformMaterial, accentMaterial);
        platforms[1] = CreatePlatform("Platform_02", new Vector3(0f, 0.35f, 3.25f), new Vector3(3.4f, 0.5f, 3.0f), platformMaterial, accentMaterial);
        platforms[2] = CreatePlatform("Platform_03", new Vector3(0f, 0.7f, 6.4f), new Vector3(3.3f, 0.5f, 3.0f), platformMaterial, accentMaterial);
        platforms[3] = CreatePlatform("Platform_04", new Vector3(0f, 1.05f, 9.55f), new Vector3(3.2f, 0.5f, 3.0f), platformMaterial, accentMaterial);

        GameObject bottle = CreateBottle(bottleMaterial, waterMaterial, capMaterial, platforms[0].position + Vector3.up * 1.02f);
        BottleFlipController controller = bottle.GetComponent<BottleFlipController>();

        GameObject managerObject = new GameObject("BottleFlipGameManager");
        BottleFlipGameManager manager = managerObject.AddComponent<BottleFlipGameManager>();
        SerializedObject managerSerialized = new SerializedObject(manager);
        managerSerialized.FindProperty("bottle").objectReferenceValue = controller;
        SerializedProperty platformProperty = managerSerialized.FindProperty("platforms");
        platformProperty.arraySize = platforms.Length;
        for (int i = 0; i < platforms.Length; i++) platformProperty.GetArrayElementAtIndex(i).objectReferenceValue = platforms[i];
        managerSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
        camera.fieldOfView = 66f;
        camera.nearClipPlane = 0.03f;
        camera.farClipPlane = 80f;
        cameraObject.transform.position = new Vector3(0f, 2.7f, -8.2f);
        BottleFlipCamera follow = cameraObject.AddComponent<BottleFlipCamera>();
        SerializedObject followSerialized = new SerializedObject(follow);
        followSerialized.FindProperty("target").objectReferenceValue = bottle.transform;
        followSerialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject lightObject = new GameObject("Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.color = new Color(0.74f, 0.86f, 1f);
        lightObject.transform.rotation = Quaternion.Euler(38f, -28f, 0f);

        GameObject fillObject = new GameObject("Fill Light");
        Light fill = fillObject.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.intensity = 7f;
        fill.range = 13f;
        fill.color = new Color(0.1f, 0.6f, 1f);
        fillObject.transform.position = new Vector3(0f, 3f, 5f);

        GameObject audioObject = new GameObject("Feedback Audio");
        audioObject.AddComponent<BottleFlipAudio>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("Bottle Flip VR prototype scene built at " + ScenePath);
    }

    public static void Validate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject bottle = GameObject.Find("Bottle");
        BottleFlipController controller = bottle == null ? null : bottle.GetComponent<BottleFlipController>();
        Rigidbody body = bottle == null ? null : bottle.GetComponent<Rigidbody>();
        GameObject manager = GameObject.Find("BottleFlipGameManager");
        GameObject camera = GameObject.Find("Main Camera");
        GameObject[] landingSurfaces = GameObject.FindGameObjectsWithTag("LandingSurface");
        if (bottle == null || controller == null || body == null || manager == null || camera == null || landingSurfaces.Length < 4)
        {
            throw new System.InvalidOperationException("Bottle Flip scene validation failed: missing core objects or components.");
        }
        CapsuleCollider capsule = bottle.GetComponent<CapsuleCollider>();
        if (!body.useGravity || capsule == null || capsule.direction != 1)
        {
            throw new System.InvalidOperationException("Bottle Flip scene validation failed: rigidbody/capsule setup is not ready for the shifted-COM physics model.");
        }
        Debug.Log($"Bottle Flip VR validation passed: {scene.path}, platforms={landingSurfaces.Length}, controller={controller.GetType().Name}");
    }

    private static GameObject CreateBottle(Material bottleMaterial, Material waterMaterial, Material capMaterial, Vector3 position)
    {
        GameObject root = new GameObject("Bottle");
        root.transform.position = position;
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = 0.45f;
        body.drag = 0.05f;
        body.angularDrag = 0.18f;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.maxAngularVelocity = 40f;
        CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
        capsule.radius = 0.3f;
        capsule.height = 1.5f;
        capsule.direction = 1;
        BottleFlipController controller = root.AddComponent<BottleFlipController>();

        ChildPrimitive(root, "BottleBody", PrimitiveType.Cylinder, new Vector3(0f, -0.08f, 0f), new Vector3(0.58f, 0.58f, 0.58f), bottleMaterial);
        ChildPrimitive(root, "WaterFill", PrimitiveType.Cylinder, new Vector3(0f, -0.22f, 0f), new Vector3(0.47f, 0.22f, 0.47f), waterMaterial);
        ChildPrimitive(root, "BottleNeck", PrimitiveType.Cylinder, new Vector3(0f, 0.58f, 0f), new Vector3(0.3f, 0.28f, 0.3f), bottleMaterial);
        ChildPrimitive(root, "Cap", PrimitiveType.Cylinder, new Vector3(0f, 0.91f, 0f), new Vector3(0.33f, 0.09f, 0.33f), capMaterial);
        return root;
    }

    private static Transform CreatePlatform(string name, Vector3 position, Vector3 scale, Material platformMaterial, Material accentMaterial)
    {
        GameObject platform = CreatePrimitive(name, PrimitiveType.Cube, position, scale, platformMaterial, "LandingSurface");
        ChildPrimitive(platform, "EdgeGlow", PrimitiveType.Cube, new Vector3(0f, 0.29f, 0f), new Vector3(0.96f, 0.035f, 0.96f), accentMaterial);
        return platform.transform;
    }

    private static GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, string tag)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetPositionAndRotation(position, Quaternion.identity);
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!string.IsNullOrEmpty(tag)) go.tag = tag;
        return go;
    }

    private static GameObject ChildPrimitive(GameObject parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject child = GameObject.CreatePrimitive(type);
        child.name = name;
        child.transform.SetParent(parent.transform, false);
        child.transform.localPosition = localPosition;
        child.transform.localScale = localScale;
        child.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(child.GetComponent<Collider>());
        return child;
    }

    private static Material Material(string name, Color color, float metallic, float smoothness)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private static void EnsureFolder(string folder)
    {
        string[] pieces = folder.Split('/');
        string current = pieces[0];
        for (int i = 1; i < pieces.Length; i++)
        {
            string next = current + "/" + pieces[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, pieces[i]);
            current = next;
        }
    }

    private static void EnsureTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        }
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }
}
