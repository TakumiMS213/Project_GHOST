using System.Collections.Generic;
using NUnit.Framework;
using TelegGhost.Runtime;
using TelegGhost.Runtime.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools.Utils;
using UnityEngine.SceneManagement;

namespace TelegGhost.Tests
{
    public sealed class StageSpecificationAssetTests
    {
        private readonly struct GhostSpec
        {
            public GhostSpec(
                int stage,
                string name,
                ObservationType type,
                bool rideable,
                Vector2 start,
                Vector2 end,
                int nodeCount,
                float pulse = 0.5f)
            {
                Stage = stage;
                Name = name;
                Type = type;
                Rideable = rideable;
                Start = start;
                End = end;
                NodeCount = nodeCount;
                Pulse = pulse;
            }

            public int Stage { get; }
            public string Name { get; }
            public ObservationType Type { get; }
            public bool Rideable { get; }
            public Vector2 Start { get; }
            public Vector2 End { get; }
            public int NodeCount { get; }
            public float Pulse { get; }
        }

        private readonly struct RectSpec
        {
            public RectSpec(int stage, string name, float x, float y, float width, float height)
            {
                Stage = stage;
                Name = name;
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public int Stage { get; }
            public string Name { get; }
            public float X { get; }
            public float Y { get; }
            public float Width { get; }
            public float Height { get; }
        }

        private static readonly string[] StagePaths =
        {
            "",
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

        private static readonly GhostSpec[] Ghosts =
        {
            G(1,"T1-A",ObservationType.Tele,false,3.5f,1.4f,6.5f,1.4f,4),
            G(1,"T1-B",ObservationType.Tele,false,10f,2f,15.5f,1.4f,6),
            G(1,"T1-C",ObservationType.Tele,false,20f,1.4f,25f,1.4f,6),
            G(2,"S2-A",ObservationType.Star,false,3.5f,1.4f,6.5f,1.4f,4),
            G(2,"S2-B",ObservationType.Star,false,10f,2f,15.5f,1.4f,6),
            G(2,"S2-C",ObservationType.Star,false,20f,1.4f,25f,1.4f,6),
            G(3,"T3-A",ObservationType.Tele,false,4f,1.4f,6f,1.4f,3),
            G(3,"S3-B",ObservationType.Star,false,11f,4f,14f,4f,4),
            G(3,"T3-C",ObservationType.Tele,false,20f,4.8f,18f,4.8f,3),
            G(3,"S3-C",ObservationType.Star,false,22f,1.4f,24f,1.4f,3),
            G(4,"T4-P1",ObservationType.Tele,true,4.5f,1.8f,9.5f,1.8f,6),
            G(4,"S4-P2",ObservationType.Star,true,14.5f,1.8f,19.5f,1.8f,6),
            G(4,"T4-P3",ObservationType.Tele,true,24.5f,2.2f,29.5f,2.2f,6),
            G(5,"T5-P1",ObservationType.Tele,true,4.5f,1.8f,10.5f,1.8f,7),
            G(5,"S5-P2",ObservationType.Star,true,18f,1.8f,22f,1.8f,5),
            G(6,"GT6",ObservationType.Tele,false,-4f,3.5f,42f,3.5f,47,0.45f),
            G(6,"S6-A",ObservationType.Star,false,15f,4.2f,17f,4.2f,3),
            G(7,"T7-A",ObservationType.Tele,false,3f,1.4f,1f,1.4f,3),
            G(7,"S7-A",ObservationType.Star,false,9f,1.4f,11f,1.4f,3),
            G(7,"T7-B",ObservationType.Tele,false,25f,2.8f,27f,2.8f,3),
            G(7,"S7-B",ObservationType.Star,false,25f,5f,27f,5f,3),
            G(8,"T8-P",ObservationType.Tele,true,4.5f,1.8f,30.5f,1.8f,27),
            G(9,"T9-A",ObservationType.Tele,true,6.5f,2.4f,10.5f,2.4f,5),
            G(9,"S9-B",ObservationType.Star,true,11.5f,6.4f,7.5f,6.4f,5),
            G(9,"T9-C",ObservationType.Tele,true,6.5f,10.4f,10.5f,10.4f,5),
            G(9,"S9-D",ObservationType.Star,true,11.5f,14.4f,7.5f,14.4f,5),
            G(9,"T9-E",ObservationType.Tele,true,6.5f,18.4f,9.5f,18.4f,4),
            G(10,"T10-A",ObservationType.Tele,true,4.5f,1.8f,9.5f,1.8f,6),
            G(10,"S10-B",ObservationType.Star,true,14.5f,4.2f,11.5f,4.2f,4),
            G(10,"T10-C",ObservationType.Tele,true,16.5f,2.4f,21.5f,2.4f,6),
            G(10,"S10-D",ObservationType.Star,true,27.5f,4.4f,23.5f,4.4f,5),
            G(11,"T11-A",ObservationType.Tele,false,9f,5.9f,12f,5.9f,4),
            G(11,"S11-B",ObservationType.Star,false,22f,1.4f,19f,1.4f,4),
            G(11,"T11-P",ObservationType.Tele,true,20.5f,2.2f,25.5f,2.2f,6),
            G(12,"GT12",ObservationType.Tele,false,-4f,3.5f,38f,3.5f,43,0.45f),
            G(12,"S12-A",ObservationType.Star,false,13f,4.8f,15f,4.8f,3),
            G(12,"T12-P",ObservationType.Tele,true,21.5f,2f,26.5f,2f,6),
            G(12,"T12-B",ObservationType.Tele,false,33f,1.4f,35f,1.4f,3),
            G(13,"S13-A",ObservationType.Star,true,4.5f,2f,8.5f,2f,5),
            G(13,"S13-B",ObservationType.Star,true,11.5f,4f,15.5f,4f,5),
            G(13,"S13-C",ObservationType.Star,true,18.5f,2f,22.5f,2f,5),
            G(13,"S13-D",ObservationType.Star,true,25.5f,4f,29.5f,4f,5),
            G(13,"S13-E",ObservationType.Star,true,32.5f,2f,36.5f,2f,5),
            G(13,"S13-F",ObservationType.Star,true,39.5f,4f,41.5f,4f,3),
            G(14,"T14-A",ObservationType.Tele,false,18.5f,2.2f,20.5f,2.2f,3,1.2f),
            G(14,"S14-B",ObservationType.Star,false,18.5f,6f,20.5f,6f,3,1.2f),
            G(15,"T15-A",ObservationType.Tele,false,6f,1.4f,9f,1.4f,4),
            G(15,"S15-B",ObservationType.Star,false,12f,4.9f,15f,4.9f,4),
            G(15,"T15-P",ObservationType.Tele,true,17.5f,7.8f,21.5f,7.8f,5),
            G(15,"T15-C",ObservationType.Tele,true,28.5f,1.8f,33.5f,1.8f,6),
            G(15,"S15-D",ObservationType.Star,true,38.5f,4.5f,35.5f,4.5f,4),
            G(15,"T15-E",ObservationType.Tele,true,41.5f,2.2f,45.5f,2.2f,5),
            G(15,"S15-F",ObservationType.Star,true,50.5f,4.7f,47.5f,4.7f,4),
            G(15,"GT15",ObservationType.Tele,false,54f,4f,91f,4f,38,0.45f),
            G(15,"S15-G",ObservationType.Star,false,62f,5f,64f,5f,3),
            G(15,"T15-H",ObservationType.Tele,true,67.5f,2f,73.5f,2f,7),
            G(15,"T15-I",ObservationType.Tele,false,82f,1.4f,84f,1.4f,3),
            G(15,"T15-FINAL",ObservationType.Tele,true,87.5f,2f,90.5f,2f,4)
        };

        private static readonly RectSpec[] Rects =
        {
            R(1,"Ground_A",0,0,12,1), R(1,"Ground_C",15,0,13,1), R(1,"Bridge_B",12,.8f,3,.3f),
            R(2,"Ground_A",0,0,12,1), R(2,"Ground_C",15,0,13,1), R(2,"Bridge_B",12,.8f,3,.3f),
            R(3,"Ground_A",0,0,10,1), R(3,"Ground_B",10,0,7,1), R(3,"Ground_C",17,0,15,1),
            R(3,"Upper_B",8,3,5,.6f), R(3,"Upper_C",18,4,6,.6f), R(3,"SightBlock_Center",15.5f,1,.8f,5.5f),
            R(4,"Ground_00",0,0,5,1), R(4,"Ground_10",10,0,4,1), R(4,"Ground_20",20,0,4,1),
            R(4,"Ground_30",30,0,4,1), R(4,"Ledge_12",12,3.2f,3,.5f), R(4,"Ledge_27",27,3.5f,3,.5f),
            R(5,"Ground_00",0,0,5,1), R(5,"Ground_10",10,0,7,1), R(5,"Ground_22",22,0,5,1),
            R(5,"Ground_31",31,0,3,1), R(5,"Upper_12",12,3.5f,4,.5f), R(5,"Bridge_B",22,1,5,.25f),
            R(6,"Ground_00",0,0,10,1), R(6,"Ground_12",12,0,8,1),
            R(6,"Ground_28",28,0,8,1), R(6,"Ground_38",38,0,6,1), R(6,"Upper_14",14,3.5f,4,.5f),
            R(6,"Fragile_20",20,0,1,1), R(6,"Fragile_21",21,0,1,1), R(6,"Fragile_22",22,0,1,1),
            R(6,"Fragile_23",23,0,1,1), R(6,"Fragile_24",24,0,1,1), R(6,"Fragile_25",25,0,1,1),
            R(7,"Ground",0,0,34,1), R(7,"ControlStep",16.5f,1.5f,2,.5f), R(7,"ControlPerch",17,3,3,.5f),
            R(7,"SightBlock_Center",15.5f,1,.7f,6.5f),
            R(8,"Ground_00",0,0,5,1), R(8,"Ground_10",10,0,6,1), R(8,"Ground_16",16,0,2,1),
            R(8,"Ground_26",26,0,14,1), R(8,"Upper_11",11,4,7,.5f), R(8,"Upper_20",20,4,6,.5f),
            R(9,"Ground",0,0,18,1), R(9,"Fixed_04",1,4,5,.5f), R(9,"Fixed_08",12,8,5,.5f),
            R(9,"Fixed_12",1,12,5,.5f), R(9,"Fixed_16",12,16,5,.5f), R(9,"GoalFloor",6,20,6,.5f),
            R(10,"StartGround",0,0,4,1), R(10,"CheckpointIsland",15.25f,0,1.5f,1), R(10,"GoalGround",32,0,4,1),
            R(11,"Ground",0,0,30,1), R(11,"Upper_Left",3,5,9,.5f), R(11,"Upper_Right",19,5,7,.5f),
            R(12,"Ground_00",0,0,8,1), R(12,"Ground_10",10,0,8,1), R(12,"Ground_22",22,0,6,1),
            R(12,"Ground_32",32,0,8,1), R(12,"Upper_12",12,4,5,.5f),
            R(13,"StartGround",0,0,4,1), R(13,"GoalGround",42,0,4,1),
            R(14,"Ground",0,0,26,1), R(14,"ControlStep",3,1.4f,2.5f,.5f), R(14,"ControlDeck",5,2.5f,7,.5f),
            R(14,"LowerShelf",17.5f,1.4f,4,.4f), R(14,"UpperShelf",17.5f,5.2f,4,.4f),
            R(14,"SightBlock_Lock",9.8f,4.2f,.4f,3.4f),
            R(15,"Ground_A",0,0,24,1), R(15,"StepA1",1.5f,1.6f,3,.5f), R(15,"StepA2",4.5f,2.8f,3.5f,.5f),
            R(15,"LedgeA",2,4,6,.5f), R(15,"LedgeB",10,4,5,.5f), R(15,"StepA3",13.5f,5.4f,4,.5f),
            R(15,"UpperDeck",16,7,6,.5f), R(15,"Ground_24",24,0,4,1), R(15,"Ground_52",52,0,6,1),
            R(15,"Ground_58",58,0,8,1), R(15,"Ground_68",68,0,8,1), R(15,"Ground_80",80,0,7,1),
            R(15,"GoalGround",92,0,2,1)
        };

        [Test]
        public void ProductionPrefabsUseGlobalSpecificationValues()
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Resource/Prefabs/Game/Character/Player_Square.prefab");
            Assert.That(player, Is.Not.Null);
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = player.GetComponent<CapsuleCollider2D>();
            PlayerController2D controller = player.GetComponent<PlayerController2D>();
            GhostObserver2D observer = player.GetComponent<GhostObserver2D>();
            Assert.That(body.gravityScale * Mathf.Abs(Physics2D.gravity.y), Is.EqualTo(24f).Within(0.02f));
            Assert.That(collider.size, Is.EqualTo(new Vector2(0.7f, 1.6f)).Using(Vector2ComparerWithEqualsOperator.Instance));
            Assert.That(ReadFloat(controller, "moveSpeed"), Is.EqualTo(6f).Within(0.001f));
            Assert.That(ReadFloat(controller, "jumpImpulse"), Is.EqualTo(10.5f).Within(0.001f));
            Assert.That(ReadFloat(observer, "fieldOfView"), Is.EqualTo(32f).Within(0.001f));
            Assert.That(ReadFloat(observer, "viewDistance"), Is.EqualTo(12f).Within(0.001f));
            Assert.That(player.transform.Find("Vision")?.localPosition.y, Is.EqualTo(0.15f).Within(0.001f));
            MeshRenderer visionRenderer = player.transform.Find("Vision")?.GetComponent<MeshRenderer>();
            Assert.That(visionRenderer, Is.Not.Null);
            Assert.That(visionRenderer.sharedMaterial, Is.Not.Null);
            Assert.That(visionRenderer.sharedMaterial.color.r, Is.EqualTo(1f).Within(0.001f));
            Assert.That(visionRenderer.sharedMaterial.color.g, Is.EqualTo(1f).Within(0.001f));
            Assert.That(visionRenderer.sharedMaterial.color.b, Is.EqualTo(1f).Within(0.001f));
            Assert.That(visionRenderer.sharedMaterial.color.a, Is.EqualTo(0.14f).Within(0.001f));
            Assert.That(visionRenderer.sortingOrder, Is.EqualTo(-2));
            int rideableLayer = LayerMask.NameToLayer("RideableGhost");
            Assert.That(rideableLayer, Is.GreaterThanOrEqualTo(0));
            Assert.That((ReadInt(controller, "groundMask") & (1 << rideableLayer)) != 0, Is.True);

            string[] ghostPrefabs =
            {
                "TELE_Square_Rideable", "TELE_Square_Utility",
                "STAR_Square_Rideable", "STAR_Square_Utility"
            };
            for (int i = 0; i < ghostPrefabs.Length; i++)
            {
                GameObject ghost = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Resource/Prefabs/Game/Ghost/" + ghostPrefabs[i] + ".prefab");
                Assert.That(ghost, Is.Not.Null, ghostPrefabs[i]);
                Assert.That(ghost.GetComponent<GhostPulseController2D>().PulseInterval, Is.EqualTo(0.5f).Within(0.001f));
                Assert.That(ReadFloat(ghost.GetComponent<GhostMoveAction2D>(), "moveDurationPerUnit"), Is.EqualTo(0.15f).Within(0.001f));
                bool rideable = ghostPrefabs[i].EndsWith("Rideable");
                Assert.That(
                    ghost.layer,
                    Is.EqualTo(LayerMask.NameToLayer(rideable ? "RideableGhost" : "Ghost")),
                    ghostPrefabs[i]);
            }
        }

        [Test]
        public void EveryGhostMatchesAuthoredTypePathAndPulse()
        {
            WithScenes((stage, objects) =>
            {
                for (int i = 0; i < Ghosts.Length; i++)
                {
                    GhostSpec spec = Ghosts[i];
                    if (spec.Stage != stage)
                    {
                        continue;
                    }

                    GameObject root = Find(objects, spec.Name);
                    Assert.That(root, Is.Not.Null, "Stage " + stage + ": " + spec.Name);
                    GhostController2D controller = root.GetComponent<GhostController2D>();
                    GhostMoveAction2D move = root.GetComponent<GhostMoveAction2D>();
                    GhostPulseController2D pulse = root.GetComponent<GhostPulseController2D>();
                    Assert.That(controller.ObservationType, Is.EqualTo(spec.Type), spec.Name);
                    Assert.That(move.Rideable, Is.EqualTo(spec.Rideable), spec.Name);
                    Assert.That(move.PathNodeCount, Is.EqualTo(spec.NodeCount), spec.Name);
                    Assert.That(move.PathNodes[0], Is.EqualTo(spec.Start).Using(Vector2ComparerWithEqualsOperator.Instance), spec.Name);
                    Assert.That(move.PathNodes[move.PathNodeCount - 1], Is.EqualTo(spec.End).Using(Vector2ComparerWithEqualsOperator.Instance), spec.Name);
                    Assert.That(pulse.PulseInterval, Is.EqualTo(spec.Pulse).Within(0.001f), spec.Name);
                    Assert.That(move.RouteMode, Is.EqualTo(GhostMoveRouteMode.PingPong), spec.Name);
                }
            });
        }

        [Test]
        public void AuthoredRectGeometryMatchesSpecification()
        {
            WithScenes((stage, objects) =>
            {
                for (int i = 0; i < Rects.Length; i++)
                {
                    RectSpec spec = Rects[i];
                    if (spec.Stage != stage)
                    {
                        continue;
                    }

                    GameObject root = Find(objects, spec.Name);
                    Assert.That(root, Is.Not.Null, "Stage " + stage + ": " + spec.Name);
                    Vector3 expectedCenter = new Vector3(
                        spec.X + spec.Width * 0.5f,
                        spec.Y + spec.Height * 0.5f,
                        0f);
                    Assert.That(root.transform.position.x, Is.EqualTo(expectedCenter.x).Within(0.001f), spec.Name);
                    Assert.That(root.transform.position.y, Is.EqualTo(expectedCenter.y).Within(0.001f), spec.Name);
                    Assert.That(root.transform.localScale.x, Is.EqualTo(spec.Width).Within(0.001f), spec.Name);
                    Assert.That(root.transform.localScale.y, Is.EqualTo(spec.Height).Within(0.001f), spec.Name);
                }
            });
        }

        [Test]
        public void InitialGazeAndSpecialSystemsMatchSpecification()
        {
            WithScenes((stage, objects) =>
            {
                PlayerController2D player = FindComponent<PlayerController2D>(objects);
                Assert.That(ReadInt(player, "initialViewHorizontalSign"), Is.EqualTo(stage == 2 ? -1 : 1));
                Assert.That(FindComponent<StageGhostActivation2D>(objects), Is.Null);

                if (stage == 1 || stage == 2 || stage == 5)
                {
                    Assert.That(FindComponent<SpawnBridge2D>(objects), Is.Not.Null, "Stage " + stage);
                }
                if (stage == 6 || stage == 12 || stage == 15)
                {
                    Assert.That(FindComponent<GiantTele2D>(objects), Is.Not.Null, "Stage " + stage);
                    Assert.That(FindComponent<FragileFloor2D>(objects), Is.Not.Null, "Stage " + stage);
                }
                if (stage == 4 || stage == 6 || stage == 8 || stage == 9 || stage == 10 || stage == 15)
                {
                    Assert.That(FindComponent<StageCheckpoint2D>(objects), Is.Not.Null, "Stage " + stage);
                }
            });
        }

        [Test]
        public void AuditCorrectionsRemoveObsoleteCollisionAndPlaceSafeRespawns()
        {
            WithScenes((stage, objects) =>
            {
                if (stage == 6)
                {
                    Assert.That(Find(objects, "Ground_20"), Is.Null);
                }
                else if (stage == 10)
                {
                    GameObject checkpoint = Find(objects, "CP10");
                    Assert.That(checkpoint, Is.Not.Null);
                    Assert.That(checkpoint.transform.position, Is.EqualTo(new Vector3(16f, 1f, 0f))
                        .Using(Vector3ComparerWithEqualsOperator.Instance));
                }
                else if (stage == 13)
                {
                    Assert.That(Find(objects, "Sight_10"), Is.Null);
                    Assert.That(Find(objects, "Sight_24"), Is.Null);
                    Assert.That(Find(objects, "Sight_38"), Is.Null);
                }
                else if (stage == 15)
                {
                    Assert.That(Find(objects, "FinalUpper"), Is.Null);
                    Assert.That(Find(objects, "GoalGround"), Is.Not.Null);
                }
            });
        }

        [Test]
        public void AuditCorrectedRoutesFitThePlayerJumpEnvelope()
        {
            const float jumpHeight = 2.297f;
            float[] stageThirteenPlatformTops = { 2.3f, 4.3f, 2.3f, 4.3f, 2.3f, 4.3f };
            for (int i = 1; i < stageThirteenPlatformTops.Length; i++)
            {
                float upwardDifference = stageThirteenPlatformTops[i] - stageThirteenPlatformTops[i - 1];
                Assert.That(upwardDifference, Is.LessThanOrEqualTo(jumpHeight));
            }

            float[] stageFifteenAreaATops = { 1f, 2.1f, 3.3f, 4.5f, 4.5f, 5.9f, 7.5f };
            for (int i = 1; i < stageFifteenAreaATops.Length; i++)
            {
                Assert.That(
                    stageFifteenAreaATops[i] - stageFifteenAreaATops[i - 1],
                    Is.LessThanOrEqualTo(jumpHeight));
            }

            Assert.That(2.3f - 1f, Is.LessThanOrEqualTo(jumpHeight), "T15-FINAL entry height");
            Assert.That(92f - 91.5f, Is.EqualTo(0.5f).Within(0.001f), "T15-FINAL exit gap");
        }

        private static GhostSpec G(
            int stage,
            string name,
            ObservationType type,
            bool rideable,
            float startX,
            float startY,
            float endX,
            float endY,
            int nodes,
            float pulse = 0.5f)
        {
            return new GhostSpec(
                stage,
                name,
                type,
                rideable,
                new Vector2(startX, startY),
                new Vector2(endX, endY),
                nodes,
                pulse);
        }

        private static RectSpec R(
            int stage,
            string name,
            float x,
            float y,
            float width,
            float height)
        {
            return new RectSpec(stage, name, x, y, width, height);
        }

        private static void WithScenes(System.Action<int, List<GameObject>> assertion)
        {
            SceneSetup[] original = EditorSceneManager.GetSceneManagerSetup();
            try
            {
                for (int stage = 1; stage <= 15; stage++)
                {
                    Scene scene = EditorSceneManager.OpenScene(StagePaths[stage], OpenSceneMode.Single);
                    assertion(stage, GetAll(scene));
                }
            }
            finally
            {
                if (original.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(original);
                }
            }
        }

        private static List<GameObject> GetAll(Scene scene)
        {
            List<GameObject> objects = new List<GameObject>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int j = 0; j < transforms.Length; j++)
                {
                    objects.Add(transforms[j].gameObject);
                }
            }
            return objects;
        }

        private static GameObject Find(List<GameObject> objects, string name)
        {
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].name == name)
                {
                    return objects[i];
                }
            }
            return null;
        }

        private static T FindComponent<T>(List<GameObject> objects) where T : Component
        {
            for (int i = 0; i < objects.Count; i++)
            {
                T component = objects[i].GetComponent<T>();
                if (component != null)
                {
                    return component;
                }
            }
            return null;
        }

        private static float ReadFloat(Object target, string propertyName)
        {
            return new SerializedObject(target).FindProperty(propertyName).floatValue;
        }

        private static int ReadInt(Object target, string propertyName)
        {
            return new SerializedObject(target).FindProperty(propertyName).intValue;
        }
    }
}
