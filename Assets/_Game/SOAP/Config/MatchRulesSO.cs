using UnityEngine;

namespace Game.SOAP.Config
{
    [CreateAssetMenu(
        fileName = "MatchRules",
        menuName = "Game/Match Rules")]
    public sealed class MatchRulesSO : ScriptableObject
    {
        [SerializeField, Min(1f)]
        private float hidingDuration = 180f;

        [SerializeField, Min(1f)]
        private float searchingDuration = 300f;

        public float HidingDuration => hidingDuration;
        public float SearchingDuration => searchingDuration;
    }
}