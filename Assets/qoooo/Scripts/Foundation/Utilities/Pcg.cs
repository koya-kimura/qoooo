using System;
using UnityEngine;

namespace qoooo.Foundation.Utilities
{
    public readonly struct UInt2
    {
        public readonly uint X;
        public readonly uint Y;

        public UInt2(uint x, uint y)
        {
            X = x;
            Y = y;
        }
    }

    public readonly struct UInt3
    {
        public readonly uint X;
        public readonly uint Y;
        public readonly uint Z;

        public UInt3(uint x, uint y, uint z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }

    public readonly struct UInt4
    {
        public readonly uint X;
        public readonly uint Y;
        public readonly uint Z;
        public readonly uint W;

        public UInt4(uint x, uint y, uint z, uint w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
    }

    public static class Pcg
    {
        private const float UIntMax = 4294967295f;

        public static uint Hash(uint v)
        {
            unchecked
            {
                var state =
                    v * 747796405u +
                    2891336453u;

                var word =
                    ((state >> (int)((state >> 28) + 4)) ^ state)
                    * 277803737u;

                return (word >> 22) ^ word;
            }
        }

        public static UInt2 Hash2D(UInt2 v)
        {
            unchecked
            {
                var x = v.X * 1664525u + 1013904223u;
                var y = v.Y * 1664525u + 1013904223u;

                x += y * 1664525u;
                y += x * 1664525u;

                x ^= x >> 16;
                y ^= y >> 16;

                x += y * 1664525u;
                y += x * 1664525u;

                x ^= x >> 16;
                y ^= y >> 16;

                return new UInt2(x, y);
            }
        }

        public static UInt3 Hash3D(UInt3 v)
        {
            unchecked
            {
                var x = v.X * 1664525u + 1013904223u;
                var y = v.Y * 1664525u + 1013904223u;
                var z = v.Z * 1664525u + 1013904223u;

                x += y * z;
                y += z * x;
                z += x * y;

                x ^= x >> 16;
                y ^= y >> 16;
                z ^= z >> 16;

                x += y * z;
                y += z * x;
                z += x * y;

                return new UInt3(x, y, z);
            }
        }

        public static UInt4 Hash4D(UInt4 v)
        {
            unchecked
            {
                var x = v.X * 1664525u + 1013904223u;
                var y = v.Y * 1664525u + 1013904223u;
                var z = v.Z * 1664525u + 1013904223u;
                var w = v.W * 1664525u + 1013904223u;

                x += y * w;
                y += z * x;
                z += x * y;
                w += y * z;

                x ^= x >> 16;
                y ^= y >> 16;
                z ^= z >> 16;
                w ^= w >> 16;

                x += y * w;
                y += z * x;
                z += x * y;
                w += y * z;

                return new UInt4(x, y, z, w);
            }
        }

        // --------------------------------------------------
        // uint -> 0..1
        // --------------------------------------------------

        public static float Pcg01(uint v)
        {
            return Hash(v) / UIntMax;
        }

        public static Vector2 Pcg01(UInt2 v)
        {
            var result = Hash2D(v);

            return new Vector2(
                result.X / UIntMax,
                result.Y / UIntMax
            );
        }

        public static Vector3 Pcg01(UInt3 v)
        {
            var result = Hash3D(v);

            return new Vector3(
                result.X / UIntMax,
                result.Y / UIntMax,
                result.Z / UIntMax
            );
        }

        public static Vector4 Pcg01(UInt4 v)
        {
            var result = Hash4D(v);

            return new Vector4(
                result.X / UIntMax,
                result.Y / UIntMax,
                result.Z / UIntMax,
                result.W / UIntMax
            );
        }

        // --------------------------------------------------
        // float -> asuint -> PCG
        // --------------------------------------------------

        public static float Pcg01(float v)
        {
            return Pcg01(AsUInt(v));
        }

        public static Vector2 Pcg01(Vector2 v)
        {
            return Pcg01(new UInt2(
                AsUInt(v.x),
                AsUInt(v.y)
            ));
        }

        public static Vector3 Pcg01(Vector3 v)
        {
            return Pcg01(new UInt3(
                AsUInt(v.x),
                AsUInt(v.y),
                AsUInt(v.z)
            ));
        }

        public static Vector4 Pcg01(Vector4 v)
        {
            return Pcg01(new UInt4(
                AsUInt(v.x),
                AsUInt(v.y),
                AsUInt(v.z),
                AsUInt(v.w)
            ));
        }

        // --------------------------------------------------
        // HLSL asuint(float) 相当
        // --------------------------------------------------

        private static uint AsUInt(float value)
        {
            return unchecked(
                (uint)BitConverter.SingleToInt32Bits(value)
            );
        }
    }
}