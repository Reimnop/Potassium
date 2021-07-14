using System;

namespace LeaderAnimator
{
    [Serializable]
    public struct Keyframe
    {
        public float Time;
        public float[] Values;
        public Easing Easing;
    }

    public sealed class Sequence
    {
        private Keyframe[] keyframes;
        private float[] currentValues;

        private int count;

        private float lastTime;
        private int lastIndex;

        public Sequence(Keyframe[] keyframes, int count)
        {
            this.keyframes = keyframes;
            this.count = count;

            currentValues = new float[count];
        }

        public float[] GetValues()
            => currentValues;

        public void Update(float time)
        {
            if (keyframes.Length == 1)
            {
                DeepCopyArray(keyframes[0].Values);
                return;
            }

            if (time <= keyframes[0].Time)
            {
                DeepCopyArray(keyframes[0].Values);
                return;
            }

            if (time >= keyframes[keyframes.Length - 1].Time)
            {
                DeepCopyArray(keyframes[keyframes.Length - 1].Values);
                return;
            }

            FindClosestPair(time, out var start, out var end);

            float length = end.Time - start.Time;
            float t = (time - start.Time) / length;

            float easedT = Ease.EaseLookup[end.Easing](t);

            for (int i = 0; i < count; i++)
            {
                currentValues[i] = Lerp(start.Values[i], end.Values[i], easedT);
            }
        }

        private float Lerp(float x, float y, float t)
        {
            return x * (1 - t) + y * t;
        }

        private void DeepCopyArray(float[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
                currentValues[i] = arr[i];
        }

        private void FindClosestPair(float time, out Keyframe start, out Keyframe end)
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