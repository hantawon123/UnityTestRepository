using System;
using UnityEngine;

namespace Game.Client
{
    [CreateAssetMenu(menuName = "Game/Character Test Motion Bank")]
    public sealed class CharacterTestMotionBank : ScriptableObject
    {
        public CharacterTestMotion[] motions;
    }

    [Serializable]
    public sealed class CharacterTestMotion
    {
        public string name;
        public float frameRate = 30f;
        public bool loop = true;
        public string[] bones = Array.Empty<string>();
        public Quaternion[] frame0 = Array.Empty<Quaternion>();
        public Quaternion[] rotations = Array.Empty<Quaternion>();
        public int frameCount;
    }
}
