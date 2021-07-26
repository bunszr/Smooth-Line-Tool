using UnityEngine;
using System.Collections.Generic;
using System;

namespace SmoothLineCreation
{
    public class SmoothLine : MonoBehaviour
    {
        int[] ThreeIndies = new int[3]; // Önceki, şimdiki, bir sonraki indexler
        Vector3[] ThreeNodes = new Vector3[3]; // Önceki, şimdiki, bir sonraki düğümler
        const float eps = .01f;
        public Action OnPathChangedEvent;

        public readonly float DefaultControlRadius = 1f;
        const int SplitSegmentCount = 10;

        [Range(0, 40), HideInInspector] public int resolation = 5;
        [HideInInspector] public VisualSetting visualSetting;

        [SerializeField, HideInInspector] List<PathInfo> pathInfos = new List<PathInfo>();
        [SerializeField, HideInInspector] List<Vector3> nodes = new List<Vector3>();
        [SerializeField, HideInInspector] List<Anchor> anchors = new List<Anchor>();
        [SerializeField, HideInInspector] List<Vector3> smoothPoints = new List<Vector3>();

        [HideInInspector] public bool useSnap;
        [HideInInspector] public float snapValue = .5f;

        public List<Vector3> Nodes => nodes;
        public List<Anchor> Anchors => anchors;
        public List<Vector3> SmoothPoints => smoothPoints;
        public int NumNodes => nodes.Count;
        public int LastNodeIndex => nodes.Count - 1;

        [SerializeField, HideInInspector] float totalPathDistance;
        public float TotalPathDistance => totalPathDistance;

        List<Vector3> initialTransportedNodes = new List<Vector3>();
        bool transportWithTransform;
        public bool TransportWithTransform
        {
            get => transportWithTransform;
            set
            {
                transportWithTransform = value;
                initialTransportedNodes.Clear();
                if (value)
                {
                    for (int i = 0; i < nodes.Count; i++)
                    {
                        initialTransportedNodes.Add(transform.InverseTransformPoint(nodes[i]));
                    }
                }
            }
        }

        //SerializeField olmasa runtime'da path tekrar açılır. ÖNEMLİ!!!!!
        [SerializeField, HideInInspector] bool isClose;
        public bool IsClose
        {
            get => isClose;
            set
            {
                isClose = value;
                if (!value)
                {
                    if (anchors.Count >= 3)
                    {
                        anchors[0] = new Anchor();
                        anchors[anchors.Count - 1] = new Anchor();
                    }
                }
            }
        }

        private void Awake()
        {
            UpdatePathInfos();
        }

        float GetTotalPathDistance()
        {
            float totalPathDistance = 0;
            for (int i = 0; i < smoothPoints.Count - 1; i++)
            {
                totalPathDistance += Vector3.Distance(smoothPoints[i + 1], smoothPoints[i]);
            }
            return totalPathDistance;
        }

        // Path üzerinde bir değişiklik yapıldıysa bunun çağrılması gerekir.
        public void UpdateWhenPathChanges()
        {
            ConvertToSmoothCurve();
            UpdatePathInfos();
            if (OnPathChangedEvent != null)
            {
                OnPathChangedEvent();
            }
        }

        void UpdatePathInfos()
        {
            totalPathDistance = GetTotalPathDistance();
            pathInfos.Clear();
            int numPathInfo = smoothPoints.Count / SplitSegmentCount;
            if (numPathInfo <= 2)
            {
                pathInfos.Add(new PathInfo(0, 0, 0));
                pathInfos.Add(new PathInfo(1, TotalPathDistance, smoothPoints.Count - 1));
                return;
            }

            float totalDst = 0;
            int indexOfPathInfo = 0;
            for (int i = 0; i < smoothPoints.Count - 1; i++)
            {
                if (i % SplitSegmentCount == 0)
                {
                    float percent = totalDst / TotalPathDistance;
                    pathInfos.Add(new PathInfo(percent, totalDst, i));
                    indexOfPathInfo++;
                }

                totalDst += Vector3.Distance(smoothPoints[i], smoothPoints[i + 1]);
            }
            pathInfos.Add(new PathInfo(1, TotalPathDistance, smoothPoints.Count - 1));
        }

        public void ConvertToSmoothCurve()
        {
            smoothPoints.Clear();
            Vector3 firstSmoothPoint = IsClose && anchors[0].nodeToAnchorDistance > 1 ? nodes[0] + (nodes[1] - nodes[0]).normalized * anchors[0].EdgeDst : nodes[0];
            smoothPoints.Add(firstSmoothPoint);

            for (int i = 0; i < nodes.Count - 2; i++)
            {
                SetSmoothPoints(i + 1, nodes[i], nodes[i + 1], nodes[i + 2]);
            }

            if (IsClose)
            {
                SetSmoothPoints(LastNodeIndex, nodes[LastNodeIndex - 1], nodes[LastNodeIndex], nodes[0]);
                SetSmoothPoints(0, nodes[LastNodeIndex], nodes[0], nodes[1]);
            }
            else
                smoothPoints.Add(nodes[NumNodes - 1]);
        }

        void SetSmoothPoints(int middleIndex, Vector3 nodeA, Vector3 nodeB, Vector3 nodeC)
        {
            Vector3 dirA = nodeA - nodeB;
            Vector3 dirB = nodeC - nodeB;

            float bisectorAngle;
            float maxNodeToAncDistance = GetPossibleMaxNodeToAncDistance(middleIndex, dirA, dirB, out bisectorAngle);
            float nodeToAncDistance = Mathf.Clamp(anchors[middleIndex].nodeToAnchorDistance, 1, maxNodeToAncDistance);
            Vector3 dirNodeBToAnchor = (dirA.normalized + dirB.normalized).normalized;
            Vector3 center = nodeB + dirNodeBToAnchor * nodeToAncDistance;
            float radius = 0;

            if (anchors[middleIndex].nodeToAnchorDistance > DefaultControlRadius)
            {
                Vector3 closestPointFromDirA = Utility.ClosestPointOnLineSegment(center, nodeA, nodeB);
                Vector3 closestPointFromDirB = Utility.ClosestPointOnLineSegment(center, nodeB, nodeC);

                radius = Vector3.Distance(closestPointFromDirA, center);
                Vector3 axis = Vector3.Cross(dirB, dirA);
                float angle = Vector3.Angle(closestPointFromDirA - center, closestPointFromDirB - center);
                float stepAngle = angle / resolation;
                Vector3 startRotDir = (closestPointFromDirA - center).normalized;

                for (int i = 0; i <= resolation; i++)
                {
                    float currAngle = i * stepAngle;
                    Vector3 rotatedPoint = center + Quaternion.AngleAxis(currAngle, axis) * startRotDir * radius;
                    smoothPoints.Add(rotatedPoint);
                }
            }
            else
            {
                smoothPoints.Add(nodes[middleIndex]);
            }
            anchors[middleIndex] = new Anchor(nodeToAncDistance, center, maxNodeToAncDistance, bisectorAngle, radius);
        }

        // Yarıçap değerini düğümler arasına sıkıştırmak için önceki ve sonraki düz kenar uzunlukları
        float GetPossibleMaxNodeToAncDistance(int index, Vector3 dirA, Vector3 dirB, out float bisectorAngle)
        {
            GetBeforeCurrNextIndies(index);
            float previousEdgeDst = dirA.magnitude * .97f - anchors[ThreeIndies[0]].EdgeDst; // .97f = noktalar birbirinin üzerine binmesin diye
            float nextEdgeDst = dirB.magnitude * .97f - anchors[ThreeIndies[2]].EdgeDst;

            bisectorAngle = Vector3.Angle(dirA, dirA.normalized + dirB.normalized);
            float maxNodeToAncDistance = Mathf.Min(previousEdgeDst, nextEdgeDst) / Mathf.Cos(bisectorAngle * Mathf.Deg2Rad);
            return maxNodeToAncDistance;
        }

        float GetPossibleMaxNodeToAncDistance(int index)
        {
            GetBeforeCurrNextIndies(index);
            Vector3 dirA = nodes[ThreeIndies[0]] - nodes[ThreeIndies[1]];
            Vector3 dirB = nodes[ThreeIndies[2]] - nodes[ThreeIndies[1]];
            float previousEdgeDst = dirA.magnitude * .97f - anchors[ThreeIndies[0]].EdgeDst; // .97f = noktalar birbirinin üzerine binmesin diye
            float nextEdgeDst = dirB.magnitude * .97f - anchors[ThreeIndies[2]].EdgeDst;

            float bisectorAngle = Vector3.Angle(dirA, dirA.normalized + dirB.normalized);
            float maxNodeToAncDistance = Mathf.Min(previousEdgeDst, nextEdgeDst) / Mathf.Cos(bisectorAngle * Mathf.Deg2Rad);
            return maxNodeToAncDistance;
        }

        public int[] GetBeforeCurrNextIndies(int currIndex)
        {
            if (currIndex == 0)
            {
                ThreeIndies[0] = nodes.Count - 1;
                ThreeIndies[1] = currIndex;
                ThreeIndies[2] = currIndex + 1;
            }
            else if (currIndex == nodes.Count - 1)
            {
                ThreeIndies[0] = currIndex - 1;
                ThreeIndies[1] = currIndex;
                ThreeIndies[2] = 0;
            }
            else
            {
                ThreeIndies[0] = currIndex - 1;
                ThreeIndies[1] = currIndex;
                ThreeIndies[2] = currIndex + 1;
            }
            return ThreeIndies;
        }

        public Vector3 GetPerpendicularVector(int currIndex)
        {
            GetBeforeCurrNextIndies(currIndex);
            ThreeNodes[0] = nodes[ThreeIndies[0]];
            ThreeNodes[1] = nodes[ThreeIndies[1]];
            ThreeNodes[2] = nodes[ThreeIndies[2]];
            return Vector3.Cross(ThreeNodes[0] - ThreeNodes[1], ThreeNodes[2] - ThreeNodes[1]);
        }

        public void InitializeSmoothLine(MovingSpace movingSpace)
        {
            TransportWithTransform = false;
            if (nodes.Count == 0)
            {
                nodes.Clear();
                anchors.Clear();
                if (movingSpace == MovingSpace.XY)
                {
                    nodes.Add(Vector3.left * 5);
                    nodes.Add(Vector3.up * 5);
                    nodes.Add(Vector3.right * 5);
                }
                else if (movingSpace == MovingSpace.XZ)
                {
                    nodes.Add(Vector3.left * 5);
                    nodes.Add(Vector3.forward * 5);
                    nodes.Add(Vector3.right * 5);
                }
                anchors.Add(new Anchor());
                anchors.Add(new Anchor());
                anchors.Add(new Anchor());
            }
            ConvertToSmoothCurve();
        }

        public void Reset(MovingSpace movingSpace)
        {
            nodes.Clear();
            InitializeSmoothLine(movingSpace);
        }

        public void AddNode(Vector3 point)
        {
            if (TransportWithTransform)
            {
                Debug.LogError("transportWithTransform değişkenini false yapın");
                return;
            }
            point = useSnap ? point.ToRound(snapValue) : point;
            nodes.Add(point);
            anchors.Add(new Anchor());
        }

        public void InstertPoint(int index, Vector3 point)
        {
            if (TransportWithTransform)
            {
                Debug.LogError("transportWithTransform değişkenini false yapın");
                return;
            }
            point = useSnap ? point.ToRound(snapValue) : point;
            nodes.Insert(index, point);
            anchors.Insert(index, new Anchor());
        }

        public void UpdateNode(int index, Vector3 newPos)
        {
            if (TransportWithTransform)
            {
                //Hareket aracı düğümün üzerindeyse bu mesajı görmezden gelin.
                Debug.LogError("transportWithTransform değişkenini false yapın");
                return;
            }
            nodes[index] = useSnap ? newPos.ToRound(snapValue) : newPos;
        }

        // TransportWithTransform = true ise smoothLine nesnesini hareket, ölçek, rotasyonu ile oynarsanız düğümlerede etki ettirir
        public void UpdateAllNodeWithTransform()
        {
            if (!transportWithTransform)
            {
                Debug.LogError("TransportWithTransform değişkenini true yapın");
                return;
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                nodes[i] = transform.TransformPoint(initialTransportedNodes[i]);
            }
        }

        public void DeleteNode(int index)
        {
            if (NumNodes > 3)
            {
                nodes.RemoveAt(index);
                anchors.RemoveAt(index);
                if (!isClose)
                {
                    anchors[0] = new Anchor();
                    anchors[anchors.Count - 1] = new Anchor();
                }
            }
            else
                Debug.LogError("En az 3 düğüm olmalı");
        }

        public void UpdateRadius(int anchorIndex, float newRadius)
        {
            if (!IsClose && (anchorIndex == 0 || anchorIndex == LastNodeIndex))
            {
                Debug.LogError("Path açık olduğu için ilk ve son indexlerin yarıçaplarını değiştiremezsiniz. (Path açıkda olsa kapalıda olsa Node ve Anchor sayıları birbirine eşittir");
                return;
            }
            float maxRadius = GetPossibleMaxNodeToAncDistance(anchorIndex);
            Anchor tempAnchor = anchors[anchorIndex];
            tempAnchor.nodeToAnchorDistance = Mathf.Clamp(newRadius, 1, maxRadius);
            anchors[anchorIndex] = tempAnchor;
        }

        public Vector3 GetPointAtTime(float time, MoveType moveType = MoveType.Stop)
        {
            time = time.GetNewTimeWithMoveType(moveType);
            return PathUtility.GetPointAtTime(time, pathInfos, smoothPoints, TotalPathDistance, moveType);
        }

        public Vector3 GetPointAtTravelledDistance(float dst, MoveType moveType = MoveType.Stop)
        {
            float t = dst / TotalPathDistance;
            return GetPointAtTime(t, moveType);
        }

        public Vector3 GetDirectionAtTime(float currentTime, Vector3 currentPos, Vector3 transformForward, MoveType moveType = MoveType.Stop)
        {
            currentTime = currentTime.GetNewTimeWithMoveType(moveType);
            Vector3 dir = currentTime == 0 ? GetPointAtTime(eps) - nodes[0]
                        : currentTime == 1 ? smoothPoints[smoothPoints.Count - 1] - GetPointAtTime(1 - eps)
                        : GetPointAtTime(Mathf.Clamp01(currentTime + eps)) - GetPointAtTime(Mathf.Clamp01(currentTime - eps));
            dir.Normalize();
            return dir;
        }

        public Vector3 GetDirectionAtDistanceTravelled(float currentDstTravelled, Vector3 currentPos, Vector3 transformForward, MoveType moveType = MoveType.Stop)
        {
            float t = currentDstTravelled / TotalPathDistance;
            return GetDirectionAtTime(t, currentPos, transformForward, moveType);
        }

        public float GetClosestTimeOnPath(Vector3 worldPoint, MoveType moveType)
        {
            return GetClosestDistanceTravelled(worldPoint, moveType) / TotalPathDistance;
        }

        public float GetClosestDistanceTravelled(Vector3 worldPoint, MoveType moveType)
        {
            return PathUtility.GetClosestDistanceTravelled(worldPoint, pathInfos, smoothPoints, TotalPathDistance);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (UnityEditor.Selection.activeGameObject != gameObject && visualSetting.showPathNotSelected)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < smoothPoints.Count - 1; i++)
                {
                    Gizmos.DrawLine(smoothPoints[i], smoothPoints[i + 1]);
                }

                // Gizmos.color = Color.yellow;
                // for (int i = 0; i < nodes.Count; i++)
                // {
                //     Gizmos.DrawSphere(nodes[i], .25f);
                // }
            }
        }
#endif

        [System.Serializable]
        public struct Anchor
        {
            public float maxNodeToAncDistance;
            public float bisectorAngle;
            public float nodeToAnchorDistance;
            public float radius;
            public Vector3 position;

            public float EdgeDst => Mathf.Cos(bisectorAngle * Mathf.Deg2Rad) * nodeToAnchorDistance;

            public Anchor(float nodeToAnchorDistance, Vector3 position, float maxNodeToAncDistance, float bisectorAngle, float radius)
            {
                this.maxNodeToAncDistance = maxNodeToAncDistance;
                this.bisectorAngle = bisectorAngle;
                this.nodeToAnchorDistance = nodeToAnchorDistance;
                this.position = position;
                this.radius = radius;
            }
        }

        [System.Serializable]
        public class VisualSetting
        {
            public float nodeRadius = .5f;
            public float anchorRadius = .3f;
            public bool showPathNotSelected = true;
        }
    }
}