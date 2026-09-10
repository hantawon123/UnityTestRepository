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
                Is.EqualTo("Idle"));
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
                Is.EqualTo("Carry_Run_Forward"));
            Assert.That(
                PlayerAnimationDriver.ResolveLocomotionClip(
                    PlayerPosture.Crouching, false, 2f, new Vector2(0f, 1f), 4f, 7f),
                Is.EqualTo("Crouch_Walk_Forward"));
            Assert.That(
                PlayerAnimationDriver.ResolveLocomotionClip(
                    PlayerPosture.Prone, true, 0.8f, new Vector2(0f, -1f), 4f, 7f),
                    Is.EqualTo("Carry_Crawl_Back"));
        }

        [Test]
        public void PlaybackSpeed_ScalesWithPlanarSpeedForLocomotion()
        {
            Assert.That(
                PlayerAnimationDriver.ResolvePlaybackSpeed("Walk_Forward", 4f, 4f, 7f, 2f, 0.8f),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                PlayerAnimationDriver.ResolvePlaybackSpeed("Run_Forward", 10.5f, 4f, 7f, 2f, 0.8f),
                Is.EqualTo(1.5f).Within(0.001f));
            Assert.That(
                PlayerAnimationDriver.ResolvePlaybackSpeed("Crouch_Walk_Forward", 2f, 4f, 7f, 2f, 0.8f),
                Is.EqualTo(1f).Within(0.001f));
            Assert.That(
                PlayerAnimationDriver.ResolvePlaybackSpeed("Idle", 4f, 4f, 7f, 2f, 0.8f),
                Is.EqualTo(1f));
            Assert.That(
                PlayerAnimationDriver.ResolvePlaybackSpeed("Punch", 7f, 4f, 7f, 2f, 0.8f),
                Is.EqualTo(1f));
        }

        [Test]
        public void PickupPutDown_UsesPostureMatchedClips()
        {
            Assert.That(
                PlayerAnimationDriver.ResolvePickupClip(PlayerPosture.Standing),
                Is.EqualTo("Pickup_Low"));
            Assert.That(
                PlayerAnimationDriver.ResolvePickupClip(PlayerPosture.Crouching),
                Is.EqualTo("Pickup_Crouch"));
            Assert.That(
                PlayerAnimationDriver.ResolvePickupClip(PlayerPosture.Prone),
                Is.EqualTo("Pickup_Prone"));
            Assert.That(
                PlayerAnimationDriver.ResolvePutDownClip(PlayerPosture.Crouching),
                Is.EqualTo("PutDown_Crouch"));
            Assert.That(
                PlayerAnimationDriver.ResolvePutDownClip(PlayerPosture.Prone),
                Is.EqualTo("PutDown_Prone"));
        }

        [Test]
        public void PickupOneShot_IsInterruptedByLocomotion()
        {
            Assert.That(PlayerAnimationDriver.IsMovementInterruptible("Pickup_Low"), Is.True);
            Assert.That(PlayerAnimationDriver.IsMovementInterruptible("PutDown_Prone"), Is.True);
            Assert.That(PlayerAnimationDriver.IsMovementInterruptible("Land"), Is.True);
            Assert.That(PlayerAnimationDriver.IsMovementInterruptible("Carry_Land"), Is.True);
            Assert.That(PlayerAnimationDriver.IsMovementInterruptible("Prone_End"), Is.False);
            Assert.That(PlayerAnimationDriver.IsLocomotionMoving("Carry_Walk_Forward"), Is.True);
            Assert.That(PlayerAnimationDriver.IsLocomotionMoving("Carry_Crawl_Forward"), Is.True);
            Assert.That(PlayerAnimationDriver.IsLocomotionMoving("Carry_Idle"), Is.False);
            Assert.That(PlayerAnimationDriver.ResolveJumpClip(false), Is.EqualTo("Jump"));
            Assert.That(PlayerAnimationDriver.ResolveJumpClip(true), Is.EqualTo("Carry_Jump"));
            Assert.That(PlayerAnimationDriver.ResolveLandClip(false), Is.EqualTo("Land"));
            Assert.That(PlayerAnimationDriver.ResolveLandClip(true), Is.EqualTo("Carry_Land"));
            Assert.That(PlayerAnimationDriver.IsJumpState("Carry_Jump"), Is.True);
            Assert.That(PlayerAnimationDriver.IsJumpState("Fall"), Is.False);
            Assert.That(
                PlayerAnimationDriver.ResolvePunchClip(PlayerPosture.Standing, 0f, 4f, 7f),
                Is.EqualTo("Punch"));
            Assert.That(
                PlayerAnimationDriver.ResolvePunchClip(PlayerPosture.Standing, 4f, 4f, 7f),
                Is.EqualTo("Punch_Walk"));
            Assert.That(
                PlayerAnimationDriver.ResolvePunchClip(PlayerPosture.Standing, 7f, 4f, 7f),
                Is.EqualTo("Punch_Run"));
            Assert.That(
                PlayerAnimationDriver.ResolvePunchClip(PlayerPosture.Crouching, 0f, 4f, 7f),
                Is.EqualTo("Punch_Crouch"));
            Assert.That(
                PlayerAnimationDriver.ResolvePunchClip(PlayerPosture.Crouching, 2f, 4f, 7f),
                Is.EqualTo("Punch_Crouch_Walk"));
            Assert.That(
                PlayerAnimationDriver.ResolveHitClip(PlayerPosture.Standing, 4f, 4f, 7f),
                Is.EqualTo("Hit_Walk"));
            Assert.That(
                PlayerAnimationDriver.ResolveHitClip(PlayerPosture.Crouching, 0f, 4f, 7f),
                Is.EqualTo("Hit_Crouch"));
            Assert.That(
                PlayerAnimationDriver.ResolvePlaybackSpeed("Punch_Walk", 7f, 4f, 7f, 2f, 0.8f),
                Is.EqualTo(1f));
            Assert.That(
                PlayerAnimationDriver.ResolvePlaybackSpeed("Hit_Run", 7f, 4f, 7f, 2f, 0.8f),
                Is.EqualTo(1f));
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
            Assert.That(AssetDatabase.GetAssetPath(source), Does.Contain("FirstPlayerCapsule_Idle.fbx"));

            var animator = visual.GetComponentInChildren<Animator>(true);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.applyRootMotion, Is.False);
            var controller = animator.runtimeAnimatorController as AnimatorController;
            Assert.That(controller, Is.Not.Null);
            var names = controller.layers[0].stateMachine.states.Select(entry => entry.state.name).ToArray();
            Assert.That(names, Does.Contain("Idle"));
            Assert.That(names, Does.Contain("Walk_Left"));
            Assert.That(names, Does.Contain("Carry_Idle"));
            Assert.That(names, Does.Contain("Throw"));
            Assert.That(names, Does.Contain("Punch"));
            Assert.That(names, Does.Contain("Stunned"));

            var punch = controller.layers[0].stateMachine.states
                .Select(entry => entry.state)
                .First(state => state.name == "Punch");
            Assert.That(punch.motion, Is.Not.Null);
            Assert.That(punch.motion.name, Is.EqualTo("Punch"));

            var networked = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Game/Content/Prefabs/NetworkedPlayer.prefab");
            Assert.That(networked.GetComponentInChildren<Animator>(true), Is.Not.Null);
            Assert.That(prefab.GetComponent<AvatarAppearanceApplier>(), Is.Not.Null);
        }
    }
}
