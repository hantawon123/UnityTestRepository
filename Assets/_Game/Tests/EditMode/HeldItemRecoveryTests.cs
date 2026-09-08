using System.Collections.Generic;
using System.Reflection;
using Game.Bootstrap;
using Game.Client.Interactions;
using Game.Core.Lobby;
using Game.Network.Match;
using Game.Network.Session;
using NUnit.Framework;
using UnityEngine;

namespace Game.Architecture.Tests
{
    public sealed class HeldItemRecoveryTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [TestCase(-1)]
        [TestCase(1)]
        public void UnchangedAuthorityVersion_RepairsWrongLocalHolder(int authoritativeHolder)
        {
            using var room = new RoomBrowserSystem();
            var network = new NetworkRunnerService(null, null, null, null, null, null);
            var itemObject = new GameObject("RecoveryItem", typeof(Rigidbody), typeof(BoxCollider));
            var player0 = new GameObject("WrongHolder");
            var player1 = new GameObject("CorrectHolder");
            try
            {
                var item = itemObject.AddComponent<CarryableItem>();
                typeof(CarryableItem).GetMethod("Awake", Private).Invoke(item, null);
                var wrong = player0.AddComponent<PlayerInteractor>();
                var correct = player1.AddComponent<PlayerInteractor>();
                typeof(PlayerInteractor).GetField("holdPoint", Private).SetValue(wrong, player0.transform);
                typeof(PlayerInteractor).GetField("holdPoint", Private).SetValue(correct, player1.transform);
                wrong.ApplyConfirmedPickup(item);

                var bridge = new NetworkInteractionSceneBridge(network, room, false, itemObject.scene);
                Field<Dictionary<string, CarryableItem>>(bridge, "items").Add(item.ObjectId, item);
                Field<Dictionary<int, PlayerInteractor>>(bridge, "interactors").Add(0, wrong);
                Field<Dictionary<int, PlayerInteractor>>(bridge, "interactors").Add(1, correct);
                Field<Dictionary<string, int>>(bridge, "appliedVersions").Add(item.ObjectId, 5);
                var pose = new Pose(new Vector3(2f, 1f, 3f), Quaternion.identity);
                typeof(NetworkInteractionSceneBridge).GetField("objectStates", Private).SetValue(bridge,
                    new[] { new MatchObjectStateSnapshot(item.ObjectId, authoritativeHolder, pose, default, false, 5) });
                typeof(NetworkInteractionSceneBridge).GetMethod("ApplyObjectStates", Private).Invoke(bridge, null);

                Assert.That(wrong.CarriedItem, Is.Null);
                Assert.That(item.IsCarried, Is.EqualTo(authoritativeHolder == 1));
                if (authoritativeHolder == 1) Assert.That(correct.CarriedItem, Is.SameAs(item));
                else Assert.That(item.transform.position, Is.EqualTo(pose.position));
            }
            finally
            {
                Object.DestroyImmediate(itemObject);
                Object.DestroyImmediate(player0);
                Object.DestroyImmediate(player1);
            }
        }

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, Private).GetValue(owner);
    }
}
