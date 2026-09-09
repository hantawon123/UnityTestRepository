using System.Linq;
using Game.Client.Character;
using Game.Client.Players;
using Game.Core.Players;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class PlayerAnimationDriverTests
    {
        [Test]
        public void Direction_UsesPlusXAsLeft()
        {
            Assert.That(
                PlayerAnimationDriver.ResolveDirection(new Vector2(1f, 0f)),
                Is.EqualTo(PlayerAnimationDriver.MoveDirection.Left));
            Assert.That(
                PlayerAnimationDriver.ResolveDirection(new Vector2(-1f, 0f)),
                Is.EqualTo(PlayerAnimationDriver.MoveDirection.Right));
            Assert.That(
                PlayerAnimationDriver.ResolveDirection(new Vector2(0f, 1f)),
                Is.EqualTo(PlayerAnimationDriver.MoveDirection.Forward));
            Assert.That(
                PlayerAnimationDriver.ResolveDirection(new Vector2(0f, -1f)),
                Is.EqualTo(PlayerAnimationDriver.MoveDirection.Back));
        }

        [Test]
        public void Locomotion_PicksCarryAndStrafeClips()
        {
            Assert.That(
                PlayerAnimationDriver.ResolveLocomotionClip(
                    PlayerPosture.Standing, false, 0f, Vector2.zero, 4f, 7f),
                Is.EqualTo("Idle_Breathing"));
            Assert.That(
                PlayerAnimationDriver.ResolveLocomotionClip(
                    PlayerPosture.Standing, true, 0f, Vector2.zero, 4f, 7f),
                Is.EqualTo("Carry_Idle"));
            Assert.That(
                PlayerAnimationDriver.ResolveLocomotionClip(
                    PlayerPosture.Standing, false, 4f, new Vector2(-1f, 0f), 4f, 7f),
                Is.EqualTo("Walk_Right"));
            Assert.That(
                PlayerAnimationDriver.ResolveLocomotionClip(
                    PlayerPosture.Standing, true, 7f, new Vector2(0f, 1f), 4f, 7f),
                Is.EqualTo("Carry_Run"));
            Assert.That(
                PlayerAnimationDriver.ResolveLocomotionClip(
                    PlayerPosture.Crouching, false, 2f, new Vector2(0f, 1f), 4f, 7f),
                Is.EqualTo("Crouch_Walk_Forward_KneesUp"));
            Assert.That(
                PlayerAnimationDriver.ResolveLocomotionClip(
                    PlayerPosture.Prone, true, 0.8f, new Vector2(0f, -1f), 4f, 7f),
                    Is.EqualTo("Carry_Crawl_Back"));
        }

        [Test]
        public void PlayerCharacter_UsesFirstGenericVisualAndClips()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Content/Prefabs/PlayerCharacter.prefab");
            Assert.That(prefab, Is.Not.Null);

            var visual = prefab.transform.Find("Visual");
            Assert.That(visual, Is.Not.Null);
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(visual.gameObject);
            Assert.That(source, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(source), Does.Contain("FirstPlayerCapsule_Idle_Breathing_2s.fbx"));

            var animator = visual.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.applyRootMotion, Is.False);
            var controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.That(controller, Is.Not.Null);
            var names = controller.layers[0].stateMachine.states.Select(entry => entry.state.name).ToArray();
            Assert.That(names, Does.Contain("Idle_Breathing"));
            Assert.That(names, Does.Contain("Walk_Left"));
            Assert.That(names, Does.Contain("Carry_Idle"));
            Assert.That(names, Does.Contain("Throw"));
            Assert.That(names, Does.Contain("Punch"));
            Assert.That(names, Does.Contain("Stunned"));

            var punch = controller.layers[0].stateMachine.states
                .Select(entry => entry.state)
                .First(state => state.name == "Punch");
            var idle = controller.layers[0].stateMachine.states
                .Select(entry => entry.state)
                .First(state => state.name == "Idle_Breathing");
            Assert.That(punch.motion, Is.SameAs(idle.motion));

            var networked = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Content/Prefabs/NetworkedPlayer.prefab");
            Assert.That(networked.GetComponentInChildren<Animator>(true), Is.Not.Null);
            Assert.That(prefab.GetComponent<AvatarAppearanceApplier>(), Is.Not.Null);
        }
    }
}
