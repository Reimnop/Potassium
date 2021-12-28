using LeaderAnimator;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Potassium
{
    public class LevelObjectRef
    {
        public float StartTime;
        public float KillTime;

        public TextMeshPro TextComponent;
        public Material ObjectMaterial;

        public Transform BaseTransform;
        public Transform VisualTransform;

        public DataManager.GameData.BeatmapObject BeatmapObject;

        public Sequence PositionSequence;
        public Sequence ScaleSequence;
        public Sequence RotationSequence;
        public ColorSequence ColorSequence;

        public List<ParentObjectRef> Parents;
    }
}
