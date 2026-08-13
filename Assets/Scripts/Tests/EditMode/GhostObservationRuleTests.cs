using NUnit.Framework;
using TelegGhost.Runtime;
using UnityEngine;

namespace TelegGhost.Tests
{
    public sealed class GhostObservationRuleTests
    {
        [TestCase(false, true)]
        [TestCase(true, false)]
        public void TeleMovesOnlyWhileUnobserved(bool observed, bool expected)
        {
            Assert.That(GhostMover2D.ShouldMove(ObservationType.Tele, observed), Is.EqualTo(expected));
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void StarMovesOnlyWhileObserved(bool observed, bool expected)
        {
            Assert.That(GhostMover2D.ShouldMove(ObservationType.Star, observed), Is.EqualTo(expected));
        }

        [Test]
        public void TeleRequiresObservationBeforeReleaseAndCanBeFrozenAgain()
        {
            GhostHabitState state = GhostHabitState.Dormant;
            state = GhostHabitController2D.ResolveState(ObservationType.Tele, state, false);
            Assert.That(state, Is.EqualTo(GhostHabitState.Dormant));

            state = GhostHabitController2D.ResolveState(ObservationType.Tele, state, true);
            Assert.That(state, Is.EqualTo(GhostHabitState.Primed));

            state = GhostHabitController2D.ResolveState(ObservationType.Tele, state, true);
            Assert.That(state, Is.EqualTo(GhostHabitState.Primed));

            state = GhostHabitController2D.ResolveState(ObservationType.Tele, state, false);
            Assert.That(state, Is.EqualTo(GhostHabitState.Acting));

            state = GhostHabitController2D.ResolveState(ObservationType.Tele, state, true);
            Assert.That(state, Is.EqualTo(GhostHabitState.Frozen));
        }

        [Test]
        public void StarBloomsOnlyWhileObservedAfterDiscovery()
        {
            GhostHabitState state = GhostHabitController2D.ResolveState(
                ObservationType.Star,
                GhostHabitState.Dormant,
                true);
            Assert.That(state, Is.EqualTo(GhostHabitState.Acting));

            state = GhostHabitController2D.ResolveState(ObservationType.Star, state, false);
            Assert.That(state, Is.EqualTo(GhostHabitState.Frozen));
        }

        [TestCase(1, false, 1f, 0f)]
        [TestCase(-1, false, -1f, 0f)]
        [TestCase(1, true, 0.8191520f, 0.5735764f)]
        [TestCase(-1, true, -0.8191520f, 0.5735764f)]
        public void ViewDirectionHasFourDiscreteStates(
            int horizontalSign,
            bool lookUp,
            float expectedX,
            float expectedY)
        {
            Vector2 direction = PlayerController2D.ComposeViewDirection(horizontalSign, lookUp);
            Assert.That(direction.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(direction.y, Is.EqualTo(expectedY).Within(0.0001f));
        }
    }
}
