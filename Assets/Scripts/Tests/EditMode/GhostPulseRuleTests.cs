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
        public void ObserverUsesCenterPointAndSolidObstruction()
        {
            GhostObserver2D observer = CreateObserver(Vector2.zero, Vector2.right);
            Assert.That(observer.CanObserve(Vector2.zero), Is.True);
            Assert.That(observer.CanObserve(new Vector2(4f, 0f)), Is.True);

            GameObject wall = new GameObject("Pulse_Wall_Test");
            createdObjects.Add(wall);
            wall.transform.position = new Vector3(2f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 2f);
            Physics2D.SyncTransforms();

            Assert.That(observer.CanObserve(new Vector2(4f, 0f)), Is.False);
        }

        [Test]
        public void AnyObserverCanMarkPointObserved()
        {
            GhostObserver2D away = CreateObserver(Vector2.zero, Vector2.left);
            GhostObserver2D toward = CreateObserver(new Vector2(0f, 1f), Vector2.right);
            var observers = new List<GhostObserver2D> { away, toward };

            Assert.That(
                GhostObservationDetector2D.IsObservedByAny(new Vector2(4f, 1f), observers),
                Is.True);

            toward.enabled = false;
            Assert.That(
                GhostObservationDetector2D.IsObservedByAny(new Vector2(4f, 1f), observers),
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
    }
}
