using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TelegGhost.Runtime;
using TelegGhost.Runtime.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TelegGhost.Tests
{
    public sealed class GameStagePlayModeTests
    {
        private static readonly string[] StageNames =
        {
            "Stage01_TELE",
            "Stage02_STAR",
            "Stage03_DualBasics",
            "Stage04_Rideable",
            "Stage05_RideAndSwitch",
            "Stage06_GiantTELE",
            "Stage07_DualControl",
            "Stage08_Reuse",
            "Stage09_Vertical",
            "Stage10_RealtimeView",
            "Stage11_MultiSwitch",
            "Stage12_IntenseMix",
            "Stage13_STARChain",
            "Stage14_DensePuzzle",
            "Stage15_Finale"
        };

        [UnityTest]
        public IEnumerator AllGameStagesLoadWithCoreRuntimeObjects()
        {
            for (int i = 0; i < StageNames.Length; i++)
            {
                yield return SceneManager.LoadSceneAsync(StageNames[i], LoadSceneMode.Single);
                yield return null;
                Assert.That(Object.FindAnyObjectByType<PlayerController2D>(), Is.Not.Null, StageNames[i]);
                Assert.That(Object.FindAnyObjectByType<GameStageFlow2D>(), Is.Not.Null, StageNames[i]);
                Assert.That(Object.FindAnyObjectByType<StageGhostActivation2D>(), Is.Null, StageNames[i]);
            }
        }

        [UnityTest]
        public IEnumerator StageOneTeleIsInvisibleAndEmitsPulseFeedback()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_TELE", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            GhostObserver2D observer = Object.FindAnyObjectByType<GhostObserver2D>();
            GameObject teleObject = GameObject.Find("T1-A");
            GhostObservationDetector2D detector = teleObject != null
                ? teleObject.GetComponent<GhostObservationDetector2D>()
                : null;
            GhostMoveAction2D move = teleObject != null
                ? teleObject.GetComponent<GhostMoveAction2D>()
                : null;
            GhostPulseController2D pulse = teleObject != null
                ? teleObject.GetComponent<GhostPulseController2D>()
                : null;
            Assert.That(observer, Is.Not.Null);
            Assert.That(detector, Is.Not.Null);
            Assert.That(move, Is.Not.Null);
            Assert.That(pulse, Is.Not.Null);

            Assert.That(detector.IsObserved, Is.True);
            yield return new WaitForSeconds(0.65f);
            Assert.That(move.CurrentNodeIndex, Is.Zero);

            observer.enabled = false;
            detector.RefreshObservation();
            yield return null;

            Transform body = move.transform.Find("Visuals/Body");
            SpriteRenderer bodyRenderer = body != null ? body.GetComponent<SpriteRenderer>() : null;
            Assert.That(bodyRenderer, Is.Not.Null);
            Assert.That(bodyRenderer.color.a, Is.EqualTo(0f).Within(0.001f));

            bool sawAfterimage = false;
            bool sawAudio = false;
            float timeout = 1.2f;
            while (timeout > 0f && move.CurrentGridIndex == 0)
            {
                SpriteRenderer[] renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i].enabled && renderers[i].name.StartsWith("Afterimage_"))
                    {
                        sawAfterimage = true;
                    }
                }

                AudioSource source = pulse.GetComponent<AudioSource>();
                sawAudio |= source != null && source.isPlaying;
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(move.CurrentGridIndex, Is.GreaterThan(0));
            Assert.That(sawAfterimage, Is.True);
            Assert.That(sawAudio, Is.True);
        }

        [UnityTest]
        public IEnumerator OffCameraGhostPulseDoesNotPlayAudio()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_TELE", LoadSceneMode.Single);
            yield return null;

            GhostObserver2D observer = Object.FindAnyObjectByType<GhostObserver2D>();
            GameObject teleObject = GameObject.Find("T1-A");
            GhostObservationDetector2D detector = teleObject.GetComponent<GhostObservationDetector2D>();
            GhostPulseController2D pulse = teleObject.GetComponent<GhostPulseController2D>();
            Rigidbody2D body = teleObject.GetComponent<Rigidbody2D>();
            AudioSource source = teleObject.GetComponent<AudioSource>();
            Assert.That(observer, Is.Not.Null);
            Assert.That(detector, Is.Not.Null);
            Assert.That(pulse, Is.Not.Null);
            Assert.That(body, Is.Not.Null);
            Assert.That(source, Is.Not.Null);

            body.position = new Vector2(100f, 1.4f);
            teleObject.transform.position = body.position;
            Physics2D.SyncTransforms();
            observer.enabled = false;
            detector.RefreshObservation();

            bool sawAudio = false;
            float timeout = 0.9f;
            while (timeout > 0f)
            {
                sawAudio |= source.isPlaying;
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.That(pulse.IsActionExecuting, Is.True, "Off-camera pulse did not execute");
            Assert.That(sawAudio, Is.False);
        }

        [UnityTest]
        public IEnumerator ResetRestoresPlayerAndGhostThenGoalLoadsNextStage()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_TELE", LoadSceneMode.Single);
            yield return null;

            GameStageFlow2D flow = Object.FindAnyObjectByType<GameStageFlow2D>();
            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            GhostObserver2D observer = Object.FindAnyObjectByType<GhostObserver2D>();
            GameObject teleObject = GameObject.Find("T1-A");
            GhostObservationDetector2D detector = teleObject.GetComponent<GhostObservationDetector2D>();
            GhostMoveAction2D move = teleObject.GetComponent<GhostMoveAction2D>();
            observer.enabled = false;
            detector.RefreshObservation();

            yield return new WaitForSeconds(0.9f);
            Assert.That(move.CurrentGridIndex, Is.GreaterThan(0));
            player.Teleport(new Vector2(10f, 1f));
            flow.Respawn();
            yield return new WaitForFixedUpdate();
            Assert.That(move.CurrentGridIndex, Is.Zero);
            Assert.That(player.transform.position.x, Is.EqualTo(1.5f).Within(0.05f));
            Assert.That(player.transform.position.y, Is.EqualTo(1.8f).Within(0.05f));

            flow.CompleteStage();
            float timeout = 3f;
            while (SceneManager.GetActiveScene().name != "Stage02_STAR" && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Stage02_STAR"));
        }

        [UnityTest]
        public IEnumerator MovementPausesMidGridAndResumesFromRemainingDistance()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_TELE", LoadSceneMode.Single);
            yield return null;

            GhostObserver2D observer = Object.FindAnyObjectByType<GhostObserver2D>();
            GameObject teleObject = GameObject.Find("T1-A");
            GhostObservationDetector2D detector = teleObject.GetComponent<GhostObservationDetector2D>();
            GhostMoveAction2D move = teleObject.GetComponent<GhostMoveAction2D>();
            GhostPulseController2D pulse = teleObject.GetComponent<GhostPulseController2D>();
            Rigidbody2D body = teleObject.GetComponent<Rigidbody2D>();

            observer.enabled = false;
            detector.RefreshObservation();
            float timeout = 1.2f;
            while (!pulse.IsActionExecuting && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            Assert.That(pulse.IsActionExecuting, Is.True);
            yield return new WaitForFixedUpdate();

            observer.enabled = true;
            detector.RefreshObservation();
            yield return new WaitForFixedUpdate();
            float pausedX = body.position.x;
            Assert.That(pausedX, Is.GreaterThan(3.5f));
            Assert.That(pausedX, Is.LessThan(4.5f));
            yield return new WaitForSeconds(0.2f);
            Assert.That(body.position.x, Is.EqualTo(pausedX).Within(0.002f));

            observer.enabled = false;
            detector.RefreshObservation();
            timeout = 0.8f;
            while (move.CurrentNodeIndex == 0 && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            Assert.That(move.CurrentNodeIndex, Is.EqualTo(1));
            Assert.That(body.position.x, Is.EqualTo(4.5f).Within(0.02f));
        }

        [UnityTest]
        public IEnumerator SwitchControlsBridgeAndDoorOnlyWhileOccupied()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_TELE", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            GhostSwitch2D targetSwitch = GameObject.Find("SW1-B").GetComponent<GhostSwitch2D>();
            DoorController2D door = GameObject.Find("D1-B").GetComponent<DoorController2D>();
            SpawnBridge2D bridge = GameObject.Find("Bridge_B").GetComponent<SpawnBridge2D>();
            Assert.That(targetSwitch.IsActive, Is.False);
            Assert.That(door.IsOpen, Is.False);
            Assert.That(bridge.IsActive, Is.False);

            player.Teleport(new Vector2(15.5f, 1.8f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(targetSwitch.IsActive, Is.True);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(bridge.IsActive, Is.True);

            player.Teleport(new Vector2(18f, 1.8f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(targetSwitch.IsActive, Is.False);
            Assert.That(door.IsOpen, Is.False);
            Assert.That(bridge.IsActive, Is.False);
        }

        [UnityTest]
        public IEnumerator CheckpointBecomesTheRespawnFootPosition()
        {
            yield return SceneManager.LoadSceneAsync("Stage04_Rideable", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            GameStageFlow2D flow = Object.FindAnyObjectByType<GameStageFlow2D>();
            player.Teleport(new Vector2(11f, 1.8f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(flow.CurrentRespawnFootPosition.x, Is.EqualTo(11f).Within(0.01f));
            Assert.That(flow.CurrentRespawnFootPosition.y, Is.EqualTo(1f).Within(0.01f));

            player.Teleport(new Vector2(30f, 6f));
            flow.Respawn();
            yield return new WaitForFixedUpdate();
            Assert.That(player.transform.position.x, Is.EqualTo(11f).Within(0.05f));
            Assert.That(player.transform.position.y, Is.EqualTo(1.8f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator RideableGhostCountsAsGroundForJumping()
        {
            yield return SceneManager.LoadSceneAsync("Stage04_Rideable", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            GameObject platform = GameObject.Find("T4-P1");
            Assert.That(player, Is.Not.Null);
            Assert.That(platform, Is.Not.Null);
            Assert.That(platform.layer, Is.EqualTo(LayerMask.NameToLayer("RideableGhost")));

            GhostPulseController2D pulse = platform.GetComponent<GhostPulseController2D>();
            if (pulse != null)
            {
                pulse.enabled = false;
            }

            player.Teleport(new Vector2(platform.transform.position.x, platform.transform.position.y + 1.1f));
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            MethodInfo isGrounded = typeof(PlayerController2D).GetMethod(
                "IsGrounded",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(isGrounded, Is.Not.Null);
            Assert.That((bool)isGrounded.Invoke(player, null), Is.True);

            FieldInfo jumpBuffer = typeof(PlayerController2D).GetField(
                "jumpBufferTimer",
                BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo fixedUpdate = typeof(PlayerController2D).GetMethod(
                "FixedUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
            Assert.That(jumpBuffer, Is.Not.Null);
            Assert.That(fixedUpdate, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);
            jumpBuffer.SetValue(player, 0.1f);
            fixedUpdate.Invoke(player, null);
            Assert.That(playerBody.linearVelocity.y, Is.EqualTo(10.5f).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator GiantTeleBreaksFragileFloorAndRetryRestoresIt()
        {
            yield return SceneManager.LoadSceneAsync("Stage06_GiantTELE", LoadSceneMode.Single);
            yield return null;

            GameObject giant = GameObject.Find("GT6");
            GhostPulseController2D giantPulse = giant.GetComponent<GhostPulseController2D>();
            Rigidbody2D giantBody = giant.GetComponent<Rigidbody2D>();
            FragileFloor2D floor = GameObject.Find("Fragile_20").GetComponent<FragileFloor2D>();
            GameStageFlow2D flow = Object.FindAnyObjectByType<GameStageFlow2D>();
            giantPulse.enabled = false;
            giantBody.position = new Vector2(20.5f, 3.5f);
            Physics2D.SyncTransforms();

            yield return new WaitForSeconds(0.16f);
            Assert.That(floor.IsBroken, Is.True);
            flow.Respawn();
            yield return new WaitForFixedUpdate();
            Assert.That(floor.IsBroken, Is.False);
            Assert.That(floor.GetComponent<Collider2D>().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator AuditCorrectedControlPointsProduceRequiredObservationStates()
        {
            yield return SceneManager.LoadSceneAsync("Stage07_DualControl", LoadSceneMode.Single);
            yield return null;

            PlayerController2D player = Object.FindAnyObjectByType<PlayerController2D>();
            GhostObserver2D observer = Object.FindAnyObjectByType<GhostObserver2D>();
            Transform tele = GameObject.Find("T7-B").transform;
            Transform star = GameObject.Find("S7-B").transform;
            player.SetInputEnabled(false);
            player.Teleport(new Vector2(18f, 1.8f));
            Physics2D.SyncTransforms();

            player.ConfigureInitialView(-1, false);
            Assert.That(observer.CanObserve(tele.position), Is.False, "P7-L Left TELE");
            Assert.That(observer.CanObserve(star.position), Is.False, "P7-L Left STAR");

            player.ConfigureInitialView(1, true);
            Assert.That(observer.CanObserve(tele.position), Is.False, "P7-L RightUp TELE");
            Assert.That(observer.CanObserve(star.position), Is.True, "P7-L RightUp STAR");

            player.ConfigureInitialView(1, false);
            Assert.That(observer.CanObserve(tele.position), Is.True, "P7-L Right TELE");
            Assert.That(observer.CanObserve(star.position), Is.False, "P7-L Right STAR");

            player.Teleport(new Vector2(18f, 4.3f));
            Physics2D.SyncTransforms();
            Assert.That(observer.CanObserve(tele.position), Is.True, "P7-U Right TELE");
            Assert.That(observer.CanObserve(star.position), Is.True, "P7-U Right STAR");

            yield return SceneManager.LoadSceneAsync("Stage14_DensePuzzle", LoadSceneMode.Single);
            yield return null;

            player = Object.FindAnyObjectByType<PlayerController2D>();
            observer = Object.FindAnyObjectByType<GhostObserver2D>();
            tele = GameObject.Find("T14-A").transform;
            star = GameObject.Find("S14-B").transform;
            player.SetInputEnabled(false);
            player.Teleport(new Vector2(7f, 3.8f));
            Physics2D.SyncTransforms();

            player.ConfigureInitialView(-1, false);
            Assert.That(observer.CanObserve(tele.position), Is.False, "P14-Lock Left TELE");
            Assert.That(observer.CanObserve(star.position), Is.False, "P14-Lock Left STAR");

            player.ConfigureInitialView(1, false);
            Assert.That(observer.CanObserve(tele.position), Is.True, "P14-Lock Right TELE");
            Assert.That(observer.CanObserve(star.position), Is.False, "P14-Lock Right STAR");

            player.Teleport(new Vector2(11f, 3.8f));
            Physics2D.SyncTransforms();
            Assert.That(observer.CanObserve(tele.position), Is.True, "P14-Star Right TELE");
            Assert.That(observer.CanObserve(star.position), Is.True, "P14-Star Right STAR");
        }

        [UnityTest]
        public IEnumerator WallCollisionReturnsGhostToCurrentNodeWithoutReversing()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_TELE", LoadSceneMode.Single);
            yield return null;

            GameObject wall = new GameObject("Wall_Rebound_Test");
            wall.transform.position = new Vector3(4.1f, 1.4f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 2f);
            Physics2D.SyncTransforms();

            GhostObserver2D observer = Object.FindAnyObjectByType<GhostObserver2D>();
            GameObject teleObject = GameObject.Find("T1-A");
            GhostObservationDetector2D detector = teleObject.GetComponent<GhostObservationDetector2D>();
            GhostMoveAction2D move = teleObject.GetComponent<GhostMoveAction2D>();
            observer.enabled = false;
            detector.RefreshObservation();
            yield return new WaitForSeconds(0.9f);

            Assert.That(move.CurrentNodeIndex, Is.Zero);
            Assert.That(teleObject.transform.position.x, Is.EqualTo(3.5f).Within(0.02f));
            Assert.That(move.ActionDirection.x, Is.GreaterThan(0.9f));
        }
    }
}
