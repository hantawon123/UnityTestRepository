#if UNITY_EDITOR
using System.Collections;
using Game.Client.Interactions;
using Game.Network.Match;
using Fusion;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests.PlayMode
{
    public sealed class MultiplayerRecoveryTests
    {
        private NetworkRunner runner;

        [UnityTest]
        public IEnumerator DisconnectedRunner_ShutsDownWithoutWaitingForHostExit()
        {
            using var room = new Game.Core.Lobby.RoomBrowserSystem();
            var network = new Game.Network.Session.NetworkRunnerService(null, room, null, null, null, null);
            runner = new GameObject("Disconnect single runner").AddComponent<NetworkRunner>();
            runner.AddCallbacks(network);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            typeof(Game.Network.Session.NetworkRunnerService).GetField("_runner", flags).SetValue(network, runner);
            var start = runner.StartGame(new StartGameArgs { GameMode = GameMode.Single });
            var deadline = Time.realtimeSinceStartup + 30f;
            while (!start.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(start.IsCompleted && start.Result.Ok, Is.True);
            typeof(Game.Network.Session.NetworkRunnerService).GetField("_exitReported", flags).SetValue(network, false);
            // Simulate the reason delivered before the transport disconnects.
            typeof(Game.Network.Session.NetworkRunnerService).GetMethod("ReportExit", flags).Invoke(network,
                new object[] { Game.Core.Rooms.RoomExitReason.Kicked });
            network.OnDisconnectedFromServer(runner, Fusion.Sockets.NetDisconnectReason.Timeout);
            deadline = Time.realtimeSinceStartup + 5f;
            while ((network.HasRoomSession || network.IsRoomExitPending) && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(network.HasRoomSession, Is.False);
            Assert.That(network.IsRoomExitPending, Is.False);
            Assert.That(room.LastExit.CurrentValue, Is.EqualTo(Game.Core.Rooms.RoomExitReason.Kicked));
        }

        [UnityTest]
        public IEnumerator PhysicsState_RepublishesWokenObjects_AndRejectsStaleOrHeldUpdates()
        {
            runner = new GameObject("Physics state single runner").AddComponent<NetworkRunner>();
            var start = runner.StartGame(new StartGameArgs { GameMode = GameMode.Single });
            var deadline = Time.realtimeSinceStartup + 30f;
            while (!start.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(start.IsCompleted && start.Result.Ok, Is.True);
            var prefab = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/_Game/Content/Prefabs/MatchSession.prefab");
            var spawned = runner.Spawn(prefab);
            var state = spawned.GetComponent<MatchSessionState>();
            var pose = new Pose(Vector3.up, Quaternion.identity);
            Assert.That(state.TrySetObjectPhysicsPose("box", pose, Vector3.zero, false, 0), Is.False);
            Assert.That(state.TrySetObjectReleased("box", pose, Vector3.zero), Is.True);
            Assert.That(state.TrySetObjectPhysicsPose("box", pose, Vector3.zero, false, 1), Is.True);
            pose.position = Vector3.up * 0.8f;
            Assert.That(state.TrySetObjectPhysicsPose("box", pose, Vector3.down, true, 2), Is.True,
                "A settled object must resume replication when its support is removed.");
            Assert.That(state.TrySetObjectPhysicsPose("box", pose, Vector3.zero, false, 2), Is.False);
            Assert.That(state.TrySetObjectHeld("box", 0), Is.True);
            Assert.That(state.TrySetObjectPhysicsPose("box", pose, Vector3.zero, false, 4), Is.False);
            runner.Despawn(spawned);
        }

        [UnityTest]
        public IEnumerator RemoteBox_FollowsAuthorityWithoutLocalGravity_AndCanBePickedUpAgain()
        {
            var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var hand = new GameObject("Hand");
            try
            {
                var item = root.AddComponent<CarryableItem>();
                var body = root.GetComponent<Rigidbody>();
                item.OnNetworkPose(new Pose(Vector3.up * 5f, Quaternion.identity));
                item.OnNetworkPose(new Pose(Vector3.up * 3f, Quaternion.identity));
                yield return new WaitForSeconds(0.2f);
                Assert.That(body.isKinematic, Is.True);
                Assert.That(body.position.y, Is.EqualTo(3f).Within(0.02f));
                item.OnPickedUp(hand.transform);
                yield return new WaitForSeconds(0.15f);
                Assert.That(item.IsCarried, Is.True);
                Assert.That(root.transform.parent, Is.EqualTo(hand.transform));
                Assert.That(root.transform.localPosition.sqrMagnitude, Is.LessThan(0.001f));
            }
            finally { Object.Destroy(root); Object.Destroy(hand); }
        }

        [UnityTest]
        public IEnumerator RemovingSupport_WakesSleepingUpperBox()
        {
            var support = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var upper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var hand = new GameObject("Hand");
            try
            {
                support.transform.position = Vector3.up * 10f;
                upper.transform.position = Vector3.up * 11f;
                var item = support.AddComponent<CarryableItem>();
                var body = upper.AddComponent<Rigidbody>();
                Physics.SyncTransforms();
                body.Sleep();
                Assert.That(body.IsSleeping(), Is.True);
                item.OnPickedUp(hand.transform);
                Assert.That(body.IsSleeping(), Is.False);
                yield return new WaitForSeconds(0.2f);
                Assert.That(body.position.y, Is.LessThan(10.95f));
            }
            finally { Object.Destroy(support); Object.Destroy(upper); Object.Destroy(hand); }
        }

        [UnityTest]
        public IEnumerator EndingStage_HidesWinnersItem_AndReappliesAfterReplayRestoration()
        {
            runner = new GameObject("Ending single runner").AddComponent<NetworkRunner>();
            runner.gameObject.AddComponent<Photon.Voice.Unity.VoiceConnection>().enabled = false;
            var start = runner.StartGame(new StartGameArgs { GameMode = GameMode.Single });
            var deadline = Time.realtimeSinceStartup + 30f;
            while (!start.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(start.IsCompleted && start.Result.Ok, Is.True);
            var prefab = AssetDatabase.LoadAssetAtPath<NetworkObject>("Assets/_Game/Content/Prefabs/NetworkedPlayer.prefab");
            var spawned = runner.Spawn(prefab, inputAuthority: runner.LocalPlayer);
            var avatar = spawned.GetComponent<Game.Network.Players.PlayerAvatar>();
            var itemRoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var stageRoot = new GameObject("Ending stage");
            using var room = new Game.Core.Lobby.RoomBrowserSystem();
            var network = new Game.Network.Session.NetworkRunnerService(null, room, null, null, null, null);
            using var result = new Game.Bootstrap.NetworkResultLobbyReturnController(network, network, room);
            Game.Bootstrap.EndingStagePresenter presenter = null;
            try
            {
                var item = itemRoot.AddComponent<CarryableItem>();
                Assert.That(spawned.GetComponent<PlayerInteractor>().ApplyConfirmedPickup(item), Is.True);
                room.MatchStarted(new[] { new Game.Core.Match.MatchParticipant(avatar.PlayerId, 0) });
                typeof(Game.Bootstrap.NetworkResultLobbyReturnController).GetProperty("HasMatchResult").SetValue(result, true);
                typeof(Game.Bootstrap.NetworkResultLobbyReturnController).GetProperty("LastWinnerPlayerIndices").SetValue(result, new[] { 0 });
                var stage = stageRoot.AddComponent<Game.Client.Match.EndingStage>();
                var slot = new GameObject("Slot").transform;
                slot.SetParent(stageRoot.transform);
                stage.Wire(null, null, stageRoot.transform, stageRoot.transform, null);
                presenter = new Game.Bootstrap.EndingStagePresenter(result, network, room, stage, new ResultViewStub());
                var hide = typeof(Game.Bootstrap.EndingStagePresenter).GetMethod("HideCarriedItems",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                hide.Invoke(presenter, null);
                Assert.That(itemRoot.GetComponent<Renderer>().forceRenderingOff, Is.True);
                itemRoot.GetComponent<Renderer>().forceRenderingOff = false;
                hide.Invoke(presenter, null);
                Assert.That(itemRoot.GetComponent<Renderer>().forceRenderingOff, Is.True);
                presenter.Dispose(); presenter = null;
                Assert.That(itemRoot.GetComponent<Renderer>().forceRenderingOff, Is.False);
            }
            finally
            {
                presenter?.Dispose();
                Object.Destroy(itemRoot); Object.Destroy(stageRoot);
                runner.Despawn(spawned);
            }
        }

        private sealed class ResultViewStub : Game.Client.Match.IResultView
        {
            public void SetText(string value) { }
            public void SetOutcome(string headline, string subtitle) { }
            public void SetBackdropVisible(bool visible) { }
        }

        [UnityTearDown]
        public IEnumerator ShutdownRunner()
        {
            if (runner == null) yield break;
            var shutdown = runner.Shutdown();
            while (!shutdown.IsCompleted) yield return null;
            if (runner != null) Object.Destroy(runner.gameObject);
            runner = null;
        }
    }
}
#endif
