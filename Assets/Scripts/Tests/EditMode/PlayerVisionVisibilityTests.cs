using NUnit.Framework;
using System.Reflection;
using TelegGhost.Runtime;
using UnityEditor;
using UnityEngine;

namespace TelegGhost.Tests
{
    public sealed class PlayerVisionVisibilityTests
    {
        private GameObject playerObject;
        private GameObject targetObject;
        private PlayerVision2D vision;
        private BoxCollider2D targetCollider;

        [SetUp]
        public void SetUp()
        {
            playerObject = new GameObject("Player_Test") { layer = 8 };
            playerObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            playerObject.AddComponent<BoxCollider2D>();
            PlayerController2D player = playerObject.AddComponent<PlayerController2D>();

            Transform origin = new GameObject("VisionOrigin_Test").transform;
            origin.SetParent(playerObject.transform, false);

            vision = playerObject.AddComponent<PlayerVision2D>();
            SerializedObject serializedVision = new SerializedObject(vision);
            serializedVision.FindProperty("player").objectReferenceValue = player;
            serializedVision.FindProperty("visionOrigin").objectReferenceValue = origin;
            serializedVision.FindProperty("fieldOfView").floatValue = 90f;
            serializedVision.FindProperty("viewDistance").floatValue = 10f;
            serializedVision.FindProperty("obstructionMask").intValue = 1;
            serializedVision.ApplyModifiedPropertiesWithoutUndo();
            MethodInfo awake = typeof(PlayerVision2D).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake?.Invoke(vision, null);

            targetObject = new GameObject("Target_Test") { layer = 9 };
            targetObject.transform.position = new Vector3(4f, 0f);
            targetCollider = targetObject.AddComponent<BoxCollider2D>();
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(targetObject);
            Object.DestroyImmediate(playerObject);
        }

        [Test]
        public void TargetInsideUnobstructedConeIsVisible()
        {
            Assert.That(vision.IsVisible(targetCollider), Is.True);
        }

        [Test]
        public void SolidWallBlocksObservation()
        {
            GameObject wall = CreateWall(false);
            Assert.That(vision.IsVisible(targetCollider), Is.False);
            Object.DestroyImmediate(wall);
        }

        [Test]
        public void TriggerDoesNotBlockObservation()
        {
            GameObject trigger = CreateWall(true);
            Assert.That(vision.IsVisible(targetCollider), Is.True);
            Object.DestroyImmediate(trigger);
        }

        private static GameObject CreateWall(bool isTrigger)
        {
            GameObject wall = new GameObject("Wall_Test");
            wall.transform.position = new Vector3(2f, 0f);
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.5f, 2f);
            collider.isTrigger = isTrigger;
            Physics2D.SyncTransforms();
            return wall;
        }
    }
}
