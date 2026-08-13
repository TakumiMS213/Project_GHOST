using System.Collections.Generic;
using System.IO;
using TelegGhost.Runtime;
using TelegGhost.Runtime.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace TelegGhost.Editor.Stage
{
    public static class GameStageBuilder
    {
        private const string ImageRoot = "Assets/Image/Game/Prototype";
        private const string PrefabRoot = "Assets/Resource/Prefabs/Game";
        private const string MaterialRoot = "Assets/Resource/Materials/Game";
        private const string FontRoot = "Assets/Resource/Fonts/Game";
        private const string SceneRoot = "Assets/Scene/Game";
        private const string StageSceneRoot = SceneRoot + "/Stages";
        private const string SystemSceneRoot = SceneRoot + "/System";
        private const string SquareTexturePath = ImageRoot + "/Square.png";
        private const string GhostTexturePath = ImageRoot + "/GHOST_base.png";
        private const string VisionMaterialPath = MaterialRoot + "/PlayerVision_White.mat";
        private const string GameFontPath = FontRoot + "/NotoSansJP-VF.ttf";
        private const string PlayerPrefabPath = PrefabRoot + "/Character/Player_Square.prefab";
        private const string BlockPrefabPath = PrefabRoot + "/Environment/StageBlock_Square.prefab";
        private const string DoorPrefabPath = PrefabRoot + "/Gimmick/Door_Square.prefab";
        private const string SwitchPrefabPath = PrefabRoot + "/Gimmick/Switch_Square.prefab";
        private const string BridgePrefabPath = PrefabRoot + "/Gimmick/SpawnBridge_Square.prefab";
        private const string FragileFloorPrefabPath = PrefabRoot + "/Gimmick/FragileFloor_Square.prefab";
        private const string CheckpointPrefabPath = PrefabRoot + "/Gimmick/Checkpoint_Square.prefab";
        private const string TeleRidePrefabPath = PrefabRoot + "/Ghost/TELE_Square_Rideable.prefab";
        private const string TeleUtilityPrefabPath = PrefabRoot + "/Ghost/TELE_Square_Utility.prefab";
        private const string StarRidePrefabPath = PrefabRoot + "/Ghost/STAR_Square_Rideable.prefab";
        private const string StarUtilityPrefabPath = PrefabRoot + "/Ghost/STAR_Square_Utility.prefab";
        private const string PulseAudioPath = "Assets/Resources/Short_Accent07-1(Dry).mp3";
        private const int PlayerLayer = 8;
        private const int GhostLayer = 9;
        private const int SightBlockLayer = 10;
        private const int FragileFloorLayer = 11;
        private const int RideableGhostLayer = 12;

        private static Sprite squareSprite;
        private static Sprite ghostSprite;
        private static Material visionMaterial;
        private static Font gameFont;
        private static GameObject playerPrefab;
        private static GameObject blockPrefab;
        private static GameObject doorPrefab;
        private static GameObject switchPrefab;
        private static GameObject bridgePrefab;
        private static GameObject fragileFloorPrefab;
        private static GameObject checkpointPrefab;
        private static GameObject teleRidePrefab;
        private static GameObject teleUtilityPrefab;
        private static GameObject starRidePrefab;
        private static GameObject starUtilityPrefab;

        private sealed class StageContext
        {
            public Scene Scene;
            public float Width;
            public float Height;
            public Transform Environment;
            public Transform Ghosts;
            public Transform Gimmicks;
            public Transform Labels;
            public PlayerController2D Player;
            public Transform SpawnPoint;
            public GameStageFlow2D Flow;
            public readonly List<GhostPulseController2D> Pulses = new List<GhostPulseController2D>();
            public readonly List<GhostSwitch2D> Switches = new List<GhostSwitch2D>();
            public readonly List<DoorController2D> Doors = new List<DoorController2D>();
            public readonly List<SpawnBridge2D> Bridges = new List<SpawnBridge2D>();
            public readonly List<FragileFloor2D> FragileFloors = new List<FragileFloor2D>();
        }

        [MenuItem("Tools/TELEGHOST/Build Game Stages")]
        public static void BuildAll()
        {
            PlayerSettings.runInBackground = true;
            EnsureFoldersAndLayers();
            CreateSquareTexture();
            LoadGhostSprite();
            CreateVisionMaterial();
            LoadGameFont();
            CreatePrefabs();
            AssetDatabase.DeleteAsset(StageSceneRoot + "/Stage13_TELEChain.unity");

            for (int stage = 1; stage <= 15; stage++)
            {
                BuildStage(stage);
            }

            BuildStageSelect();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(SystemSceneRoot + "/StageSelect.unity", OpenSceneMode.Single);
            Debug.Log("TELEGHOST: Version 1.1 stage specification built.");
        }

        private static void EnsureFoldersAndLayers()
        {
            string[] paths =
            {
                ImageRoot,
                MaterialRoot,
                FontRoot,
                PrefabRoot + "/Character",
                PrefabRoot + "/Environment",
                PrefabRoot + "/Ghost",
                PrefabRoot + "/Gimmick",
                StageSceneRoot,
                SystemSceneRoot,
                "Assets/Data/Game/Stage",
                "Assets/Data/Game/Evaluation"
            };

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            for (int i = 0; i < paths.Length; i++)
            {
                Directory.CreateDirectory(Path.Combine(projectRoot, paths[i]));
            }

            EnsureLayer(SightBlockLayer, "SightBlock");
            EnsureLayer(FragileFloorLayer, "FragileFloor");
            EnsureLayer(RideableGhostLayer, "RideableGhost");
            AssetDatabase.Refresh();
        }

        private static void EnsureLayer(int index, string layerName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                return;
            }

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            if (layers == null || index < 0 || index >= layers.arraySize)
            {
                return;
            }

            SerializedProperty layer = layers.GetArrayElementAtIndex(index);
            if (string.IsNullOrEmpty(layer.stringValue) || layer.stringValue == layerName)
            {
                layer.stringValue = layerName;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CreateSquareTexture()
        {
            string fullPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, SquareTexturePath);
            if (!File.Exists(fullPath))
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                File.WriteAllBytes(fullPath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SquareTexturePath, ImportAssetOptions.ForceSynchronousImport);
            }

            TextureImporter importer = AssetImporter.GetAtPath(SquareTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 1f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SquareTexturePath);
        }

        private static void LoadGhostSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(GhostTexturePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16f;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            ghostSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GhostTexturePath);
            if (ghostSprite == null)
            {
                Debug.LogError("TELEGHOST: GHOST_base sprite could not be loaded.");
            }
        }

        private static void CreateVisionMaterial()
        {
            visionMaterial = AssetDatabase.LoadAssetAtPath<Material>(VisionMaterialPath);
            if (visionMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default")
                    ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null)
                {
                    Debug.LogError("TELEGHOST: A transparent sprite shader is required for the vision area.");
                    return;
                }

                visionMaterial = new Material(shader) { name = "PlayerVision_White" };
                AssetDatabase.CreateAsset(visionMaterial, VisionMaterialPath);
            }

            visionMaterial.color = new Color(1f, 1f, 1f, 0.14f);
            EditorUtility.SetDirty(visionMaterial);
        }

        private static void LoadGameFont()
        {
            AssetDatabase.ImportAsset(GameFontPath, ImportAssetOptions.ForceSynchronousImport);
            TrueTypeFontImporter importer = AssetImporter.GetAtPath(GameFontPath) as TrueTypeFontImporter;
            if (importer != null)
            {
                bool requiresReimport = importer.fontTextureCase != FontTextureCase.Dynamic
                    || !importer.includeFontData;
                if (requiresReimport)
                {
                    importer.fontTextureCase = FontTextureCase.Dynamic;
                    importer.includeFontData = true;
                    importer.SaveAndReimport();
                }
            }

            gameFont = AssetDatabase.LoadAssetAtPath<Font>(GameFontPath);
            if (gameFont == null)
            {
                Debug.LogError("TELEGHOST: Japanese game font could not be loaded.");
            }
        }

        private static void CreatePrefabs()
        {
            blockPrefab = CreateBlockPrefab();
            doorPrefab = CreateDoorPrefab();
            switchPrefab = CreateSwitchPrefab();
            bridgePrefab = CreateBridgePrefab();
            fragileFloorPrefab = CreateFragileFloorPrefab();
            checkpointPrefab = CreateCheckpointPrefab();
            teleRidePrefab = CreateGhostPrefab(TeleRidePrefabPath, ObservationType.Tele, true);
            teleUtilityPrefab = CreateGhostPrefab(TeleUtilityPrefabPath, ObservationType.Tele, false);
            starRidePrefab = CreateGhostPrefab(StarRidePrefabPath, ObservationType.Star, true);
            starUtilityPrefab = CreateGhostPrefab(StarUtilityPrefabPath, ObservationType.Star, false);
            playerPrefab = CreatePlayerPrefab();
        }

        private static GameObject CreateBlockPrefab()
        {
            GameObject root = new GameObject("StageBlock_Square");
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = new Color(0.22f, 0.23f, 0.27f, 1f);
            root.AddComponent<BoxCollider2D>();
            return SavePrefab(root, BlockPrefabPath);
        }

        private static GameObject CreateDoorPrefab()
        {
            GameObject root = new GameObject("Door_Square");
            BoxCollider2D blocker = root.AddComponent<BoxCollider2D>();
            blocker.size = new Vector2(0.65f, 40f);
            blocker.offset = new Vector2(0f, 19f);
            Transform visual = CreateChild(root.transform, "ClosedVisual", Vector3.zero);
            SpriteRenderer renderer = CreateSpriteChild(visual, "Body", new Color(0.78f, 0.8f, 0.84f, 1f), 15);
            renderer.transform.localScale = new Vector3(0.65f, 2.6f, 1f);
            DoorController2D door = root.AddComponent<DoorController2D>();
            SetObject(door, "blockingCollider", blocker);
            SetObject(door, "closedVisual", visual.gameObject);
            return SavePrefab(root, DoorPrefabPath);
        }

        private static GameObject CreateSwitchPrefab()
        {
            GameObject root = new GameObject("Switch_Square");
            SpriteRenderer renderer = CreateSpriteChild(
                root.transform,
                "Body",
                new Color(0.28f, 0.3f, 0.34f, 1f),
                8);
            renderer.transform.localScale = new Vector3(0.9f, 0.18f, 1f);
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 0.2f);
            collider.isTrigger = true;
            GhostSwitch2D target = root.AddComponent<GhostSwitch2D>();
            SetObject(target, "indicatorRenderer", renderer);
            SetBool(target, "allowPlayer", true);
            SetBool(target, "allowGhosts", true);
            return SavePrefab(root, SwitchPrefabPath);
        }

        private static GameObject CreateBridgePrefab()
        {
            GameObject root = new GameObject("SpawnBridge_Square");
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = new Color(0.66f, 0.69f, 0.75f, 1f);
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            SpawnBridge2D bridge = root.AddComponent<SpawnBridge2D>();
            SetObjectArray(bridge, "bridgeColliders", new Object[] { collider });
            SetObjectArray(bridge, "bridgeRenderers", new Object[] { renderer });
            return SavePrefab(root, BridgePrefabPath);
        }

        private static GameObject CreateFragileFloorPrefab()
        {
            GameObject root = new GameObject("FragileFloor_Square") { layer = FragileFloorLayer };
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = new Color(0.43f, 0.45f, 0.5f, 1f);
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            FragileFloor2D floor = root.AddComponent<FragileFloor2D>();
            SetObject(floor, "floorCollider", collider);
            SetObjectArray(floor, "floorRenderers", new Object[] { renderer });
            SetInt(floor, "giantMask", 1 << GhostLayer);
            SetFloat(floor, "breakDelay", 0.1f);
            return SavePrefab(root, FragileFloorPrefabPath);
        }

        private static GameObject CreateCheckpointPrefab()
        {
            GameObject root = new GameObject("Checkpoint_Square");
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.8f, 1.8f);
            collider.offset = new Vector2(0f, 0.9f);
            Transform visual = CreateChild(root.transform, "Visual", new Vector3(0f, 0.9f, 0f));
            SpriteRenderer renderer = CreateSpriteChild(visual, "Body", new Color(0.3f, 0.32f, 0.36f, 1f), 8);
            renderer.transform.localScale = new Vector3(0.28f, 1.8f, 1f);
            StageCheckpoint2D checkpoint = root.AddComponent<StageCheckpoint2D>();
            SetObject(checkpoint, "indicatorRenderer", renderer);
            return SavePrefab(root, CheckpointPrefabPath);
        }

        private static GameObject CreateGhostPrefab(string path, ObservationType type, bool rideable)
        {
            string typeName = type == ObservationType.Tele ? "TELE" : "STAR";
            GameObject root = new GameObject(typeName + "_Square_" + (rideable ? "Rideable" : "Utility"))
            {
                layer = rideable ? RideableGhostLayer : GhostLayer
            };
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = rideable ? new Vector2(2f, 0.6f) : new Vector2(0.8f, 0.8f);
            collider.isTrigger = !rideable;

            Transform observationPoint = CreateChild(root.transform, "ObservationPoint", Vector3.zero);
            Transform visuals = CreateChild(root.transform, "Visuals", Vector3.zero);
            Vector3 visualScale = rideable ? new Vector3(2f, 0.6f, 1f) : new Vector3(0.8f, 0.8f, 1f);
            SpriteRenderer bodyRenderer = CreateSpriteChild(
                visuals,
                "Body",
                type == ObservationType.Tele
                    ? new Color(0.72f, 0.74f, 0.78f, 1f)
                    : new Color(0.8f, 0.82f, 0.86f, 1f),
                10);
            bodyRenderer.sprite = ghostSprite;
            bodyRenderer.transform.localScale = visualScale;
            SpriteRenderer fillRenderer = CreateSpriteChild(visuals, "PulseFill", Color.white, 11);
            fillRenderer.sprite = ghostSprite;
            fillRenderer.transform.localScale = visualScale;
            CreateGhostFace(visuals, type, rideable);
            TextMesh arrow = CreateText(
                visuals,
                "DirectionArrow",
                ">>",
                new Vector3(1.35f, 0f, 0f),
                0.18f,
                new Color(1f, 1f, 1f, 0.58f),
                TextAnchor.MiddleLeft);

            GhostObservationDetector2D detector = root.AddComponent<GhostObservationDetector2D>();
            SetObject(detector, "observationPoint", observationPoint);
            GhostController2D controller = root.AddComponent<GhostController2D>();
            SetEnum(controller, "observationType", (int)type);
            SetObject(controller, "observationDetector", detector);
            SetObject(controller, "observedVisualRoot", visuals.gameObject);
            SetObjectArray(controller, "observedOnlyRenderers", new Object[] { arrow.GetComponent<Renderer>() });
            SetFloat(controller, "unobservedAlpha", 0f);

            GhostMoveAction2D move = root.AddComponent<GhostMoveAction2D>();
            SetObject(move, "controlledBody", body);
            SetObject(move, "movementCollider", collider);
            SetVector2Array(move, "pathNodes", new[] { Vector2.zero, Vector2.right });
            SetEnum(move, "routeMode", (int)GhostMoveRouteMode.PingPong);
            SetObject(move, "directionIndicator", arrow.transform);
            SetFloat(move, "moveDurationPerUnit", 0.15f);
            SetFloat(move, "impactHoldDuration", 0.05f);
            SetInt(move, "collisionMask", 1 << 0);
            SetBool(move, "rideable", rideable);

            GhostPulseFill2D fill = root.AddComponent<GhostPulseFill2D>();
            SetObject(fill, "bodyRenderer", bodyRenderer);
            SetObject(fill, "fillRenderer", fillRenderer);
            SetObject(fill, "fillTransform", fillRenderer.transform);
            SetObject(fill, "flashRoot", visuals);
            AudioSource audioSource = root.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            GhostPulseController2D pulse = root.AddComponent<GhostPulseController2D>();
            SetObject(pulse, "ghostController", controller);
            SetObject(pulse, "ghostAction", move);
            SetObject(pulse, "pulseVisual", fill);
            SetObject(pulse, "visibilityRenderer", bodyRenderer);
            SetObject(pulse, "pulseAudioSource", audioSource);
            SetObject(pulse, "pulseClip", AssetDatabase.LoadAssetAtPath<AudioClip>(PulseAudioPath));
            SetFloat(pulse, "pulseVolume", 0.1625f);
            SetFloat(pulse, "pulseInterval", 0.5f);
            return SavePrefab(root, path);
        }

        private static void CreateGhostFace(
            Transform parent,
            ObservationType type,
            bool rideable)
        {
            Transform face = CreateChild(parent, "Face", new Vector3(0f, 0f, -0.05f));
            face.localScale = rideable
                ? new Vector3(0.68f, 0.44f, 1f)
                : new Vector3(0.62f, 0.62f, 1f);

            float eyeArmY = type == ObservationType.Tele ? 0.08f : 0.23f;
            CreateFaceBlock(face, "LeftEyeArm", new Vector2(-0.25f, eyeArmY), new Vector2(0.24f, 0.12f));
            CreateFaceBlock(face, "LeftEyeStem", new Vector2(-0.14f, 0.17f), new Vector2(0.1f, 0.3f));
            CreateFaceBlock(face, "RightEyeArm", new Vector2(0.25f, eyeArmY), new Vector2(0.24f, 0.12f));
            CreateFaceBlock(face, "RightEyeStem", new Vector2(0.14f, 0.17f), new Vector2(0.1f, 0.3f));

            if (type == ObservationType.Tele)
            {
                CreateFaceBlock(face, "Mouth_LeftLow", new Vector2(-0.28f, -0.23f), new Vector2(0.12f, 0.12f));
                CreateFaceBlock(face, "Mouth_LeftHigh", new Vector2(-0.14f, -0.12f), new Vector2(0.12f, 0.12f));
                CreateFaceBlock(face, "Mouth_CenterLow", new Vector2(0f, -0.23f), new Vector2(0.12f, 0.12f));
                CreateFaceBlock(face, "Mouth_RightHigh", new Vector2(0.14f, -0.12f), new Vector2(0.12f, 0.12f));
                CreateFaceBlock(face, "Mouth_RightLow", new Vector2(0.28f, -0.23f), new Vector2(0.12f, 0.12f));
                return;
            }

            CreateFaceBlock(face, "Smile_Left", new Vector2(-0.25f, -0.13f), new Vector2(0.1f, 0.2f));
            CreateFaceBlock(face, "Smile_Bottom", new Vector2(0f, -0.23f), new Vector2(0.4f, 0.1f));
            CreateFaceBlock(face, "Smile_Right", new Vector2(0.25f, -0.13f), new Vector2(0.1f, 0.2f));
        }

        private static void CreateFaceBlock(
            Transform parent,
            string name,
            Vector2 localPosition,
            Vector2 localScale)
        {
            SpriteRenderer renderer = CreateSpriteChild(parent, name, Color.black, 12);
            renderer.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            renderer.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
        }

        private static GameObject CreatePlayerPrefab()
        {
            GameObject root = new GameObject("Player_Square") { layer = PlayerLayer };
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 24f / 9.81f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            CapsuleCollider2D collider = root.AddComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.7f, 1.6f);

            Transform facingRoot = CreateChild(root.transform, "FacingVisual", Vector3.zero);
            SpriteRenderer bodyRenderer = CreateSpriteChild(facingRoot, "Body", new Color(0.94f, 0.95f, 0.98f, 1f), 20);
            bodyRenderer.transform.localScale = new Vector3(0.7f, 1.6f, 1f);
            SpriteRenderer eye = CreateSpriteChild(facingRoot, "Eye", Color.black, 21);
            eye.transform.localPosition = new Vector3(0.24f, 0.2f, -0.05f);
            eye.transform.localScale = new Vector3(0.12f, 0.12f, 1f);

            Transform groundCheck = CreateChild(root.transform, "GroundCheck", new Vector3(0f, -0.82f, 0f));
            Transform vision = CreateChild(root.transform, "Vision", new Vector3(0f, 0.15f, 0.1f));
            MeshFilter visionMesh = vision.gameObject.AddComponent<MeshFilter>();
            MeshRenderer visionRenderer = vision.gameObject.AddComponent<MeshRenderer>();
            visionRenderer.sharedMaterial = visionMaterial;
            visionRenderer.sortingOrder = -2;
            visionRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            visionRenderer.receiveShadows = false;
            Light2D visionLight = vision.gameObject.AddComponent<Light2D>();
            visionLight.lightType = Light2D.LightType.Freeform;
            visionLight.color = Color.white;
            visionLight.intensity = 0.7f;
            visionLight.shapeLightFalloffSize = 1.1f;
            visionLight.falloffIntensity = 0.9f;
            visionLight.SetShapePath(new[]
            {
                Vector3.zero,
                new Vector3(1f, -0.28f),
                new Vector3(1f, 0.28f)
            });

            Transform bounce = CreateChild(vision, "ReflectedLight", Vector3.zero);
            Light2D reflectedLight = bounce.gameObject.AddComponent<Light2D>();
            reflectedLight.lightType = Light2D.LightType.Point;
            reflectedLight.color = new Color(0.88f, 0.92f, 1f, 1f);
            reflectedLight.intensity = 0.28f;
            reflectedLight.pointLightInnerRadius = 0.25f;
            reflectedLight.pointLightOuterRadius = 2.2f;
            reflectedLight.falloffIntensity = 0.75f;

            PlayerController2D player = root.AddComponent<PlayerController2D>();
            InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Data/Input/TELEGHOST.inputactions");
            SetObject(player, "inputActions", input);
            SetObject(player, "groundCheck", groundCheck);
            SetInt(
                player,
                "groundMask",
                (1 << 0) | (1 << FragileFloorLayer) | (1 << RideableGhostLayer));
            SetObject(player, "facingRoot", facingRoot);
            SetFloat(player, "moveSpeed", 6f);
            SetFloat(player, "jumpImpulse", 10.5f);
            SetFloat(player, "coyoteTime", 0.1f);
            SetFloat(player, "jumpBufferTime", 0.1f);

            GhostObserver2D observer = root.AddComponent<GhostObserver2D>();
            SetObject(observer, "directionSource", player);
            SetObject(observer, "visionOrigin", vision);
            SetObject(observer, "visionMeshFilter", visionMesh);
            SetObject(observer, "visionLight", visionLight);
            SetInt(observer, "rayCount", 41);
            SetFloat(observer, "fieldOfView", 32f);
            SetFloat(observer, "viewDistance", 12f);
            SetInt(observer, "obstructionMask", 1 << SightBlockLayer);
            return SavePrefab(root, PlayerPrefabPath);
        }

        private static void BuildStage(int stage)
        {
            StageContext context;
            switch (stage)
            {
                case 1: context = Stage01(); break;
                case 2: context = Stage02(); break;
                case 3: context = Stage03(); break;
                case 4: context = Stage04(); break;
                case 5: context = Stage05(); break;
                case 6: context = Stage06(); break;
                case 7: context = Stage07(); break;
                case 8: context = Stage08(); break;
                case 9: context = Stage09(); break;
                case 10: context = Stage10(); break;
                case 11: context = Stage11(); break;
                case 12: context = Stage12(); break;
                case 13: context = Stage13(); break;
                case 14: context = Stage14(); break;
                default: context = Stage15(); break;
            }

            FinishStage(context, stage);
        }

        private static StageContext Stage01()
        {
            StageContext c = BeginStage(1, 28f, 8f, new Vector2(1.5f, 1f), 1);
            Ground(c, "Ground_A", 0f, 0f, 12f, 1f);
            Ground(c, "Ground_C", 15f, 0f, 13f, 1f);
            DoorController2D dA = Door(c, "D1-A", new Vector2(7.2f, 1f));
            Switch(c, "SW1-A", new Vector2(5.5f, 1.05f), dA);
            Ghost(c, "T1-A", ObservationType.Tele, false, NodePath(3.5f, 1.4f, 4.5f, 1.4f, 5.5f, 1.4f, 6.5f, 1.4f));
            DoorController2D dB = Door(c, "D1-B", new Vector2(17f, 1f));
            GhostSwitch2D swB = Switch(c, "SW1-B", new Vector2(15.5f, 1.05f), dB);
            Bridge(c, "Bridge_B", 12f, 0.8f, 3f, 0.3f, swB);
            Ghost(c, "T1-B", ObservationType.Tele, false, NodePath(10f, 2f, 11f, 2f, 12f, 2f, 13f, 2f, 14f, 2f, 15.5f, 1.4f));
            GhostSwitch2D swC = Switch(c, "SW1-C", new Vector2(23f, 1.05f), null);
            Ghost(c, "T1-C", ObservationType.Tele, false, HorizontalPath(20f, 25f, 1.4f));
            LockGoal(Goal(c, new Vector2(27f, 1.2f)), swC);
            return c;
        }

        private static StageContext Stage02()
        {
            StageContext c = BeginStage(2, 28f, 8f, new Vector2(1.5f, 1f), -1);
            Ground(c, "Ground_A", 0f, 0f, 12f, 1f);
            Ground(c, "Ground_C", 15f, 0f, 13f, 1f);
            DoorController2D dA = Door(c, "D2-A", new Vector2(7.2f, 1f));
            Switch(c, "SW2-A", new Vector2(5.5f, 1.05f), dA);
            Ghost(c, "S2-A", ObservationType.Star, false, NodePath(3.5f, 1.4f, 4.5f, 1.4f, 5.5f, 1.4f, 6.5f, 1.4f));
            DoorController2D dB = Door(c, "D2-B", new Vector2(17f, 1f));
            GhostSwitch2D swB = Switch(c, "SW2-B", new Vector2(15.5f, 1.05f), dB);
            Bridge(c, "Bridge_B", 12f, 0.8f, 3f, 0.3f, swB);
            Ghost(c, "S2-B", ObservationType.Star, false, NodePath(10f, 2f, 11f, 2f, 12f, 2f, 13f, 2f, 14f, 2f, 15.5f, 1.4f));
            GhostSwitch2D swC = Switch(c, "SW2-C", new Vector2(23f, 1.05f), null);
            Ghost(c, "S2-C", ObservationType.Star, false, HorizontalPath(20f, 25f, 1.4f));
            LockGoal(Goal(c, new Vector2(27f, 1.2f)), swC);
            return c;
        }

        private static StageContext Stage03()
        {
            StageContext c = BeginStage(3, 32f, 11f, new Vector2(1.5f, 1f), 1);
            Ground(c, "Ground_A", 0f, 0f, 10f, 1f);
            Ground(c, "Ground_B", 10f, 0f, 7f, 1f);
            Ground(c, "Ground_C", 17f, 0f, 15f, 1f);
            Ground(c, "Upper_B", 8f, 3f, 5f, 0.6f);
            Ground(c, "Upper_C", 18f, 4f, 6f, 0.6f);
            SightBlock(c, "SightBlock_Center", 15.5f, 1f, 0.8f, 5.5f);
            DoorController2D dA = Door(c, "D3-A", new Vector2(8f, 1f));
            Switch(c, "SW3-A", new Vector2(6f, 1.05f), dA);
            Ghost(c, "T3-A", ObservationType.Tele, false, HorizontalPath(4f, 6f, 1.4f));
            DoorController2D dB = Door(c, "D3-B", new Vector2(16.8f, 1f));
            Switch(c, "SW3-B", new Vector2(14f, 3.65f), dB);
            Ghost(c, "S3-B", ObservationType.Star, false, HorizontalPath(11f, 14f, 4f));
            DoorController2D dC = Door(c, "D3-C", new Vector2(26f, 1f));
            GhostSwitch2D swC = Switch(c, "SW3-C", new Vector2(18f, 4.45f), null);
            GhostSwitch2D swD = Switch(c, "SW3-D", new Vector2(24f, 1.05f), null);
            Ghost(c, "T3-C", ObservationType.Tele, false, HorizontalPath(20f, 18f, 4.8f));
            Ghost(c, "S3-C", ObservationType.Star, false, HorizontalPath(22f, 24f, 1.4f));
            MultiGate(c, "Gate_D3-C", dC, swC, swD);
            Goal(c, new Vector2(31f, 1.2f));
            return c;
        }

        private static StageContext Stage04()
        {
            StageContext c = BeginStage(4, 34f, 10f, new Vector2(1.5f, 1f), 1);
            Ground(c, "Ground_00", 0f, 0f, 5f, 1f);
            Ground(c, "Ground_10", 10f, 0f, 4f, 1f);
            Ground(c, "Ground_20", 20f, 0f, 4f, 1f);
            Ground(c, "Ground_30", 30f, 0f, 4f, 1f);
            Ground(c, "Ledge_12", 12f, 3.2f, 3f, 0.5f);
            Ground(c, "Ledge_27", 27f, 3.5f, 3f, 0.5f);
            Ghost(c, "T4-P1", ObservationType.Tele, true, HorizontalPath(4.5f, 9.5f, 1.8f));
            Checkpoint(c, "CP4-1", new Vector2(11f, 1f));
            Ghost(c, "S4-P2", ObservationType.Star, true, HorizontalPath(14.5f, 19.5f, 1.8f));
            Checkpoint(c, "CP4-2", new Vector2(21f, 1f));
            Ghost(c, "T4-P3", ObservationType.Tele, true, HorizontalPath(24.5f, 29.5f, 2.2f));
            Goal(c, new Vector2(33f, 1.2f));
            return c;
        }

        private static StageContext Stage05()
        {
            StageContext c = BeginStage(5, 34f, 10f, new Vector2(1.5f, 1f), 1);
            Ground(c, "Ground_00", 0f, 0f, 5f, 1f);
            Ground(c, "Ground_10", 10f, 0f, 7f, 1f);
            Ground(c, "Ground_22", 22f, 0f, 5f, 1f);
            Ground(c, "Ground_31", 31f, 0f, 3f, 1f);
            Ground(c, "Upper_12", 12f, 3.5f, 4f, 0.5f);
            Ground(c, "SwitchPedestal_A", 7.2f, 0f, 0.6f, 1.4f);
            Ground(c, "SwitchPedestal_B", 19.7f, 0f, 0.6f, 1.4f);
            DoorController2D dA = Door(c, "D5-A", new Vector2(14.5f, 1f));
            GhostSwitch2D swA = Switch(c, "SW5-A", new Vector2(7.5f, 1.5f), dA);
            Ghost(c, "T5-P1", ObservationType.Tele, true, HorizontalPath(4.5f, 10.5f, 1.8f));
            DoorController2D dB = Door(c, "D5-B", new Vector2(27.5f, 1f));
            GhostSwitch2D swB = Switch(c, "SW5-B", new Vector2(20f, 1.5f), dB);
            Bridge(c, "Bridge_B", 22f, 1f, 5f, 0.25f, swB);
            Ghost(c, "S5-P2", ObservationType.Star, true, HorizontalPath(18f, 22f, 1.8f));
            Goal(c, new Vector2(33f, 1.2f));
            return c;
        }

        private static StageContext Stage06()
        {
            StageContext c = BeginStage(6, 44f, 10f, new Vector2(2f, 1f), 1);
            Ground(c, "Ground_00", 0f, 0f, 10f, 1f);
            Ground(c, "Ground_12", 12f, 0f, 8f, 1f);
            Ground(c, "Ground_28", 28f, 0f, 8f, 1f);
            Ground(c, "Ground_38", 38f, 0f, 6f, 1f);
            Ground(c, "Upper_14", 14f, 3.5f, 4f, 0.5f);
            for (int x = 20; x <= 25; x++) Fragile(c, "Fragile_" + x, x, 0f, 1f, 1f);
            Ghost(c, "GT6", ObservationType.Tele, false, HorizontalPath(-4f, 42f, 3.5f), 0.45f, new Vector2(6f, 6f), true);
            DoorController2D dA = Door(c, "D6-A", new Vector2(19f, 1f));
            Switch(c, "SW6-A", new Vector2(17f, 3.85f), dA);
            Ghost(c, "S6-A", ObservationType.Star, false, HorizontalPath(15f, 17f, 4.2f));
            Checkpoint(c, "CP6", new Vector2(28.5f, 1f));
            Goal(c, new Vector2(43f, 1.2f));
            return c;
        }

        private static StageContext Stage07()
        {
            StageContext c = BeginStage(7, 34f, 11f, new Vector2(6f, 1f), 1);
            Ground(c, "Ground", 0f, 0f, 34f, 1f);
            Ground(c, "ControlStep", 16.5f, 1.5f, 2f, 0.5f);
            Ground(c, "ControlPerch", 17f, 3f, 3f, 0.5f);
            SightBlock(c, "SightBlock_Center", 15.5f, 1f, 0.7f, 6.5f);
            ControlMarker(c, "P7-L", new Vector2(18f, 1f));
            ControlMarker(c, "P7-U", new Vector2(18f, 3.5f));
            DoorController2D dA = Door(c, "D7-A", new Vector2(13f, 1f));
            GhostSwitch2D swA = Switch(c, "SW7-A", new Vector2(1f, 1.05f), null);
            GhostSwitch2D swB = Switch(c, "SW7-B", new Vector2(11f, 1.05f), null);
            Ghost(c, "T7-A", ObservationType.Tele, false, HorizontalPath(3f, 1f, 1.4f));
            Ghost(c, "S7-A", ObservationType.Star, false, HorizontalPath(9f, 11f, 1.4f));
            MultiGate(c, "Gate_D7-A", dA, swA, swB);
            DoorController2D dB = Door(c, "D7-B", new Vector2(30.5f, 1f));
            GhostSwitch2D swC = Switch(c, "SW7-C", new Vector2(27f, 2.45f), null);
            GhostSwitch2D swD = Switch(c, "SW7-D", new Vector2(27f, 4.65f), null);
            Ghost(c, "T7-B", ObservationType.Tele, false, HorizontalPath(25f, 27f, 2.8f));
            Ghost(c, "S7-B", ObservationType.Star, false, HorizontalPath(25f, 27f, 5f));
            MultiGate(c, "Gate_D7-B", dB, swC, swD);
            Goal(c, new Vector2(33f, 1.2f));
            return c;
        }

        private static StageContext Stage08()
        {
            StageContext c = BeginStage(8, 40f, 12f, new Vector2(1.5f, 1f), 1);
            Ground(c, "Ground_00", 0f, 0f, 5f, 1f);
            Ground(c, "Ground_10", 10f, 0f, 6f, 1f);
            Ground(c, "Ground_16", 16f, 0f, 2f, 1f);
            Ground(c, "Ground_26", 26f, 0f, 14f, 1f);
            Ground(c, "Upper_11", 11f, 4f, 7f, 0.5f);
            Ground(c, "Upper_20", 20f, 4f, 6f, 0.5f);
            DoorController2D dA = Door(c, "D8-A", new Vector2(19f, 4f));
            GhostSwitch2D swA = Switch(c, "SW8-A", new Vector2(17f, 0.95f), dA);
            Ghost(c, "T8-P", ObservationType.Tele, true, HorizontalPath(4.5f, 30.5f, 1.8f));
            Checkpoint(c, "CP8", new Vector2(26.5f, 1f));
            Goal(c, new Vector2(39f, 1.2f));
            return c;
        }

        private static StageContext Stage09()
        {
            StageContext c = BeginStage(9, 18f, 24f, new Vector2(2f, 1f), 1);
            Ground(c, "Ground", 0f, 0f, 18f, 1f);
            Ground(c, "Fixed_04", 1f, 4f, 5f, 0.5f);
            Ground(c, "Fixed_08", 12f, 8f, 5f, 0.5f);
            Ground(c, "Fixed_12", 1f, 12f, 5f, 0.5f);
            Ground(c, "Fixed_16", 12f, 16f, 5f, 0.5f);
            Ground(c, "GoalFloor", 6f, 20f, 6f, 0.5f);
            SightBlock(c, "Sight_06", 0f, 6.2f, 9f, 0.35f);
            SightBlock(c, "Sight_10", 9f, 10.2f, 9f, 0.35f);
            SightBlock(c, "Sight_14", 0f, 14.2f, 9f, 0.35f);
            SightBlock(c, "Sight_18", 9f, 18.2f, 9f, 0.35f);
            Ghost(c, "T9-A", ObservationType.Tele, true, HorizontalPath(6.5f, 10.5f, 2.4f));
            Ghost(c, "S9-B", ObservationType.Star, true, HorizontalPath(11.5f, 7.5f, 6.4f));
            Ghost(c, "T9-C", ObservationType.Tele, true, HorizontalPath(6.5f, 10.5f, 10.4f));
            Ghost(c, "S9-D", ObservationType.Star, true, HorizontalPath(11.5f, 7.5f, 14.4f));
            Ghost(c, "T9-E", ObservationType.Tele, true, HorizontalPath(6.5f, 9.5f, 18.4f));
            Checkpoint(c, "CP9-1", new Vector2(3.5f, 4.5f));
            Checkpoint(c, "CP9-2", new Vector2(14.5f, 8.5f));
            Checkpoint(c, "CP9-3", new Vector2(3.5f, 12.5f));
            Checkpoint(c, "CP9-4", new Vector2(14.5f, 16.5f));
            Goal(c, new Vector2(9f, 20.8f));
            return c;
        }

        private static StageContext Stage10()
        {
            StageContext c = BeginStage(10, 36f, 11f, new Vector2(2f, 1f), 1);
            Ground(c, "StartGround", 0f, 0f, 4f, 1f);
            Ground(c, "CheckpointIsland", 15.25f, 0f, 1.5f, 1f);
            Ground(c, "GoalGround", 32f, 0f, 4f, 1f);
            Ghost(c, "T10-A", ObservationType.Tele, true, HorizontalPath(4.5f, 9.5f, 1.8f));
            Ghost(c, "S10-B", ObservationType.Star, true, HorizontalPath(14.5f, 11.5f, 4.2f));
            Ghost(c, "T10-C", ObservationType.Tele, true, HorizontalPath(16.5f, 21.5f, 2.4f));
            Ghost(c, "S10-D", ObservationType.Star, true, HorizontalPath(27.5f, 23.5f, 4.4f));
            Checkpoint(c, "CP10", new Vector2(16f, 1f));
            Goal(c, new Vector2(35f, 1.2f));
            return c;
        }

        private static StageContext Stage11()
        {
            StageContext c = BeginStage(11, 30f, 12f, new Vector2(14f, 1f), 1);
            Ground(c, "Ground", 0f, 0f, 30f, 1f);
            Ground(c, "Upper_Left", 3f, 5f, 9f, 0.5f);
            Ground(c, "Upper_Right", 19f, 5f, 7f, 0.5f);
            SightBlock(c, "Sight_14", 14f, 1f, 0.8f, 5.5f);
            SightBlock(c, "Sight_18", 18f, 1f, 0.8f, 3.2f);
            DoorController2D door = Door(c, "D11", new Vector2(25.5f, 1f));
            GhostSwitch2D swA = Switch(c, "SW11-A", new Vector2(12f, 5.45f), null);
            GhostSwitch2D swB = Switch(c, "SW11-B", new Vector2(19f, 1.05f), null);
            Ghost(c, "T11-A", ObservationType.Tele, false, HorizontalPath(9f, 12f, 5.9f));
            Ghost(c, "S11-B", ObservationType.Star, false, HorizontalPath(22f, 19f, 1.4f));
            Ghost(c, "T11-P", ObservationType.Tele, true, HorizontalPath(20.5f, 25.5f, 2.2f));
            MultiGate(c, "Gate_D11", door, swA, swB);
            Goal(c, new Vector2(29f, 1.2f));
            return c;
        }

        private static StageContext Stage12()
        {
            StageContext c = BeginStage(12, 40f, 11f, new Vector2(2f, 1f), 1);
            Ground(c, "Ground_00", 0f, 0f, 8f, 1f);
            Ground(c, "Ground_10", 10f, 0f, 8f, 1f);
            Ground(c, "Ground_22", 22f, 0f, 6f, 1f);
            Ground(c, "Ground_32", 32f, 0f, 8f, 1f);
            Ground(c, "Upper_12", 12f, 4f, 5f, 0.5f);
            for (int x = 28; x <= 31; x++) Fragile(c, "Fragile_" + x, x, 1f, 1f, 0.4f);
            Ghost(c, "GT12", ObservationType.Tele, false, HorizontalPath(-4f, 38f, 3.5f), 0.45f, new Vector2(6f, 6f), true);
            DoorController2D dA = Door(c, "D12-A", new Vector2(17.5f, 1f));
            Switch(c, "SW12-A", new Vector2(15f, 4.45f), dA);
            Ghost(c, "S12-A", ObservationType.Star, false, HorizontalPath(13f, 15f, 4.8f));
            Ghost(c, "T12-P", ObservationType.Tele, true, HorizontalPath(21.5f, 26.5f, 2f));
            DoorController2D dB = Door(c, "D12-B", new Vector2(37f, 1f));
            Switch(c, "SW12-B", new Vector2(35f, 1.05f), dB);
            Ghost(c, "T12-B", ObservationType.Tele, false, HorizontalPath(33f, 35f, 1.4f));
            Goal(c, new Vector2(39f, 1.2f));
            return c;
        }

        private static StageContext Stage13()
        {
            StageContext c = BeginStage(13, 46f, 11f, new Vector2(2f, 1f), 1);
            Ground(c, "StartGround", 0f, 0f, 4f, 1f);
            Ground(c, "GoalGround", 42f, 0f, 4f, 1f);
            Ghost(c, "S13-A", ObservationType.Star, true, HorizontalPath(4.5f, 8.5f, 2f));
            Ghost(c, "S13-B", ObservationType.Star, true, HorizontalPath(11.5f, 15.5f, 4f));
            Ghost(c, "S13-C", ObservationType.Star, true, HorizontalPath(18.5f, 22.5f, 2f));
            Ghost(c, "S13-D", ObservationType.Star, true, HorizontalPath(25.5f, 29.5f, 4f));
            Ghost(c, "S13-E", ObservationType.Star, true, HorizontalPath(32.5f, 36.5f, 2f));
            Ghost(c, "S13-F", ObservationType.Star, true, HorizontalPath(39.5f, 41.5f, 4f));
            Goal(c, new Vector2(45f, 1.2f));
            return c;
        }

        private static StageContext Stage14()
        {
            StageContext c = BeginStage(14, 26f, 12f, new Vector2(2f, 1f), 1);
            Ground(c, "Ground", 0f, 0f, 26f, 1f);
            Ground(c, "ControlStep", 3f, 1.4f, 2.5f, 0.5f);
            Ground(c, "ControlDeck", 5f, 2.5f, 7f, 0.5f);
            Ground(c, "LowerShelf", 17.5f, 1.4f, 4f, 0.4f);
            Ground(c, "UpperShelf", 17.5f, 5.2f, 4f, 0.4f);
            SightBlock(c, "SightBlock_Lock", 9.8f, 4.2f, 0.4f, 3.4f);
            ControlMarker(c, "P14-Lock", new Vector2(7f, 3f));
            ControlMarker(c, "P14-Star", new Vector2(11f, 3f));
            DoorController2D door = Door(c, "D14", new Vector2(23.5f, 1f));
            GhostSwitch2D swA = Switch(c, "SW14-A", new Vector2(20.5f, 1.85f), null);
            GhostSwitch2D swB = Switch(c, "SW14-B", new Vector2(20.5f, 5.65f), null);
            Ghost(c, "T14-A", ObservationType.Tele, false, HorizontalPath(18.5f, 20.5f, 2.2f), 1.2f);
            Ghost(c, "S14-B", ObservationType.Star, false, HorizontalPath(18.5f, 20.5f, 6f), 1.2f);
            MultiGate(c, "Gate_D14", door, swA, swB);
            Goal(c, new Vector2(25f, 1.2f));
            return c;
        }

        private static StageContext Stage15()
        {
            StageContext c = BeginStage(15, 94f, 16f, new Vector2(2f, 1f), 1);
            Ground(c, "Ground_A", 0f, 0f, 24f, 1f);
            Ground(c, "StepA1", 1.5f, 1.6f, 3f, 0.5f);
            Ground(c, "StepA2", 4.5f, 2.8f, 3.5f, 0.5f);
            Ground(c, "LedgeA", 2f, 4f, 6f, 0.5f);
            Ground(c, "LedgeB", 10f, 4f, 5f, 0.5f);
            Ground(c, "StepA3", 13.5f, 5.4f, 4f, 0.5f);
            Ground(c, "UpperDeck", 16f, 7f, 6f, 0.5f);
            Ground(c, "Ground_24", 24f, 0f, 4f, 1f);
            Ground(c, "Ground_52", 52f, 0f, 6f, 1f);
            Ground(c, "Ground_58", 58f, 0f, 8f, 1f);
            Ground(c, "Ground_68", 68f, 0f, 8f, 1f);
            Ground(c, "Ground_80", 80f, 0f, 7f, 1f);
            Ground(c, "GoalGround", 92f, 0f, 2f, 1f);
            SightBlock(c, "Sight_A1", 8.5f, 1f, 0.6f, 6f);
            SightBlock(c, "Sight_A2", 15f, 1f, 0.6f, 7f);
            SightBlock(c, "Sight_A3", 20.5f, 1f, 0.6f, 5f);

            DoorController2D dA = Door(c, "D15-A", new Vector2(23f, 1f));
            GhostSwitch2D swA = Switch(c, "SW15-A", new Vector2(9f, 1.05f), null);
            GhostSwitch2D swB = Switch(c, "SW15-B", new Vector2(15f, 4.55f), null);
            Ghost(c, "T15-A", ObservationType.Tele, false, HorizontalPath(6f, 9f, 1.4f));
            Ghost(c, "S15-B", ObservationType.Star, false, HorizontalPath(12f, 15f, 4.9f));
            Ghost(c, "T15-P", ObservationType.Tele, true, HorizontalPath(17.5f, 21.5f, 7.8f));
            MultiGate(c, "Gate_D15-A", dA, swA, swB);
            Checkpoint(c, "CP15-A", new Vector2(25f, 1f));

            Ghost(c, "T15-C", ObservationType.Tele, true, HorizontalPath(28.5f, 33.5f, 1.8f));
            Ghost(c, "S15-D", ObservationType.Star, true, HorizontalPath(38.5f, 35.5f, 4.5f));
            Ghost(c, "T15-E", ObservationType.Tele, true, HorizontalPath(41.5f, 45.5f, 2.2f));
            Ghost(c, "S15-F", ObservationType.Star, true, HorizontalPath(50.5f, 47.5f, 4.7f));

            GhostPulseController2D giant = Ghost(
                c,
                "GT15",
                ObservationType.Tele,
                false,
                HorizontalPath(54f, 91f, 4f),
                0.45f,
                new Vector2(6f, 7f),
                true);
            giant.gameObject.SetActive(false);
            StageCheckpoint2D cpB = Checkpoint(c, "CP15-B", new Vector2(54f, 1f));
            SetObjectArray(cpB, "activateTargets", new Object[] { giant.gameObject });

            DoorController2D dC = Door(c, "D15-C", new Vector2(65.5f, 1f));
            Switch(c, "SW15-C", new Vector2(64f, 4.65f), dC);
            Ghost(c, "S15-G", ObservationType.Star, false, HorizontalPath(62f, 64f, 5f));
            Ghost(c, "T15-H", ObservationType.Tele, true, HorizontalPath(67.5f, 73.5f, 2f));
            for (int x = 76; x <= 79; x++) Fragile(c, "Fragile_" + x, x, 1f, 1f, 0.4f);
            DoorController2D dD = Door(c, "D15-D", new Vector2(86f, 1f));
            Switch(c, "SW15-D", new Vector2(84f, 1.05f), dD);
            Ghost(c, "T15-I", ObservationType.Tele, false, HorizontalPath(82f, 84f, 1.4f));
            Ghost(c, "T15-FINAL", ObservationType.Tele, true, HorizontalPath(87.5f, 90.5f, 2f));
            Goal(c, new Vector2(93f, 1.2f));
            return c;
        }

        private static StageContext BeginStage(
            int stage,
            float width,
            float height,
            Vector2 spawnFoot,
            int initialHorizontalSign)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = StageSceneName(stage);
            StageContext c = new StageContext
            {
                Scene = scene,
                Width = width,
                Height = height,
                Environment = new GameObject("Environment").transform,
                Ghosts = new GameObject("Ghosts").transform,
                Gimmicks = new GameObject("Gimmicks").transform,
                Labels = new GameObject("StageLabels").transform
            };

            GameObject systemRoot = new GameObject("_SYSTEM");
            c.SpawnPoint = CreateChild(systemRoot.transform, "SpawnPoint", spawnFoot);
            GameObject playerObject = InstantiatePrefab(
                playerPrefab,
                null,
                new Vector3(spawnFoot.x, spawnFoot.y + 0.8f, 0f),
                "Player");
            c.Player = playerObject.GetComponent<PlayerController2D>();
            SetInt(c.Player, "initialViewHorizontalSign", initialHorizontalSign);
            SetBool(c.Player, "initialLookingUp", false);
            c.Flow = systemRoot.AddComponent<GameStageFlow2D>();
            SetObject(c.Flow, "player", c.Player);
            SetObject(c.Flow, "respawnPoint", c.SpawnPoint);
            SetFloat(c.Flow, "playerFootOffset", 0.8f);
            CreateCamera(c.Player, width, height);
            CreateGlobalLight();
            CreateText(
                c.Labels,
                "StageTitle",
                "STAGE " + stage.ToString("00") + "  " + StageTitle(stage),
                new Vector3(spawnFoot.x - 0.5f, 5.2f, 0f),
                0.16f,
                Color.white,
                TextAnchor.MiddleLeft);
            CreateText(
                c.Labels,
                "Controls",
                "A/D MOVE   SPACE JUMP   S FLIP VIEW   HOLD W LOOK UP   R RESET",
                new Vector3(spawnFoot.x - 0.5f, 4.65f, 0f),
                0.075f,
                new Color(0.5f, 0.52f, 0.58f, 1f),
                TextAnchor.MiddleLeft);
            return c;
        }

        private static void FinishStage(StageContext c, int stage)
        {
            DeathZone(c);
            SetObjectArray(c.Flow, "ghostPulses", c.Pulses.ToArray());
            SetObjectArray(c.Flow, "ghostSwitches", c.Switches.ToArray());
            SetObjectArray(c.Flow, "doors", c.Doors.ToArray());
            SetObjectArray(c.Flow, "bridges", c.Bridges.ToArray());
            SetObjectArray(c.Flow, "fragileFloors", c.FragileFloors.ToArray());
            SetString(c.Flow, "nextSceneName", stage == 15 ? "StageSelect" : StageSceneName(stage + 1));
            EditorSceneManager.SaveScene(c.Scene, StageSceneRoot + "/" + StageSceneName(stage) + ".unity");
        }

        private static void Ground(StageContext c, string name, float x, float y, float width, float height)
        {
            RectObject(blockPrefab, c.Environment, name, x, y, width, height, 0);
        }

        private static void SightBlock(StageContext c, string name, float x, float y, float width, float height)
        {
            GameObject block = RectObject(blockPrefab, c.Environment, name, x, y, width, height, SightBlockLayer);
            BoxCollider2D collider = block.GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = new Color(0.12f, 0.13f, 0.16f, 0.85f);
            }
        }

        private static void ControlMarker(StageContext c, string id, Vector2 footPosition)
        {
            Transform root = CreateChild(c.Labels, id, footPosition + Vector2.up * 0.04f);
            SpriteRenderer line = CreateSpriteChild(
                root,
                "Marker",
                new Color(0.78f, 0.81f, 0.88f, 0.9f),
                7);
            line.transform.localScale = new Vector3(0.7f, 0.08f, 1f);
            CreateText(
                root,
                "Label",
                id,
                new Vector3(0f, 0.2f, 0f),
                0.07f,
                new Color(0.78f, 0.81f, 0.88f, 1f),
                TextAnchor.LowerCenter);
        }

        private static FragileFloor2D Fragile(
            StageContext c,
            string name,
            float x,
            float y,
            float width,
            float height)
        {
            GameObject root = RectObject(fragileFloorPrefab, c.Environment, name, x, y, width, height, FragileFloorLayer);
            FragileFloor2D floor = root.GetComponent<FragileFloor2D>();
            if (floor != null)
            {
                c.FragileFloors.Add(floor);
            }
            return floor;
        }

        private static GhostPulseController2D Ghost(
            StageContext c,
            string id,
            ObservationType type,
            bool rideable,
            Vector2[] nodes,
            float pulseInterval = 0.5f,
            Vector2? giantSize = null,
            bool giant = false)
        {
            GameObject prefab;
            if (type == ObservationType.Tele)
            {
                prefab = rideable ? teleRidePrefab : teleUtilityPrefab;
            }
            else
            {
                prefab = rideable ? starRidePrefab : starUtilityPrefab;
            }

            GameObject root = InstantiatePrefab(prefab, c.Ghosts, nodes[0], id);
            GhostMoveAction2D move = root.GetComponent<GhostMoveAction2D>();
            GhostPulseController2D pulse = root.GetComponent<GhostPulseController2D>();
            SetVector2Array(move, "pathNodes", nodes);
            SetEnum(move, "routeMode", (int)GhostMoveRouteMode.PingPong);
            SetFloat(move, "moveDurationPerUnit", 0.15f);
            SetFloat(pulse, "pulseInterval", pulseInterval);
            if (giant)
            {
                SetInt(move, "collisionMask", 0);
            }
            if (giantSize.HasValue)
            {
                Vector2 baseSize = new Vector2(0.8f, 0.8f);
                root.transform.localScale = new Vector3(
                    giantSize.Value.x / baseSize.x,
                    giantSize.Value.y / baseSize.y,
                    1f);
            }
            if (giant)
            {
                root.AddComponent<GiantTele2D>();
            }
            c.Pulses.Add(pulse);
            return pulse;
        }

        private static GhostSwitch2D Switch(
            StageContext c,
            string id,
            Vector2 position,
            DoorController2D controlledDoor)
        {
            GameObject root = InstantiatePrefab(switchPrefab, c.Gimmicks, position, id);
            GhostSwitch2D target = root.GetComponent<GhostSwitch2D>();
            SetObject(target, "controlledDoor", controlledDoor);
            c.Switches.Add(target);
            return target;
        }

        private static DoorController2D Door(StageContext c, string id, Vector2 position)
        {
            GameObject root = InstantiatePrefab(doorPrefab, c.Gimmicks, position, id);
            DoorController2D door = root.GetComponent<DoorController2D>();
            c.Doors.Add(door);
            return door;
        }

        private static SpawnBridge2D Bridge(
            StageContext c,
            string id,
            float x,
            float y,
            float width,
            float height,
            GhostSwitch2D source)
        {
            GameObject root = RectObject(bridgePrefab, c.Gimmicks, id, x, y, width, height, 0);
            SpawnBridge2D bridge = root.GetComponent<SpawnBridge2D>();
            SetObject(bridge, "sourceSwitch", source);
            bridge.SetActive(false);
            c.Bridges.Add(bridge);
            return bridge;
        }

        private static StageCheckpoint2D Checkpoint(StageContext c, string id, Vector2 footPosition)
        {
            GameObject root = InstantiatePrefab(checkpointPrefab, c.Gimmicks, footPosition, id);
            StageCheckpoint2D checkpoint = root.GetComponent<StageCheckpoint2D>();
            SetObject(checkpoint, "stageFlow", c.Flow);
            return checkpoint;
        }

        private static GameObject Goal(StageContext c, Vector2 position)
        {
            GameObject root = new GameObject("Goal");
            root.transform.SetParent(c.Gimmicks, false);
            root.transform.position = position;
            SpriteRenderer renderer = CreateSpriteChild(root.transform, "Body", Color.white, 12);
            renderer.transform.localScale = new Vector3(1.2f, 2.4f, 1f);
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.2f, 2.4f);
            collider.isTrigger = true;
            GameStageTrigger2D trigger = root.AddComponent<GameStageTrigger2D>();
            SetObject(trigger, "stageFlow", c.Flow);
            SetEnum(trigger, "triggerType", (int)GameStageTriggerType.Complete);
            CreateText(root.transform, "GoalLabel", "GOAL", Vector3.zero, 0.18f, Color.black, TextAnchor.MiddleCenter);
            return root;
        }

        private static void LockGoal(GameObject goal, GhostSwitch2D source)
        {
            if (goal == null)
            {
                return;
            }
            SwitchLockedGoal2D gate = goal.AddComponent<SwitchLockedGoal2D>();
            SetObject(gate, "sourceSwitch", source);
            SetObject(gate, "goalCollider", goal.GetComponent<Collider2D>());
            SetObject(gate, "goalRenderer", goal.GetComponentInChildren<Renderer>());
        }

        private static void MultiGate(
            StageContext c,
            string name,
            DoorController2D door,
            params GhostSwitch2D[] switches)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(c.Gimmicks, false);
            MultiSwitchGate2D gate = root.AddComponent<MultiSwitchGate2D>();
            SetObject(gate, "controlledDoor", door);
            SetObjectArray(gate, "requiredSwitches", switches);
        }

        private static void DeathZone(StageContext c)
        {
            GameObject root = new GameObject("DeathZone");
            root.transform.SetParent(c.Gimmicks, false);
            root.transform.position = new Vector3(c.Width * 0.5f, -6f, 0f);
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(c.Width + 20f, 4f);
            collider.isTrigger = true;
            GameStageTrigger2D trigger = root.AddComponent<GameStageTrigger2D>();
            SetObject(trigger, "stageFlow", c.Flow);
            SetEnum(trigger, "triggerType", (int)GameStageTriggerType.Respawn);
        }

        private static GameObject RectObject(
            GameObject prefab,
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            int layer)
        {
            GameObject root = InstantiatePrefab(
                prefab,
                parent,
                new Vector3(x + width * 0.5f, y + height * 0.5f, 0f),
                name);
            root.transform.localScale = new Vector3(width, height, 1f);
            root.layer = layer;
            return root;
        }

        private static Vector2[] HorizontalPath(float startX, float endX, float y)
        {
            int steps = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(endX - startX)));
            Vector2[] nodes = new Vector2[steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                nodes[i] = new Vector2(Mathf.Lerp(startX, endX, i / (float)steps), y);
            }
            return nodes;
        }

        private static Vector2[] NodePath(params float[] coordinates)
        {
            int count = coordinates.Length / 2;
            Vector2[] nodes = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                nodes[i] = new Vector2(coordinates[i * 2], coordinates[i * 2 + 1]);
            }
            return nodes;
        }

        private static void CreateCamera(PlayerController2D player, float width, float height)
        {
            GameObject cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            cameraObject.transform.position = new Vector3(5f, 5f, -10f);
            UniversalAdditionalCameraData data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderType = CameraRenderType.Base;
            CameraFollow2D follow = cameraObject.AddComponent<CameraFollow2D>();
            SetObject(follow, "target", player.transform);
            SetObject(follow, "viewSource", player);
            SetFloat(follow, "minimumX", 5f);
            SetFloat(follow, "maximumX", Mathf.Max(5f, width - 5f));
            SetFloat(follow, "minimumY", 5f);
            SetFloat(follow, "maximumY", Mathf.Max(5f, height - 5f));
            SetFloat(follow, "fixedY", 5f);
            SetBool(follow, "followTargetY", height > 11f);
            SetVector2(follow, "viewOffset", new Vector2(1.5f, 0.8f));
        }

        private static void CreateGlobalLight()
        {
            GameObject lightObject = new GameObject("Global Light 2D");
            Light2D light = lightObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 0.18f;
            light.color = new Color(0.62f, 0.65f, 0.72f, 1f);
        }

        private static void BuildStageSelect()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform environment = new GameObject("Environment").transform;
            Transform portals = new GameObject("StagePortals").transform;
            float width = 42f;
            RectObject(blockPrefab, environment, "Ground", 0f, 0f, width, 1f, 0);
            GameObject player = InstantiatePrefab(playerPrefab, null, new Vector3(1.5f, 1.8f, 0f), "Player");
            CreateCamera(player.GetComponent<PlayerController2D>(), width, 10f);
            CreateGlobalLight();
            CreateText(null, "Title", "TELEGHOST  STAGE SELECT", new Vector3(1f, 5.2f, 0f), 0.2f, Color.white, TextAnchor.MiddleLeft);
            for (int stage = 1; stage <= 15; stage++)
            {
                float x = 2.5f + stage * 2.45f;
                GameObject portal = new GameObject("Portal_" + stage.ToString("00"));
                portal.transform.SetParent(portals, false);
                portal.transform.position = new Vector3(x, 2.2f, 0f);
                SpriteRenderer renderer = portal.AddComponent<SpriteRenderer>();
                renderer.sprite = squareSprite;
                renderer.color = stage % 2 == 0
                    ? new Color(0.48f, 0.5f, 0.56f, 1f)
                    : new Color(0.33f, 0.35f, 0.4f, 1f);
                portal.transform.localScale = new Vector3(1.1f, 2.4f, 1f);
                BoxCollider2D collider = portal.AddComponent<BoxCollider2D>();
                collider.isTrigger = true;
                StagePortal2D stagePortal = portal.AddComponent<StagePortal2D>();
                SetString(stagePortal, "sceneName", StageSceneName(stage));
                CreateText(portal.transform, "Number", stage.ToString("00"), Vector3.zero, 0.18f, Color.white, TextAnchor.MiddleCenter);
            }
            EditorSceneManager.SaveScene(scene, SystemSceneRoot + "/StageSelect.unity");
        }

        private static void ConfigureBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(SystemSceneRoot + "/StageSelect.unity", true)
            };
            for (int stage = 1; stage <= 15; stage++)
            {
                scenes.Add(new EditorBuildSettingsScene(
                    StageSceneRoot + "/" + StageSceneName(stage) + ".unity",
                    true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject InstantiatePrefab(
            GameObject prefab,
            Transform parent,
            Vector3 position,
            string name)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
            {
                return null;
            }
            instance.name = name;
            instance.transform.position = position;
            return instance;
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            return child.transform;
        }

        private static SpriteRenderer CreateSpriteChild(
            Transform parent,
            string name,
            Color color,
            int sortingOrder)
        {
            Transform child = CreateChild(parent, name, Vector3.zero);
            SpriteRenderer renderer = child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static TextMesh CreateText(
            Transform parent,
            string name,
            string text,
            Vector3 position,
            float characterSize,
            Color color,
            TextAnchor anchor)
        {
            GameObject root = new GameObject(name);
            if (parent != null)
            {
                root.transform.SetParent(parent, false);
                root.transform.localPosition = position;
            }
            else
            {
                root.transform.position = position;
            }
            TextMesh mesh = root.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.characterSize = characterSize;
            mesh.fontSize = 64;
            if (gameFont != null)
            {
                mesh.font = gameFont;
            }
            mesh.color = color;
            mesh.anchor = anchor;
            mesh.alignment = anchor == TextAnchor.MiddleCenter ? TextAlignment.Center : TextAlignment.Left;
            MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (gameFont != null)
                {
                    renderer.sharedMaterial = gameFont.material;
                }
                renderer.sortingOrder = 30;
            }
            return mesh;
        }

        private static string StageSceneName(int stage)
        {
            string[] slugs =
            {
                "", "TELE", "STAR", "DualBasics", "Rideable", "RideAndSwitch",
                "GiantTELE", "DualControl", "Reuse", "Vertical", "RealtimeView",
                "MultiSwitch", "IntenseMix", "STARChain", "DensePuzzle", "Finale"
            };
            return "Stage" + stage.ToString("00") + "_" + slugs[stage];
        }

        private static string StageTitle(int stage)
        {
            string[] titles =
            {
                "", "見送る幽霊", "見つめる幽霊", "視線の使い分け", "幽霊に乗って",
                "運ぶ足場", "巨大な影", "二つの視線",
                "一体を使い切る", "見上げる塔", "視線をつなぐ",
                "同時に動かす", "崩れる足元", "見つめ続ける道",
                "タイミングを合わせて", "視線の果てへ"
            };
            return titles[stage];
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            if (target == null)
            {
                return;
            }
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetObjectArray(Object target, string propertyName, Object[] values)
        {
            if (target == null)
            {
                return;
            }
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }
            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetVector2Array(Object target, string propertyName, Vector2[] values)
        {
            if (target == null)
            {
                return;
            }
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }
            property.arraySize = values != null ? values.Length : 0;
            for (int i = 0; i < property.arraySize; i++)
            {
                property.GetArrayElementAtIndex(i).vector2Value = values[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SetSerializedValue(target, propertyName, property => property.floatValue = value);
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            SetSerializedValue(target, propertyName, property => property.intValue = value);
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SetSerializedValue(target, propertyName, property => property.boolValue = value);
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            SetSerializedValue(target, propertyName, property => property.enumValueIndex = value);
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SetSerializedValue(target, propertyName, property => property.stringValue = value);
        }

        private static void SetVector2(Object target, string propertyName, Vector2 value)
        {
            SetSerializedValue(target, propertyName, property => property.vector2Value = value);
        }

        private static void SetSerializedValue(
            Object target,
            string propertyName,
            System.Action<SerializedProperty> apply)
        {
            if (target == null)
            {
                return;
            }
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
            {
                apply(property);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
