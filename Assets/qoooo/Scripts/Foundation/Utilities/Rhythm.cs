using UnityEngine;

namespace qoooo.Foundation.Utilities
{
    public delegate float EasingFn(float x);

    // Maps caller-supplied timing into visual indices, phases, and noise values.
    // It owns no clock state and never advances musical time.
    public static class Rhythm
    {
        public static int GetIndex(float t, float interval)
        {
            return Mathf.FloorToInt(Mathf.Max(0f, t) / Mathf.Max(1f, interval));
        }

        public static float GetPhase(
            float t,
            float cycleLength,
            float easeDuration,
            EasingFn easingFn = null)
        {
            var currentCycle = Mathf.Floor(t / cycleLength);
            var nextCycle = currentCycle + 1f;

            var localTime = t - currentCycle * cycleLength;
            var easeStartTime = cycleLength - easeDuration;

            var easeProgress = Mathf.Clamp01(
                (localTime - easeStartTime) / easeDuration
            );
            var easing = easingFn ?? Easing.EaseInOutSine;

            return currentCycle +
                   Mathf.Lerp(
                       currentCycle,
                       nextCycle,
                       easing(easeProgress)
                   );
        }

        public static float GetLerpNoise(
            float t,
            float cycleLength,
            float easeDuration,
            uint seed = 0,
            EasingFn easingFn = null)
        {
            var currentCycle = Mathf.Floor(t / cycleLength);
            var nextCycle = currentCycle + 1f;

            var localTime = t - currentCycle * cycleLength;
            var easeStartTime = cycleLength - easeDuration;

            var easeProgress = Mathf.Clamp01(
                (localTime - easeStartTime) / easeDuration
            );
            var easing = easingFn ?? Easing.EaseInOutSine;

            var currentNoise = Pcg.Pcg01(
                new Vector2(seed, currentCycle)
            ).x;

            var nextNoise = Pcg.Pcg01(
                new Vector2(seed, nextCycle)
            ).x;

            return Mathf.Lerp(
                currentNoise,
                nextNoise,
                easing(easeProgress)
            );
        }

        public static Vector3 GetLerpNoise3D(
            float t,
            float cycleLength,
            float easeDuration,
            uint seed = 0,
            EasingFn easingFn = null)
        {
            var newSeed = Pcg.Hash4D(
                new UInt4(seed, seed, seed, seed)
            );

            var time =
                t +
                Pcg.Pcg01(newSeed.W) * cycleLength;

            return new Vector3(
                GetLerpNoise(
                    time,
                    cycleLength,
                    easeDuration,
                    newSeed.X,
                    easingFn
                ) * 2f - 1f,
                GetLerpNoise(
                    time,
                    cycleLength,
                    easeDuration,
                    newSeed.Y,
                    easingFn
                ) * 2f - 1f,
                GetLerpNoise(
                    time,
                    cycleLength,
                    easeDuration,
                    newSeed.Z,
                    easingFn
                ) * 2f - 1f
            );
        }
    }
}