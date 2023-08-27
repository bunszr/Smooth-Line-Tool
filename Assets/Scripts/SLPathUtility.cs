using UnityEngine;
using System.Collections.Generic;

namespace SmoothLineTool
{
    public static class SLPathUtility
    {
        static SLPathInfo[] InterpolatingPathInfos = new SLPathInfo[2];

        public delegate float OnPathInfoVariableDelegate(SLPathInfo pathInfo);
        static OnPathInfoVariableDelegate GetPercentDelegate = x => x.percent;
        static OnPathInfoVariableDelegate GetDistanceDelegate = x => x.distance;
        static OnPathInfoVariableDelegate GetSmoothIndexDelegate = x => x.smoothIndex;

        public static SLPathInfo[] GetTwoPathInfoInRange(List<SLPathInfo> pathInfos, float value, OnPathInfoVariableDelegate pathInfoDelegateA, OnPathInfoVariableDelegate pathInfoDelegateB)
        {
            int left = 0;
            int right = pathInfos.Count - 1;
            int mid = right / 2;

            while (Mathf.Abs(left - right) > 1)
            {
                if (pathInfoDelegateA(pathInfos[left]) <= value && value <= pathInfoDelegateA(pathInfos[mid]))
                {
                    right = mid;
                    mid = (left + right) / 2;
                }
                else
                {
                    left = mid;
                    mid = (left + right) / 2;
                }
            }
            InterpolatingPathInfos[0] = pathInfos[left];
            InterpolatingPathInfos[1] = pathInfos[right];
            return InterpolatingPathInfos;
        }

        public static float GetNewTimeWithMoveType(this float time, SLMoveType moveType)
        {
            switch (moveType)
            {
                case SLMoveType.Loop:
                    time = time % 1;
                    break;
                case SLMoveType.Reverse:
                    time = Mathf.PingPong(time, 1);
                    break;
                case SLMoveType.Stop:
                    time = Mathf.Clamp01(time);
                    break;
            }
            return time;
        }

        public static void SetNewTimeForChangeablePath(ref float value, float length, ref int dir, SLMoveType moveType)
        {
            if (moveType == SLMoveType.Stop && value >= length)
                return;
            else if (moveType == SLMoveType.Reverse)
            {
                if (value >= length)
                    dir = -1;
                else if (value < 0)
                    dir = 1;
            }
        }

        public static Vector3 GetPointAtTime(float time, List<SLPathInfo> pathInfos, List<Vector3> smoothPoints, float totalPathDistance, SLMoveType moveType)
        {
            SLPathUtility.GetTwoPathInfoInRange(pathInfos, time, GetPercentDelegate, GetPercentDelegate);
            float totalDst = InterpolatingPathInfos[0].distance;
            float reachedDst = totalPathDistance * time;

            for (int i = InterpolatingPathInfos[0].smoothIndex; i < InterpolatingPathInfos[1].smoothIndex; i++)
            {
                float oldTotalDst = totalDst;
                totalDst += Vector3.Distance(smoothPoints[i], smoothPoints[i + 1]);
                if (totalDst >= reachedDst)
                {
                    float dirPercent = Mathf.InverseLerp(oldTotalDst, totalDst, reachedDst);
                    return Vector3.Lerp(smoothPoints[i], smoothPoints[i + 1], dirPercent);
                }
            }
            return Vector3.zero;
        }

        public static float GetClosestDistanceTravelled(Vector3 worldPoint, List<SLPathInfo> pathInfos, List<Vector3> smoothPoints, float totalPathDst)
        {
            float closestDst = Mathf.Infinity;
            int bestIndex = -1;
            Vector3 closestPointOnLineSegment = Vector3.zero;
            for (int i = 0; i < smoothPoints.Count - 1; i++)
            {
                Vector3 pointOnLineSegment = SLUtility.ClosestPointOnLineSegment(worldPoint, smoothPoints[i], smoothPoints[i + 1]);
                float sqrDstFromClosestPointOnLineSegmentToWorldPoint = (worldPoint - pointOnLineSegment).sqrMagnitude;
                if (sqrDstFromClosestPointOnLineSegmentToWorldPoint < closestDst)
                {
                    closestDst = sqrDstFromClosestPointOnLineSegmentToWorldPoint;
                    closestPointOnLineSegment = pointOnLineSegment;
                    bestIndex = i;
                }
            }

            GetTwoPathInfoInRange(pathInfos, bestIndex, GetSmoothIndexDelegate, GetSmoothIndexDelegate);
            float totalDst = InterpolatingPathInfos[0].distance;
            for (int i = InterpolatingPathInfos[0].smoothIndex; i <= InterpolatingPathInfos[1].smoothIndex; i++)
            {
                if (i == bestIndex)
                {
                    totalDst += Vector3.Distance(smoothPoints[bestIndex], closestPointOnLineSegment);
                    break;
                }
                totalDst += Vector3.Distance(smoothPoints[i], smoothPoints[i + 1]);
            }
            return totalDst;
        }
    }
}