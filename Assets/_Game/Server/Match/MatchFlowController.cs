using Game.Core.Match;
using Game.SOAP.Config;
using UnityEngine;

namespace Game.Server.Match
{
    public sealed class MatchFlowController : MonoBehaviour
    {
        [SerializeField] private MatchRulesSO rules;
        [SerializeField] private MatchPhase currentPhase = MatchPhase.Waiting;
        [SerializeField] private float remainingTime;

        public MatchPhase CurrentPhase => currentPhase;
        public float RemainingTime => remainingTime;

        [ContextMenu("Start Match")]
        public void StartMatch()
        {
            if(rules == null || currentPhase != MatchPhase.Waiting) { return; }

            EnterPhase(MatchPhase.Hiding);
        }

        private void Update()
        {
            if(currentPhase != MatchPhase.Hiding &&
                currentPhase != MatchPhase.Searching) 
            { 
                return; 
            }

            remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);

            if(remainingTime > 0f)
            {
                return;
            }

            EnterPhase(currentPhase == MatchPhase.Hiding
                ? MatchPhase.Searching
                : MatchPhase.Result);
        }

        private void EnterPhase(MatchPhase nextPhase)
        {
            currentPhase = nextPhase;

            remainingTime = nextPhase switch
            {
                MatchPhase.Hiding => rules.HidingDuration,
                MatchPhase.Searching => rules.SearchingDuration,
                _ => 0f
            };
        }
    }
}
