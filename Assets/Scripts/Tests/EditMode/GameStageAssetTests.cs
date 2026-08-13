using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TelegGhost.Runtime;
using TelegGhost.Runtime.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TelegGhost.Tests
{
    public sealed class GameStageAssetTests
    {
        private static readonly string[] StagePaths =
        {
            "Assets/Scene/Game/Stages/Stage01_TELE.unity",
            "Assets/Scene/Game/Stages/Stage02_STAR.unity",
            "Assets/Scene/Game/Stages/Stage03_DualBasics.unity",
            "Assets/Scene/Game/Stages/Stage04_Rideable.unity",
            "Assets/Scene/Game/Stages/Stage05_RideAndSwitch.unity",
            "Assets/Scene/Game/Stages/Stage06_GiantTELE.unity",
            "Assets/Scene/Game/Stages/Stage07_DualControl.unity",
            "Assets/Scene/Game/Stages/Stage08_Reuse.unity",
            "Assets/Scene/Game/Stages/Stage09_Vertical.unity",
            "Assets/Scene/Game/Stages/Stage10_RealtimeView.unity",
            "Assets/Scene/Game/Stages/Stage11_MultiSwitch.unity",
            "Assets/Scene/Game/Stages/Stage12_IntenseMix.unity",
            "Assets/Scene/Game/Stages/Stage13_STARChain.unity",
            "Assets/Scene/Game/Stages/Stage14_DensePuzzle.unity",
            "Assets/Scene/Game/Stages/Stage15_Finale.unity"
        };

        private static readonly int[] ExpectedGhostCounts =
        {
            3, 3, 4, 3, 2, 2, 4, 1, 5, 4, 3, 4, 6, 2, 12
        };

        [Test]
        public void BuildSettingsContainOnlyGameScenesInOrder()
        {
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            Assert.That(buildScenes.Length, Is.EqualTo(16));
            Assert.That(buildScenes[0].path, Is.EqualTo("Assets/Scene/Game/System/StageSelect.unity"));

            for (int i = 0; i < StagePaths.Length; i++)
            {
                Assert.That(buildScenes[i + 1].path, Is.EqualTo(StagePaths[i]));
                Assert.That(buildScenes[i + 1].enabled, Is.True);
            }
        }

        [Test]
        public void StageAssetsDoNotDependOnMockupContent()
        {
            for (int i = 0; i < StagePaths.Length; i++)
            {
                string[] dependencies = AssetDatabase.GetDependencies(StagePaths[i], true);
                Assert.That(
                    dependencies.Any(path => path.Contains("/Scene/Mockup/") || path.Contains("/Prefabs/Ghosts/")),
                    Is.False,
                    StagePaths[i]);
            }
        }

        [Test]
        public void SquareGhostPrefabsAndPulseAudioExist()
        {
            string[] ghostPaths =
            {
                "Assets/Resource/Prefabs/Game/Ghost/TELE_Square_Rideable.prefab",
                "Assets/Resource/Prefabs/Game/Ghost/TELE_Square_Utility.prefab",
                "Assets/Resource/Prefabs/Game/Ghost/STAR_Square_Rideable.prefab",
                "Assets/Resource/Prefabs/Game/Ghost/STAR_Square_Utility.prefab"
            };

            for (int i = 0; i < ghostPaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ghostPaths[i]);
                Assert.That(prefab, Is.Not.Null, ghostPaths[i]);
                Assert.That(prefab.GetComponent<GhostPulseController2D>(), Is.Not.Null);
                Assert.That(prefab.GetComponent<GhostMoveAction2D>(), Is.Not.Null);
                Assert.That(prefab.GetComponent<AudioSource>(), Is.Not.Null);
                SerializedObject pulse = new SerializedObject(prefab.GetComponent<GhostPulseController2D>());
                Assert.That(pulse.FindProperty("visibilityRenderer").objectReferenceValue, Is.Not.Null);
                Assert.That(pulse.FindProperty("pulseAudioSource").objectReferenceValue, Is.Not.Null);
                Assert.That(pulse.FindProperty("pulseClip").objectReferenceValue, Is.Not.Null);
                Assert.That(prefab.transform.Find("Visuals/Body"), Is.Not.Null);
            }

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Resources/Short_Accent07-1(Dry).mp3");
            Assert.That(clip, Is.Not.Null);
        }

        [Test]
        public void EveryStageHasPlayableCoreAndExpectedGhostCount()
        {
            SceneSetup[] originalSetup = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for (int i = 0; i < StagePaths.Length; i++)
                {
                    Scene scene = EditorSceneManager.OpenScene(StagePaths[i], OpenSceneMode.Single);
                    List<GameObject> allObjects = GetAllSceneObjects(scene);
                    Assert.That(allObjects.Count(go => go.GetComponent<PlayerController2D>() != null), Is.EqualTo(1), StagePaths[i]);
                    Assert.That(allObjects.Count(go => go.GetComponent<GameStageFlow2D>() != null), Is.EqualTo(1), StagePaths[i]);
                    Assert.That(allObjects.Count(go => go.GetComponent<Camera>() != null), Is.EqualTo(1), StagePaths[i]);
                    Assert.That(allObjects.Count(go => go.GetComponent<AudioListener>() != null), Is.EqualTo(1), StagePaths[i]);
                    Assert.That(allObjects.Count(go => go.name == "Goal"), Is.EqualTo(1), StagePaths[i]);
                    Assert.That(
                        allObjects.Count(go => go.GetComponent<GhostPulseController2D>() != null),
                        Is.EqualTo(ExpectedGhostCounts[i]),
                        StagePaths[i]);
                    Assert.That(
                        allObjects.Count(go => go.GetComponent<StageGhostActivation2D>() != null),
                        Is.Zero,
                        StagePaths[i]);

                    for (int objectIndex = 0; objectIndex < allObjects.Count; objectIndex++)
                    {
                        Component[] components = allObjects[objectIndex].GetComponents<Component>();
                        Assert.That(components.Any(component => component == null), Is.False, allObjects[objectIndex].name);
                    }
                }
            }
            finally
            {
                if (originalSetup.Any(setup => setup.isLoaded))
                {
                    EditorSceneManager.RestoreSceneManagerSetup(originalSetup);
                }
                else
                {
                    EditorSceneManager.OpenScene(
                        "Assets/Scene/Game/System/StageSelect.unity",
                        OpenSceneMode.Single);
                }
            }
        }

        private static List<GameObject> GetAllSceneObjects(Scene scene)
        {
            List<GameObject> objects = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int transformIndex = 0; transformIndex < transforms.Length; transformIndex++)
                {
                    objects.Add(transforms[transformIndex].gameObject);
                }
            }
            return objects;
        }
    }
}
