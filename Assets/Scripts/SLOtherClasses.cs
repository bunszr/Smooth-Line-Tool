using UnityEngine;

namespace SmoothLineTool
{
    public static class SLMouseUtility
    {
        public static Vector3 GetMousePosWithMoveSpace(Ray ray, Vector2 guiMousePos, SLMovingSpace pathSpace, float planeHeight)
        {
            float dst = 0;
            if (pathSpace == SLMovingSpace.XY)
            {
                dst = (planeHeight - ray.origin.z) / ray.direction.z;
                return ray.origin + ray.direction * dst;
            }
            dst = (planeHeight - ray.origin.y) / ray.direction.y;
            return ray.origin + ray.direction * dst;
        }
    }


    public static class SLUtility
    {
        public static Vector3 ToRound(this Vector3 v3, float mul)
        {
            return new Vector3(RoundTo(v3.x, mul), RoundTo(v3.y, mul), RoundTo(v3.z, mul));
        }

        public static float RoundTo(this float value, float mul = 1)
        {
            return Mathf.Round(value / mul) * mul;
        }

        // Sebastian Lague'e aittir.
        public static Vector3 ClosestPointOnLineSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 aB = b - a;
            Vector3 aP = p - a;
            float sqrLenAB = aB.sqrMagnitude;

            if (sqrLenAB == 0)
                return a;

            float t = Mathf.Clamp01(Vector3.Dot(aP, aB) / sqrLenAB);
            return a + aB * t;
        }

        // Sebastian Lague'e aittir.
        public static Vector2 ClosestPointOnLineSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 aB = b - a;
            Vector2 aP = p - a;
            float sqrLenAB = aB.sqrMagnitude;

            if (sqrLenAB == 0)
                return a;

            float t = Mathf.Clamp01(Vector2.Dot(aP, aB) / sqrLenAB);
            return a + aB * t;
        }
    }


    [System.Serializable]
    public struct SLPathInfo
    {
        public float percent;
        public float distance;
        public int smoothIndex;

        public SLPathInfo(float percent, float distance, int smoothIndex)
        {
            this.percent = percent;
            this.distance = distance;
            this.smoothIndex = smoothIndex;
        }
    }

    public enum SLMovingSpace { XY = 0, XZ = 1 }
    public enum SLMoveType { Stop, Loop, Reverse, None }
}