using UnityEngine;

namespace qoooo.Foundation.Utilities
{
    public static class Rhythm
    {
        public static float GetPhase(
            float t,
            float cycleLength,
            float easeDuration)
        {
            var currentCycle = Mathf.Floor(t / cycleLength);
            var nextCycle = currentCycle + 1f;

            var localTime = t - currentCycle * cycleLength;
            var easeStartTime = cycleLength - easeDuration;

            var easeProgress = Mathf.Clamp01(
                (localTime - easeStartTime) / easeDuration
            );

            return currentCycle +
                   Mathf.Lerp(
                       currentCycle,
                       nextCycle,
                       Easing.EaseInOutSine(easeProgress)
                   );
        }

        public static float GetLerpNoise(
            float t,
            float cycleLength,
            float easeDuration,
            uint seed = 0)
        {
            var currentCycle = Mathf.Floor(t / cycleLength);
            var nextCycle = currentCycle + 1f;

            var localTime = t - currentCycle * cycleLength;
            var easeStartTime = cycleLength - easeDuration;

            var easeProgress = Mathf.Clamp01(
                (localTime - easeStartTime) / easeDuration
            );

            var currentNoise = Pcg.Pcg01(
                new Vector2(seed, currentCycle)
            ).x;

            var nextNoise = Pcg.Pcg01(
                new Vector2(seed, nextCycle)
            ).x;

            return Mathf.Lerp(
                currentNoise,
                nextNoise,
                Easing.EaseInOutSine(easeProgress)
            );
        }

        public static Vector3 GetLerpNoise3D(
            float t,
            float cycleLength,
            float easeDuration,
            uint seed = 0)
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
                    newSeed.X
                ) * 2f - 1f,
                GetLerpNoise(
                    time,
                    cycleLength,
                    easeDuration,
                    newSeed.Y
                ) * 2f - 1f,
                GetLerpNoise(
                    time,
                    cycleLength,
                    easeDuration,
                    newSeed.Z
                ) * 2f - 1f
            );
        }
    }
}