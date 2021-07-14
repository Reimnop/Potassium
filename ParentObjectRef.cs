using LeaderAnimator;
using UnityEngine;

namespace Potassium
{
    public struct ParentObjectRef
    {
        public Transform ObjectTransform;

        public float StartTime;
        public float KillTime;

        public float LocalDepth;

        public Sequence PositionSequence;
        public Sequence ScaleSequence;
        public Sequence RotationSequence;
    }
}
