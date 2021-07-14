using System;
using System.Collections.Generic;
using UnityEngine;

namespace LeaderAnimator
{
    [Serializable]
    public struct ColorKeyframe
    {
        public float Time;
        public int Value;
        public Easing Easing;
    }

    public sealed class ColorSequence
    {
        private ColorKeyframe[] keyframes;
        private Color currentColor;

        private float lastTime;
        private int lastIndex;

        public ColorSequence(ColorKeyframe[] keyframes)
        {
            this.keyframes = keyframes;
        }

        public Color GetColor()
            => currentColor;

        public void Update(float time)
        {
            List<Color> theme = GameManager.inst.LiveTheme.objectColors;

            if (keyframes.Length == 1)
            {
                currentColor = theme[keyframes[0].Value];
                return;
            }

            if (time <= keyframes[0].Time)
            {
                currentColor = theme[keyframes[0].Value];
                return;
            }

            if (time >= keyframes[keyframes.Length - 1].Time)
            {
                currentColor = theme[keyframes[keyframes.Length - 1].Value];
                return;
            }

            FindClosestPair(time, out var start, out var end);

            float length = end.Time - start.Time;
            float t = (time - start.Time) / length;

            float easedT = Ease.EaseLookup[end.Easing](t);

            Color startCol = theme[start.Value];
            Color endCol = theme[end.Value];

            currentColor = Color.Lerp(startCol, endCol, easedT);
        }

        private void FindClosestPair(float time, out ColorKeyframe start, out ColorKeyframe end)
        {
            if (time >= lastTime)
            {
                while (time >= keyframes[lastIndex + 1].Time)
                {
                    lastIndex++;
                }
                start = keyframes[lastIndex];
                end = keyframes[lastIndex + 1];
            }
            else
            {
                while (time < keyframes[lastIndex].Time)
                {
                    lastIndex--;
                }
                start = keyframes[lastIndex];
                end = keyframes[lastIndex + 1];
            }
            lastTime = time;
        }
    }
}