using System;
using System.Runtime.InteropServices;

namespace LeaderAnimator
{
    [Serializable]
    public unsafe struct Keyframe
    {
        public float Time;
        public int ElementCount;
        public fixed float Values[16];
        public Easing Easing;

        public Keyframe DeepCopy()
        {
            Keyframe kf = new Keyframe();

            kf.Time = Time;
            kf.Easing = Easing;
            for (int i = 0; i < ElementCount; i++)
            {
                kf.Values[i] = Values[i];
            }

            return kf;
        }
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

        public unsafe void Update(float time)
        {
            if (keyframes.Length == 1)
            {
                for (int i = 0; i < keyframes[0].ElementCount; i++)
                {
                    currentValues[i] = keyframes[0].Values[i];
                }
                return;
            }

            if (time <= keyframes[0].Time)
            {
                for (int i = 0; i < keyframes[0].ElementCount; i++)
                {
                    currentValues[i] = keyframes[0].Values[i];
                }
                return;
            }

            if (time >= keyframes[keyframes.Length - 1].Time)
            {
                for (int i = 0; i < keyframes[keyframes.Length - 1].ElementCount; i++)
                {
                    currentValues[i] = keyframes[0].Values[i];
                }
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

        public Sequence DeepCopy()
        {
            Keyframe[] newKeyframes = new Keyframe[keyframes.Length];

            for (int i = 0; i < keyframes.Length; i++)
            {
                newKeyframes[i] = keyframes[i].DeepCopy();
            }

            return new Sequence(newKeyframes, count);
        }

        private float Lerp(float x, float y, float t)
        {
            return x * (1 - t) + y * t;
        }

        private void FindClosestPair(float time, out Keyframe start, out Keyframe end)
        {
            start = default;
            end = default;

            int index = lastIndex;

            if (keyframes[index].Time <= time && keyframes[index + 1].Time > time)
            {
                start = keyframes[index];
                end = keyframes[index + 1];
            }
            else
            {
                if (time >= lastTime) //forward
                {
                    for (int i = index + 1; i < keyframes.Length - 1; i++)
                    {
                        if (keyframes[i + 1].Time > time)
                        {
                            index = i;
                            start = keyframes[i];
                            end = keyframes[i + 1];
                            break;
                        }
                    }
                }
                else //reverse
                {
                    for (int i = index - 1; i >= 0; i--)
                    {
                        if (keyframes[i].Time <= time)
                        {
                            index = i;
                            start = keyframes[i];
                            end = keyframes[i + 1];
                            break;
                        }
                    }
                }
            }

            lastTime = time;
            lastIndex = index;
        }
    }
}