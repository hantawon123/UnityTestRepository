using UnityEngine;
using System;
using Game.Core.Match;

namespace Game.SOAP.Events
{
    [CreateAssetMenu(
        fileName = "MatchPhaseEvent",
        menuName = "Game/Events/Match Phase Event")]
    public sealed class MatchPhaseEventSO : ScriptableObject
    {
        public event Action<MatchPhase> Raised;
        public void Raise(MatchPhase phase)
        {
            Raised?.Invoke(phase);
        }
        private void OnDisable()
        {
            Raised = null;
        }
    }
}