using System.Collections.Generic;
using NUnit.Framework;
using TelegGhost.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TelegGhost.Tests
{
    public sealed class GhostPulseRuleTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(createdObjects[i]);
            }
            createdObjects.Clear();
        }

        [TestCase(ObservationType.Tele, false, true)]
        [TestCase(ObservationType.Tele, true, false)]
        [TestCase(ObservationType.Star, false, false)]
        [TestCase(ObservationType.Star, true, true)]
        public void ObservationTypeHasExpectedActiveRule(
            ObservationType type,
            bool observed,
            bool expected)
        {
            Assert.That(GhostController2D.ShouldBeActive(type, observed), Is.EqualTo(expected));
        }

        [Test]
        public void PulseProgressStopsAndResumesWithoutReset()
        {
            float progress = GhostPulseController2D.CalculateNextProgress(0.4f, false, 0.2f, 0.5f);
            Assert.That(progress, Is.EqualTo(0.4f).Within(0.0001f));

            progress = GhostPulseController2D.CalculateNextProgress(progress, true, 0.2f, 0.5f);
            Assert.That(progress, Is.EqualTo(0.8f).Within(0.0001f));
        }

        [Test]
        public void PingPongRouteReversesOnlyAtEndpoints()
        {
            int gridIndex = 0;
            int travelSign = 1;

            CompleteRouteSteps(ref gridIndex, ref travelSign, 3, 2);
            Assert.That(gridIndex, Is.EqualTo(2));
            Assert.That(travelSign, Is.EqualTo(1));

            CompleteRouteSteps(ref gridIndex, ref travelSign, 3, 1);
            Assert.That(gridIndex, Is.EqualTo(3));
            Assert.That(travelSign, Is.EqualTo(-1));

            CompleteRouteSteps(ref gridIndex, ref travelSign, 3, 3);
            Assert.That(gridIndex, Is.Zero);
            Assert.That(travelSign, Is.EqualTo(1));
        }

        [Test]
        public void RouteEndpointReversalRequiresAnotherPulseToMoveBack()
        {
            int gridIndex = 2;
            int travelSign = 1;

            CompleteRouteSteps(ref gridIndex, ref travelSign, 3, 1);
            Assert.That(gridIndex, Is.EqualTo(3));
            Assert.That(travelSign, Is.EqualTo(-1));

            CompleteRouteSteps(ref gridIndex, ref travelSign, 3, 1);
            Assert.That(gridIndex, Is.EqualTo(2));
        }

        [TestCase("STAR_Platform_Pulse")]
        [TestCase("STAR_Wisp_Pulse")]
        [TestCase("TELE_Platform_Pulse")]
        [TestCase("TELE_Wisp_Pulse")]
        public void MovingGhostPrefabsUsePointSevenFiveSecondPulse(string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Resource/Prefabs/Ghosts/{prefabName}.prefab");
            Assert.That(prefab, Is.Not.Null);

            GhostPulseController2D pulse = prefab.GetComponent<GhostPulseController2D>();
            Assert.That(pulse, Is.Not.Null);
            Assert.That(pulse.PulseInterval, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void ObserverUsesCenterPointAndSolidObstruction()
        {
            Vector2 origin = new Vector2(0f, 50f);
            GhostObserver2D observer = CreateObserver(origin, Vector2.right);
            Assert.That(observer.CanObserve(origin), Is.True);
            Assert.That(observer.CanObserve(new Vector2(4f, 50f)), Is.True);

            GameObject wall = new GameObject("Pulse_Wall_Test");
            createdObjects.Add(wall);
            wall.transform.position = new Vector3(2f, 50f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 2f);
            Physics2D.SyncTransforms();

            Assert.That(observer.CanObserve(new Vector2(4f, 50f)), Is.False);
        }

        [Test]
        public void AnyObserverCanMarkPointObserved()
        {
            GhostObserver2D away = CreateObserver(new Vector2(0f, 50f), Vector2.left);
            GhostObserver2D toward = CreateObserver(new Vector2(0f, 51f), Vector2.right);
            var observers = new List<GhostObserver2D> { away, toward };

            Assert.That(
                GhostObservationDetector2D.IsObservedByAny(new Vector2(4f, 51f), observers),
                Is.True);

            toward.enabled = false;
            Assert.That(
                GhostObservationDetector2D.IsObservedByAny(new Vector2(4f, 51f), observers),
                Is.False);
        }

        [TestCase("Move", "<Keyboard>/a")]
        [TestCase("Move", "<Keyboard>/d")]
        [TestCase("Jump", "<Keyboard>/space")]
        [TestCase("Reset", "<Keyboard>/r")]
        [TestCase("ToggleView", "<Keyboard>/s")]
        [TestCase("LookUp", "<Keyboard>/w")]
        public void InputAssetContainsRequiredKeyboardBinding(string actionName, string bindingPath)
        {
            InputActionAsset inputAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Data/Input/TELEGHOST.inputactions");
            Assert.That(inputAsset, Is.Not.Null);

            InputActionMap gameplay = inputAsset.FindActionMap("Gameplay", false);
            Assert.That(gameplay, Is.Not.Null);
            InputAction action = gameplay.FindAction(actionName, false);
            Assert.That(action, Is.Not.Null);

            bool found = false;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (string.Equals(
                    action.bindings[i].path,
                    bindingPath,
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                    break;
                }
            }

            Assert.That(found, Is.True, $"Missing binding: {actionName} -> {bindingPath}");
        }

        private GhostObserver2D CreateObserver(Vector2 position, Vector2 direction)
        {
            GameObject observerObject = new GameObject("Pulse_Observer_Test") { layer = 8 };
            createdObjects.Add(observerObject);
            observerObject.SetActive(false);
            observerObject.transform.position = position;
            GhostObserver2D observer = observerObject.AddComponent<GhostObserver2D>();

            SerializedObject serialized = new SerializedObject(observer);
            serialized.FindProperty("visionOrigin").objectReferenceValue = observerObject.transform;
            serialized.FindProperty("fixedDirection").vector2Value = direction;
            serialized.FindProperty("fieldOfView").floatValue = 60f;
            serialized.FindProperty("viewDistance").floatValue = 10f;
            serialized.FindProperty("obstructionMask").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            observerObject.SetActive(true);
            Physics2D.SyncTransforms();
            return observer;
        }

        private static void CompleteRouteSteps(
            ref int gridIndex,
            ref int travelSign,
            int gridCount,
            int count)
        {
            for (int i = 0; i < count; i++)
            {
                GhostMoveAction2D.CalculateNextPingPongStep(
                    gridIndex,
                    travelSign,
                    gridCount,
                    out gridIndex,
                    out travelSign);
            }
        }
    }
}
