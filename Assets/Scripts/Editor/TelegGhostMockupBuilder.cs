using System.IO;
using TelegGhost.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TelegGhost.Editor
{
    public static class TelegGhostMockupBuilder
    {
        private const string ScenePath = "Assets/Scene/Mockup/TELEGHOST_Mockup.unity";
        private const string GhostFolder = "Assets/Resource/Prefabs/Ghosts";
        private const string GimmickFolder = "Assets/Resource/Prefabs/Gimmicks";
        private const string InputPath = "Assets/Data/Input/TELEGHOST.inputactions";

        private static Sprite squareSprite;
        private static Material litMaterial;
        private static Material unlitMaterial;
        private static Material pathMaterial;
        private static Material coneMaterial;

        [MenuItem("TELEGHOST/Build Mockup")]
        public static void Build()
        {
            EnsureProjectStructure();
            LoadSharedAssets();
            CreateGhostPrefab("TELE_Block", ObservationType.Tele, true);
            CreateGhostPrefab("STAR_Block", ObservationType.Star, true);
            CreateGhostPrefab("TELE_Wisp", ObservationType.Tele, false);
            CreateGhostPrefab("STAR_Wisp", ObservationType.Star, false);
            CreateSwitchPrefab();
            CreateDoorPrefab();
            CreateScene();
            AssetDatabase.SaveAssets();
            Debug.Log("TELEGHOST mockup was built successfully.");
        }

        private static void EnsureProjectStructure()
        {
            EnsureFolder("Assets/Resource/Materials");
            EnsureFolder(GhostFolder);
            EnsureFolder(GimmickFolder);
            EnsureFolder("Assets/Scene/Mockup");
            EnsureLayer("Player", 8);
            EnsureLayer("Ghost", 9);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (!string.IsNullOrEmpty(parent))
            {
                AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            }
        }

        private static void EnsureLayer(string layerName, int preferredIndex)
        {
            if (LayerMask.NameToLayer(layerName) >= 0)
            {
                return;
            }

            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers != null && preferredIndex >= 8 && preferredIndex < layers.arraySize)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(preferredIndex);
                if (string.IsNullOrEmpty(layer.stringValue))
                {
                    layer.stringValue = layerName;
                    tagManager.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void LoadSharedAssets()
        {
            squareSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            litMaterial = LoadOrCreateMaterial("Assets/Resource/Materials/Mockup_Lit.mat", "Universal Render Pipeline/2D/Sprite-Lit-Default", "Sprites/Default", Color.white);
            unlitMaterial = LoadOrCreateMaterial("Assets/Resource/Materials/Mockup_Unlit.mat", "Universal Render Pipeline/2D/Sprite-Unlit-Default", "Sprites/Default", Color.white);
            pathMaterial = LoadOrCreateMaterial("Assets/Resource/Materials/GhostPath.mat", "Sprites/Default", "Universal Render Pipeline/2D/Sprite-Unlit-Default", new Color(1f, 1f, 1f, 0.38f));
            coneMaterial = LoadOrCreateMaterial("Assets/Resource/Materials/VisionCone.mat", "Sprites/Default", "Universal Render Pipeline/2D/Sprite-Unlit-Default", new Color(1f, 1f, 1f, 0.045f));
        }

        private static Material LoadOrCreateMaterial(string path, string shaderName, string fallback, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find(shaderName) ?? Shader.Find(fallback);
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateGhostPrefab(string name, ObservationType type, bool rideable)
        {
            GameObject root = new GameObject(name) { layer = 9 };
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = rideable ? new Vector2(1.8f, 1f) : new Vector2(0.7f, 0.7f);
            collider.isTrigger = !rideable;

            Color bodyColor = type == ObservationType.Tele ? new Color(0.72f, 0.72f, 0.72f) : Color.white;
            GameObject visual = CreateSprite("Body", root.transform, Vector3.zero, rideable ? new Vector2(1.8f, 1f) : new Vector2(0.72f, 0.72f), bodyColor, 5, litMaterial);
            if (!rideable)
            {
                visual.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            }

            CreateSprite("EyeLeft", root.transform, new Vector3(-0.18f, 0.12f, -0.1f), new Vector2(0.1f, 0.17f), Color.black, 7, unlitMaterial);
            CreateSprite("EyeRight", root.transform, new Vector3(0.18f, 0.12f, -0.1f), new Vector2(0.1f, 0.17f), Color.black, 7, unlitMaterial);
            CreateText("Type", root.transform, new Vector3(0f, -0.23f, -0.2f), type == ObservationType.Tele ? "TELE" : "STAR", Color.black, rideable ? 0.075f : 0.045f, 8);

            GameObject pathObject = new GameObject("PathArrow");
            pathObject.transform.SetParent(root.transform, false);
            LineRenderer pathLine = pathObject.AddComponent<LineRenderer>();
            pathLine.useWorldSpace = true;
            pathLine.positionCount = 2;
            pathLine.startWidth = 0.09f;
            pathLine.endWidth = 0.09f;
            pathLine.numCapVertices = 4;
            pathLine.sortingOrder = 3;
            pathLine.sharedMaterial = pathMaterial;

            Transform destination = CreateSprite("Destination", root.transform, Vector3.right * 4f, new Vector2(0.3f, 0.3f), new Color(1f, 1f, 1f, 0.38f), 4, unlitMaterial).transform;
            destination.localRotation = Quaternion.Euler(0f, 0f, 45f);

            GhostMover2D mover = root.AddComponent<GhostMover2D>();
            SetEnum(mover, "observationType", (int)type);
            SetReference(mover, "observationCollider", collider);
            SetVector2(mover, "moveDirection", Vector2.right);
            SetFloat(mover, "moveDistance", 4f);
            SetFloat(mover, "moveSpeed", rideable ? 1.8f : 2.1f);
            SetBool(mover, "rideable", rideable);
            SetReference(mover, "pathLine", pathLine);
            SetReference(mover, "destinationMarker", destination);

            PrefabUtility.SaveAsPrefabAsset(root, $"{GhostFolder}/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        private static void CreateSwitchPrefab()
        {
            GameObject root = new GameObject("GhostSwitch");
            CreateSprite("Base", root.transform, Vector3.zero, new Vector2(1.2f, 0.25f), new Color(0.28f, 0.28f, 0.28f), 2, litMaterial);
            SpriteRenderer indicator = CreateSprite("Indicator", root.transform, new Vector3(0f, 0.17f), new Vector2(0.78f, 0.1f), new Color(0.22f, 0.22f, 0.22f), 3, litMaterial).GetComponent<SpriteRenderer>();
            BoxCollider2D trigger = root.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1.3f, 1.1f);
            trigger.offset = new Vector2(0f, 0.35f);
            GhostSwitch2D controller = root.AddComponent<GhostSwitch2D>();
            SetReference(controller, "indicatorRenderer", indicator);
            PrefabUtility.SaveAsPrefabAsset(root, $"{GimmickFolder}/GhostSwitch.prefab");
            Object.DestroyImmediate(root);
        }

        private static void CreateDoorPrefab()
        {
            GameObject root = new GameObject("Door");
            CreateSprite("DoorVisual", root.transform, Vector3.zero, new Vector2(0.65f, 4.8f), new Color(0.68f, 0.68f, 0.68f), 3, litMaterial);
            for (int i = -2; i <= 2; i++)
            {
                CreateSprite($"Stripe_{i}", root.transform, new Vector3(0f, i * 0.8f, -0.1f), new Vector2(0.7f, 0.07f), Color.black, 4, unlitMaterial);
            }

            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            root.AddComponent<BoxCollider2D>().size = new Vector2(0.65f, 4.8f);
            DoorController2D door = root.AddComponent<DoorController2D>();
            SetVector2(door, "openOffset", Vector2.up * 5f);
            SetFloat(door, "moveSpeed", 4f);
            PrefabUtility.SaveAsPrefabAsset(root, $"{GimmickFolder}/Door.prefab");
            Object.DestroyImmediate(root);
        }

        private static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform environment = new GameObject("Environment").transform;
            Transform gameplay = new GameObject("Gameplay").transform;
            Transform presentation = new GameObject("Presentation").transform;

            CreateEnvironment(environment);
            PlayerController2D player = CreatePlayer(gameplay, out PlayerVision2D vision);
            Camera camera = CreateCamera(presentation, player.transform);
            CreateGlobalLight(presentation);

            GhostMover2D teleBlock = InstantiateGhost("TELE_Block", gameplay, new Vector3(-8f, -2.5f), vision, 2.8f);
            GhostMover2D starTutorial = InstantiateGhost("STAR_Wisp", gameplay, new Vector3(-1f, -2.75f), vision, 3.5f);
            GhostMover2D starOcclusion = InstantiateGhost("STAR_Wisp", gameplay, new Vector3(9.2f, 1.2f), vision, 4.8f);

            DoorController2D tutorialDoor = InstantiatePrefab($"{GimmickFolder}/Door.prefab", gameplay, new Vector3(4.6f, -1f)).GetComponent<DoorController2D>();
            GhostSwitch2D tutorialSwitch = InstantiatePrefab($"{GimmickFolder}/GhostSwitch.prefab", gameplay, new Vector3(2.5f, -3.05f)).GetComponent<GhostSwitch2D>();
            SetReference(tutorialSwitch, "controlledDoor", tutorialDoor);

            DoorController2D finalDoor = InstantiatePrefab($"{GimmickFolder}/Door.prefab", gameplay, new Vector3(15.5f, -1f)).GetComponent<DoorController2D>();
            GhostSwitch2D finalSwitch = InstantiatePrefab($"{GimmickFolder}/GhostSwitch.prefab", gameplay, new Vector3(14f, 1.0f)).GetComponent<GhostSwitch2D>();
            SetReference(finalSwitch, "controlledDoor", finalDoor);

            Transform spawn0 = CreateChild("Spawn_0", gameplay, new Vector3(-12.5f, -2.35f));
            Transform spawn1 = CreateChild("Spawn_1", gameplay, new Vector3(-4.7f, -2.35f));
            Transform spawn2 = CreateChild("Spawn_2", gameplay, new Vector3(5.5f, -2.35f));

            GameObject completion = CreateText("Completion", camera.transform, new Vector3(0f, 0f, 10f), "STAGE COMPLETE", Color.white, 0.18f, 100);
            completion.SetActive(false);

            LevelFlow2D flow = new GameObject("LevelFlow").AddComponent<LevelFlow2D>();
            SetReference(flow, "player", player);
            SetObjectArray(flow, "checkpointSpawns", new Object[] { spawn0, spawn1, spawn2 });
            SetObjectArray(flow, "ghosts", new Object[] { teleBlock, starTutorial, starOcclusion });
            SetObjectArray(flow, "ghostSwitches", new Object[] { tutorialSwitch, finalSwitch });
            SetObjectArray(flow, "doors", new Object[] { tutorialDoor, finalDoor });
            SetReference(flow, "completionDisplay", completion);

            CreateLevelTrigger("Checkpoint_1", gameplay, new Vector2(-4.7f, -1f), new Vector2(0.8f, 5f), flow, LevelTriggerType.Checkpoint, 1);
            CreateLevelTrigger("Checkpoint_2", gameplay, new Vector2(5.5f, -1f), new Vector2(0.8f, 5f), flow, LevelTriggerType.Checkpoint, 2);
            CreateLevelTrigger("DeathZone", gameplay, new Vector2(2f, -6.5f), new Vector2(36f, 1f), flow, LevelTriggerType.Respawn, 0);
            CreateLevelTrigger("Goal", gameplay, new Vector2(17.3f, -1f), new Vector2(1f, 5f), flow, LevelTriggerType.Goal, 0);

            CreateText("TeleInstruction", presentation, new Vector3(-7.7f, 0.7f), "1  TELE: LOOK AWAY TO MOVE", new Color(0.65f, 0.65f, 0.65f), 0.065f, 30);
            CreateText("StarInstruction", presentation, new Vector3(0.8f, 0.2f), "2  STAR: KEEP IT IN SIGHT", new Color(0.65f, 0.65f, 0.65f), 0.065f, 30);
            CreateText("OcclusionInstruction", presentation, new Vector3(10.8f, 3.2f), "3  CLIMB TO SEE OVER THE WALL", new Color(0.65f, 0.65f, 0.65f), 0.06f, 30);
            CreateText("Exit", presentation, new Vector3(17.2f, 1.7f), "EXIT", Color.white, 0.12f, 40);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        private static void CreateEnvironment(Transform parent)
        {
            CreatePlatform("Floor_Start", parent, new Vector2(-11.5f, -3.55f), new Vector2(5f, 0.7f), 0.42f);
            CreatePlatform("Floor_TELE_End", parent, new Vector2(-3.75f, -3.55f), new Vector2(3.5f, 0.7f), 0.42f);
            CreatePlatform("Floor_STAR", parent, new Vector2(1.5f, -3.55f), new Vector2(7f, 0.7f), 0.42f);
            CreatePlatform("Floor_Occlusion", parent, new Vector2(11f, -3.55f), new Vector2(12f, 0.7f), 0.42f);
            CreatePlatform("LeftBoundary", parent, new Vector2(-14.2f, 0f), new Vector2(0.5f, 8f), 0.25f);
            CreatePlatform("OcclusionWall", parent, new Vector2(10.5f, -1.2f), new Vector2(0.8f, 4.6f), 0.3f);
            CreatePlatform("Step_1", parent, new Vector2(7.5f, -2.25f), new Vector2(1.4f, 0.3f), 0.38f);
            CreatePlatform("Step_2", parent, new Vector2(8.35f, -1.25f), new Vector2(1.3f, 0.3f), 0.38f);
            CreatePlatform("ObservationLedge", parent, new Vector2(9.5f, 0f), new Vector2(1.6f, 0.35f), 0.38f);
            CreatePlatform("ExitLedge", parent, new Vector2(17.2f, -1.5f), new Vector2(2.5f, 0.35f), 0.38f);
        }

        private static PlayerController2D CreatePlayer(Transform parent, out PlayerVision2D vision)
        {
            GameObject player = new GameObject("Player") { layer = 8 };
            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(-12.5f, -2.35f);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            player.AddComponent<BoxCollider2D>().size = new Vector2(0.72f, 1.45f);

            Transform facingVisual = CreateChild("FacingVisual", player.transform, Vector3.zero);
            CreateSprite("Body", facingVisual, Vector3.zero, new Vector2(0.72f, 1.45f), new Color(0.92f, 0.92f, 0.92f), 10, litMaterial);
            CreateSprite("Face", facingVisual, new Vector3(0.22f, 0.12f, -0.1f), new Vector2(0.09f, 0.26f), Color.black, 11, unlitMaterial);
            Transform groundCheck = CreateChild("GroundCheck", player.transform, new Vector3(0f, -0.78f));
            Transform visionRoot = CreateChild("Vision", player.transform, new Vector3(0.36f, 0.18f));
            MeshFilter meshFilter = visionRoot.gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = visionRoot.gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = coneMaterial;
            meshRenderer.sortingOrder = -2;
            Light2D freeformLight = visionRoot.gameObject.AddComponent<Light2D>();
            freeformLight.lightType = Light2D.LightType.Freeform;
            freeformLight.intensity = 0.9f;
            freeformLight.color = Color.white;
            freeformLight.shapeLightFalloffSize = 0.15f;

            PlayerController2D controller = player.AddComponent<PlayerController2D>();
            SetReference(controller, "inputActions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath));
            SetReference(controller, "groundCheck", groundCheck);
            SetReference(controller, "facingRoot", facingVisual);
            SetInt(controller, "groundMask", 1);

            vision = player.AddComponent<PlayerVision2D>();
            SetReference(vision, "player", controller);
            SetReference(vision, "visionOrigin", visionRoot);
            SetReference(vision, "visionMeshFilter", meshFilter);
            SetReference(vision, "visionLight", freeformLight);
            SetInt(vision, "obstructionMask", 1);
            SetInt(vision, "rayCount", 61);
            SetFloat(vision, "fieldOfView", 90f);
            SetFloat(vision, "viewDistance", 9f);
            return controller;
        }

        private static Camera CreateCamera(Transform parent, Transform target)
        {
            GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = new Vector3(-8.8f, 0f, -10f);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.006f, 0.006f, 0.006f);
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
            SetReference(follow, "target", target);
            SetFloat(follow, "minimumX", -8.8f);
            SetFloat(follow, "maximumX", 11.5f);

            CreateText("Title", cameraObject.transform, new Vector3(-7f, 4.35f, 10f), "TELEGHOST", Color.white, 0.12f, 100);
            CreateText("Controls", cameraObject.transform, new Vector3(-4.5f, 3.85f, 10f), "A / D : MOVE     SPACE : JUMP     R : RESET", new Color(0.7f, 0.7f, 0.7f), 0.055f, 100);
            return camera;
        }

        private static void CreateGlobalLight(Transform parent)
        {
            GameObject lightObject = new GameObject("Global Darkness");
            lightObject.transform.SetParent(parent, false);
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.2f;
            light.color = new Color(0.75f, 0.75f, 0.75f);
        }

        private static GhostMover2D InstantiateGhost(string prefabName, Transform parent, Vector3 position, PlayerVision2D vision, float distance)
        {
            GhostMover2D mover = InstantiatePrefab($"{GhostFolder}/{prefabName}.prefab", parent, position).GetComponent<GhostMover2D>();
            SetReference(mover, "playerVision", vision);
            SetFloat(mover, "moveDistance", distance);
            return mover;
        }

        private static GameObject InstantiatePrefab(string path, Transform parent, Vector3 position)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            return instance;
        }

        private static void CreateLevelTrigger(string name, Transform parent, Vector2 position, Vector2 size, LevelFlow2D flow, LevelTriggerType type, int checkpointIndex)
        {
            GameObject triggerObject = new GameObject(name);
            triggerObject.transform.SetParent(parent, false);
            triggerObject.transform.position = position;
            BoxCollider2D collider = triggerObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = size;
            LevelTrigger2D trigger = triggerObject.AddComponent<LevelTrigger2D>();
            SetReference(trigger, "levelFlow", flow);
            SetEnum(trigger, "triggerType", (int)type);
            SetInt(trigger, "checkpointIndex", checkpointIndex);
        }

        private static GameObject CreatePlatform(string name, Transform parent, Vector2 position, Vector2 size, float gray)
        {
            GameObject platform = CreateSprite(name, parent, position, size, new Color(gray, gray, gray), 0, litMaterial);
            BoxCollider2D collider = platform.AddComponent<BoxCollider2D>();
            collider.size = squareSprite != null ? squareSprite.bounds.size : Vector2.one;
            return platform;
        }

        private static GameObject CreateSprite(string name, Transform parent, Vector3 position, Vector2 size, Color color, int order, Material material)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            Vector2 spriteSize = squareSprite != null ? squareSprite.bounds.size : Vector2.one;
            gameObject.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            renderer.sharedMaterial = material;
            return gameObject;
        }

        private static GameObject CreateText(string name, Transform parent, Vector3 position, string value, Color color, float size, int order)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            TextMesh text = gameObject.AddComponent<TextMesh>();
            text.text = value;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.characterSize = size;
            text.fontSize = 64;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sortingOrder = order;
            return gameObject;
        }

        private static Transform CreateChild(string name, Transform parent, Vector3 position)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }

        private static SerializedProperty FindProperty(Object target, string propertyName, out SerializedObject serialized)
        {
            serialized = new SerializedObject(target);
            return serialized.FindProperty(propertyName);
        }

        private static void SetReference(Object target, string propertyName, Object value)
        {
            SerializedProperty property = FindProperty(target, propertyName, out SerializedObject serialized);
            if (property != null) property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedProperty property = FindProperty(target, propertyName, out SerializedObject serialized);
            if (property != null) property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector2(Object target, string propertyName, Vector2 value)
        {
            SerializedProperty property = FindProperty(target, propertyName, out SerializedObject serialized);
            if (property != null) property.vector2Value = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            SerializedProperty property = FindProperty(target, propertyName, out SerializedObject serialized);
            if (property != null) property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedProperty property = FindProperty(target, propertyName, out SerializedObject serialized);
            if (property != null) property.boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            SerializedProperty property = FindProperty(target, propertyName, out SerializedObject serialized);
            if (property != null) property.enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(Object target, string propertyName, Object[] values)
        {
            SerializedProperty property = FindProperty(target, propertyName, out SerializedObject serialized);
            if (property != null)
            {
                property.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
