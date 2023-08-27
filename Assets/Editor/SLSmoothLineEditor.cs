using UnityEngine;
using UnityEditor;
using SmoothLineTool;

namespace SmoothLineEditorNamespace
{
    [CustomEditor(typeof(SLSmoothLine))]
    public class SLSmoothLineEditor : Editor
    {
        const float ClosestSqrDstToSelection = .15f;
        static readonly string[] MovingSpaceName = { "XY", "XZ" };
        const string helpInfo = "Ctrl+Sol Tık = Düğümü Sil\nShift+Sol Tık = Yeni düğüm ekle\nSpace = Hareket uzayını değiştir";

        SLSmoothLine smoothLine;
        Info info;

        bool isRepaint = false;
        bool isRepaintInspector = false;
        Vector3 pressedPoint;
        bool transportWithTransformInEditor = false;
        bool isCloseInEditor;
        bool openVisualSettingPanel = false;
        int lastSelectedNodeIndex;
        int lastSelectedRadiusIndex;
        float lastSelectedRadius; // Düğümden anchor'a olan uzaklık aslında Radius değil yani
        float lastSelectedOldRadius;
        double lastClickTime;
        bool isDoubleClickNode;

        public delegate Vector3 OnPosDelegate(int i, SLSmoothLine smooth);
        static OnPosDelegate OnPosNodeDelegate = (i, smooth) => smooth.Nodes[i];
        static OnPosDelegate OnPosAnchorDelegate = (i, smooth) => smooth.Anchors[i].position;

        void OnEnable()
        {
            info = new Info();
            smoothLine = (SLSmoothLine)target;
            smoothLine.InitializeSmoothLine();
            isCloseInEditor = smoothLine.IsClose;
        }

        private void OnDisable()
        {
            smoothLine.UpdateWhenPathChanges();
            if (smoothLine != null)
            {
                EditorUtility.SetDirty(smoothLine);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(smoothLine.gameObject.scene);
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.HelpBox(helpInfo, MessageType.Info);

            DrawInspectorForTransportWithTransformAndIsClose();

            if (!transportWithTransformInEditor)
            {
                smoothLine.resolation = EditorGUILayout.IntSlider("Resolation", smoothLine.resolation, 1, 40);
                smoothLine.movingSpace = (SLMovingSpace)EditorGUILayout.Popup("Moving Space", (int)smoothLine.movingSpace, MovingSpaceName);

                smoothLine.useSnap = EditorGUILayout.Toggle("Use Snap", smoothLine.useSnap);
                if (smoothLine.useSnap)
                {
                    smoothLine.snapValue = EditorGUILayout.FloatField("Snap Value", smoothLine.snapValue);
                }

                openVisualSettingPanel = EditorGUILayout.Foldout(openVisualSettingPanel, "Visual Settings");
                if (openVisualSettingPanel)
                {
                    smoothLine.visualSetting.nodeRadius = EditorGUILayout.FloatField("Node Radius", smoothLine.visualSetting.nodeRadius);
                    smoothLine.visualSetting.anchorRadius = EditorGUILayout.FloatField("Anchor Radius", smoothLine.visualSetting.anchorRadius);
                    smoothLine.visualSetting.showPathNotSelected = EditorGUILayout.Toggle("Show Path Not Selected", smoothLine.visualSetting.showPathNotSelected);
                }

                DrawInspectorForRadius();

                if (GUILayout.Button("Reset"))
                {
                    Undo.RecordObject(smoothLine, "Reset Smooth Line");
                    smoothLine.Reset();
                    SceneView.RepaintAll();
                }

                smoothLine.allAnchorRadius = EditorGUILayout.FloatField("All Anchor Radius", smoothLine.allAnchorRadius);
                if (GUILayout.Button("Updated All Radius"))
                {
                    Undo.RecordObject(smoothLine, "Updated All Radius Smooth Line");
                    for (int i = 1; i < smoothLine.Anchors.Count - 1; i++)
                    {
                        smoothLine.UpdateRadius(i, smoothLine.allAnchorRadius);
                    }
                    SceneView.RepaintAll();
                }
            }

            if (isDoubleClickNode && smoothLine.transform.hasChanged)
            {
                smoothLine.UpdateNode(lastSelectedNodeIndex, smoothLine.transform.position);
                GUI.changed = true;
            }

            if (GUI.changed)
            {
                smoothLine.ConvertToSmoothCurve();
                SceneView.RepaintAll();
            }
        }

        void DrawInspectorForTransportWithTransformAndIsClose()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                transportWithTransformInEditor = EditorGUILayout.Toggle("Transport With Transform", transportWithTransformInEditor);
                isCloseInEditor = EditorGUILayout.Toggle("Is Close", isCloseInEditor);
                if (check.changed)
                {
                    smoothLine.IsClose = isCloseInEditor;
                    smoothLine.TransportWithTransform = transportWithTransformInEditor;
                }
            }

            if (transportWithTransformInEditor && smoothLine.transform.hasChanged)
            {
                Undo.RecordObject(smoothLine, "UpdateAllNodeWithTransform");
                smoothLine.UpdateAllNodeWithTransform();
                smoothLine.ConvertToSmoothCurve();
            }
        }

        void DrawInspectorForRadius()
        {
            // Inspectordan radius'u ayarlamak için (Gerçek radius değeri değildir)
            lastSelectedRadius = EditorGUILayout.FloatField("Radius Last Selected", lastSelectedRadius.RoundTo(.1f));
            if (lastSelectedOldRadius != lastSelectedRadius)
            {
                smoothLine.UpdateRadius(lastSelectedRadiusIndex, lastSelectedRadius);
                lastSelectedOldRadius = lastSelectedRadius;
            }
        }

        void OnSceneGUI()
        {
            Event guiEvent = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));


            if (guiEvent.type == EventType.Repaint)
            {
                Draw();
            }
            else
            {
                HandleInput(guiEvent);
                if (isRepaint)
                {
                    smoothLine.ConvertToSmoothCurve();
                    HandleUtility.Repaint();
                    isRepaint = false;
                }

                if (isRepaintInspector)
                {
                    Repaint();
                    isRepaintInspector = false;
                }
            }
        }

        void Draw()
        {
            //Düğümün hareket etme düzlemi
            if (info.selectedNodeIndex != -1 && !isDoubleClickNode)
            {
                Vector3 size = smoothLine.movingSpace == SLMovingSpace.XY ? new Vector3(5, 5, 0) : new Vector3(5, 0, 5);
                Vector3 stepSize = size / 10;
                for (int i = 1; i <= 10; i++)
                    Handles.DrawWireCube(pressedPoint, i * stepSize);
            }

            for (int i = 0; i < smoothLine.Nodes.Count; i++)
            {
                // Düğüm görselleri
                Handles.color = info.mouseOverNodeIndex == i ? Color.red : Color.white;
                // if (!isDoubleClickNode || lastSelectedNodeIndex != i)
                Handles.SphereHandleCap(i, smoothLine.Nodes[i], Quaternion.identity, smoothLine.visualSetting.nodeRadius, EventType.Repaint);

                // Düğümden düğüme siyah çizgiler
                Handles.color = Color.black;
                if (smoothLine.IsClose || i != smoothLine.NumNodes - 1)
                {
                    Handles.DrawLine(smoothLine.Nodes[i], smoothLine.Nodes[(i + 1) % (smoothLine.NumNodes)]);
                }

                // Anchor görselleri
                if (smoothLine.IsClose)
                {
                    Handles.color = info.mouseOverAnchorIndex == i ? Color.red : Color.black;
                    Handles.DrawLine(smoothLine.Nodes[i], smoothLine.Anchors[i].position);
                    Handles.SphereHandleCap(i, smoothLine.Anchors[i].position, Quaternion.identity, smoothLine.visualSetting.anchorRadius, EventType.Repaint);
                }
                else
                {
                    if (i > 0 && i < smoothLine.NumNodes - 1)
                    {
                        Handles.color = info.mouseOverAnchorIndex == i ? Color.red : Color.black;
                        Handles.DrawLine(smoothLine.Nodes[i], smoothLine.Anchors[i].position);
                        Handles.SphereHandleCap(i, smoothLine.Anchors[i].position, Quaternion.identity, smoothLine.visualSetting.anchorRadius, EventType.Repaint);
                    }
                }
            }

            // Smooth noktaları çiziyoruz
            Handles.color = Color.green;
            for (int i = 0; i < smoothLine.SmoothPoints.Count - 1; i++)
            {
                Handles.DrawLine(smoothLine.SmoothPoints[i], smoothLine.SmoothPoints[i + 1]);
            }

            Handles.color = Color.red;
            Handles.DrawLine(smoothLine.Nodes[info.nodeSegmentIndies[0]], smoothLine.Nodes[info.nodeSegmentIndies[1]]);
        }

        void HandleInput(Event guiEvent)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            float planeHeight = smoothLine.movingSpace == SLMovingSpace.XY ? smoothLine.Nodes[smoothLine.LastNodeIndex].z : smoothLine.Nodes[smoothLine.LastNodeIndex].y;
            Vector3 mousePos = SLMouseUtility.GetMousePosWithMoveSpace(ray, guiEvent.mousePosition, smoothLine.movingSpace, planeHeight);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Shift)
            {
                ShiftLeftMouseDown(mousePos, ray);
            }
            else if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.modifiers == EventModifiers.Control)
            {
                CtrlLeftMouseDown(mousePos);
            }
            else if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
            {
                LeftMouseDown(mousePos);
            }
            else if (guiEvent.type == EventType.MouseDrag && guiEvent.button == 0)
            {
                LeftMouseDrag(mousePos, ray);
            }
            else if (guiEvent.type == EventType.MouseUp && guiEvent.button == 0)
            {
                LeftMouseUp();
            }
            else if (guiEvent.type == EventType.MouseMove)
            {
                MouseMove(ray, mousePos);
            }
            else if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.Space)
            {
                ChangeSpace();
                if (info.selectedNodeIndex != -1)
                {
                    pressedPoint = smoothLine.Nodes[info.selectedNodeIndex];
                }
                Repaint();
            }
        }

        void ShiftLeftMouseDown(Vector3 mousePos, Ray ray)
        {
            Undo.RecordObject(smoothLine, "Add Point");
            if (info.segmentPercent != 0)
            {
                Vector3 addingPoint = Vector3.Lerp(smoothLine.Nodes[info.nodeSegmentIndies[0]], smoothLine.Nodes[info.nodeSegmentIndies[1]], info.segmentPercent);
                smoothLine.InstertPoint(info.nodeSegmentIndies[1], addingPoint);
            }
            else
            {
                smoothLine.AddNode(mousePos);
            }
            isRepaint = true;
        }

        void CtrlLeftMouseDown(Vector3 mousePos)
        {
            if (info.mouseOverNodeIndex != -1)
            {
                Undo.RecordObject(smoothLine, "Remove Point");
                smoothLine.DeleteNode(info.mouseOverNodeIndex);
                isRepaint = true;
            }
        }

        protected void MouseMove(Ray ray, Vector3 mousePos)
        {
            info.mouseOverNodeIndex = GetClosestIndex(ray, OnPosNodeDelegate);
            info.mouseOverAnchorIndex = GetClosestIndex(ray, OnPosAnchorDelegate);
            SetClosestNodeSegmentIndies(ray, mousePos);
            isRepaint = true;
        }

        protected void LeftMouseDown(Vector3 mousePos)
        {
            Vector3 clickPos = mousePos;
            if (info.mouseOverNodeIndex != -1)
            {
                Undo.RecordObject(smoothLine, "Add Point");
                info.selectedNodeIndex = info.mouseOverNodeIndex;
                pressedPoint = smoothLine.Nodes[info.mouseOverNodeIndex];
                isDoubleClickNode = lastSelectedNodeIndex != info.mouseOverNodeIndex ? false : isDoubleClickNode; // Hareket aracı aktifken diğer düğümlere basarsak, hareket aracı pasif hale gelsin
                lastSelectedNodeIndex = info.mouseOverNodeIndex;
                clickPos = smoothLine.Nodes[info.mouseOverNodeIndex];
            }
            SetDoubleClick(clickPos);

            if (info.mouseOverAnchorIndex != -1)
            {
                info.selectedAnchorIndex = info.mouseOverAnchorIndex;
            }
            isRepaint = true;
        }

        void SetDoubleClick(Vector3 cursorPos)
        {
            if (EditorApplication.timeSinceStartup - lastClickTime < .22f)
            {
                isDoubleClickNode = info.mouseOverNodeIndex != -1 ? true : false;
                smoothLine.transform.position = cursorPos;
            }
            lastClickTime = EditorApplication.timeSinceStartup;
        }

        protected void LeftMouseDrag(Vector3 mousePos, Ray ray)
        {
            if (isDoubleClickNode)
            {
                smoothLine.UpdateNode(lastSelectedNodeIndex, smoothLine.transform.position);
            }
            else
            {
                //Seçilen düğümün pozisyonunu ayarlıyoruz
                if (info.selectedNodeIndex != -1)
                {
                    Undo.RecordObject(smoothLine, "Move Point");
                    float enter = 0;
                    Vector3 inNormal = smoothLine.movingSpace == SLMovingSpace.XY ? Vector3.forward : Vector3.up;
                    Plane plane = new Plane(inNormal, pressedPoint);
                    if (plane.Raycast(ray, out enter))
                    {
                        Vector3 hitPoint = ray.GetPoint(enter);
                        smoothLine.UpdateNode(info.selectedNodeIndex, hitPoint);
                    }
                }
            }

            // Anchor radius'unu ayarlıyoruz
            if (info.selectedAnchorIndex != -1)
            {
                Undo.RecordObject(smoothLine, "Radius Updated");
                Plane plane = new Plane(smoothLine.GetPerpendicularVector(info.selectedAnchorIndex), smoothLine.Nodes[info.selectedAnchorIndex]);
                float enter = 0;
                if (plane.Raycast(ray, out enter))
                {
                    Vector3 hitPoint = ray.GetPoint(enter);
                    UpdateRadiusWithAnchor(info.selectedAnchorIndex, hitPoint);
                }
            }

            // Transform üzerinden düğümleri hareket ettiriyor
            if (transportWithTransformInEditor && smoothLine.transform.hasChanged)
            {
                Undo.RecordObject(smoothLine, "UpdateAllNodeWithTransform");
                smoothLine.UpdateAllNodeWithTransform();
            }

            isRepaint = true;
        }

        void UpdateRadiusWithAnchor(int index, Vector3 planeHitPoint)
        {
            Vector3 dirFromMouseToNode = (planeHitPoint - smoothLine.Nodes[index]);
            float magFromMouseToNode = dirFromMouseToNode.magnitude;
            Vector3 dirFromControlToNode = smoothLine.Anchors[index].position - smoothLine.Nodes[index];
            float radius = Vector3.Dot(dirFromControlToNode, dirFromMouseToNode) > 0 && magFromMouseToNode > smoothLine.DefaultControlRadius ? magFromMouseToNode : smoothLine.DefaultControlRadius;

            SLSmoothLine.Anchor tempAnchor = smoothLine.Anchors[index];
            tempAnchor.nodeToAnchorDistance = Mathf.Clamp(radius, 1, smoothLine.Anchors[index].maxNodeToAncDistance);
            smoothLine.Anchors[index] = tempAnchor;

            lastSelectedRadiusIndex = index;
            lastSelectedRadius = tempAnchor.nodeToAnchorDistance;
            isRepaintInspector = true;
        }

        void LeftMouseUp()
        {
            info.selectedNodeIndex = -1;
            info.selectedAnchorIndex = -1;
        }

        void ChangeSpace()
        {
            smoothLine.movingSpace = (SLMovingSpace)(1 - (int)smoothLine.movingSpace);
        }

        public int GetClosestIndex(Ray ray, OnPosDelegate onPosDelegate)
        {
            Vector3 rayDir = ray.GetPoint(1000);
            for (int i = 0; i < smoothLine.Anchors.Count; i++)
            {
                Vector3 closestPoint = SLUtility.ClosestPointOnLineSegment(onPosDelegate(i, smoothLine), ray.origin, rayDir);
                if ((closestPoint - onPosDelegate(i, smoothLine)).sqrMagnitude < ClosestSqrDstToSelection)
                    return i;
            }
            return -1;
        }

        void SetClosestNodeSegmentIndies(Ray ray, Vector3 mousePos)
        {
            if (info.mouseOverNodeIndex == -1 && info.mouseOverAnchorIndex == -1)
            {
                Vector2 mouseScreenPos = HandleUtility.WorldToGUIPoint(mousePos);
                int loopCount = smoothLine.IsClose ? smoothLine.Nodes.Count : smoothLine.Nodes.Count - 1;
                for (int i = 0; i < loopCount; i++)
                {
                    int nextIndex = (i + 1) % smoothLine.NumNodes;
                    Vector2 pA = HandleUtility.WorldToGUIPoint(smoothLine.Nodes[i]);
                    Vector2 pB = HandleUtility.WorldToGUIPoint(smoothLine.Nodes[nextIndex]);
                    float distance = HandleUtility.DistancePointToLineSegment(mouseScreenPos, pA, pB);
                    Vector2 screenClosestPoint = SLUtility.ClosestPointOnLineSegment(mouseScreenPos, pA, pB);
                    if (distance < 20)
                    {
                        info.SetSegmentInfo(i, nextIndex, Vector2.Distance(pA, screenClosestPoint) / Vector2.Distance(pA, pB));
                        return;
                    }
                }
            }
            info.SetSegmentInfo(0, 0, 0);
        }

        public class Info
        {
            public int selectedNodeIndex = -1;
            public int mouseOverNodeIndex = -1;

            public int selectedAnchorIndex = -1;
            public int mouseOverAnchorIndex = -1;

            public int[] nodeSegmentIndies = new int[2] { 0, 0 };
            public float segmentPercent;

            public void SetSegmentInfo(int index0, int index1, float segmentPercent)
            {
                nodeSegmentIndies[0] = index0;
                nodeSegmentIndies[1] = index1;
                this.segmentPercent = segmentPercent;
            }
        }
    }
}