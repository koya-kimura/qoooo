using UnityEngine;

namespace qoooo.Util
{
    public static class Easing
    {
        public static float EaseInSine(float x)
        {
            return 1f - Mathf.Cos(x * Mathf.PI / 2f);
        }

        public static float EaseOutSine(float x)
        {
            return Mathf.Sin(x * Mathf.PI / 2f);
        }

        public static float EaseInOutSine(float x)
        {
            return -(Mathf.Cos(Mathf.PI * x) - 1f) / 2f;
        }

        public static float EaseInQuad(float x)
        {
            return x * x;
        }

        public static float EaseOutQuad(float x)
        {
            return 1f - (1f - x) * (1f - x);
        }

        public static float EaseInOutQuad(float x)
        {
            var f = -2f * x + 2f;

            return x < 0.5f
                ? 2f * x * x
                : 1f - f * f / 2f;
        }
    }
}