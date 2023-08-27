using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;
using SmoothLineTool;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SmoothLineTool
{
    [ExecuteInEditMode]
    public class SLRoadCreatorOptimized : MonoBehaviour
    {
        public SmoothLineTool.SLSmoothLine smoothLine;

        [SerializeField, Min(.1f)] float roadHalfWidth = .4f;
        [SerializeField, Min(.1f)] float thickness = .15f;

        [SerializeField] MeshFilter meshFilter;
        [SerializeField] MeshRenderer meshRenderer;

        Vector3 offset = new Vector3(0, .5f, 0);

        enum Space { XY, XZ }
        [SerializeField] Space space;

        public bool autoUpdate = false;

        private void Update()
        {
            if (autoUpdate) CreateRoadMesh();
        }

        public void UpdateMesh()
        {
            CreateRoadMesh();
        }

        void CreateRoadMesh()
        {
            List<Vector3> points = smoothLine.SmoothPoints;
            int pointsCount = points.Count;

            int vertIndex = 0;
            int triIndex = 0;
            int otherTriIndex = 0;

            int[] roadTriangleMap = { 2, 10, 3, 3, 10, 11 };
            int[] otherTriangleMap = { 0, 8, 1, 1, 8, 9, 4, 12, 5, 5, 12, 13, 6, 14, 7, 7, 14, 15 };

            Vector3[] verts = new Vector3[points.Count * 8];
            Vector2[] uvs = new Vector2[verts.Length];
            Vector3[] normals = new Vector3[verts.Length];

            int[] roadTriangles = new int[6 * (points.Count - 1)]; // 1 kare çizmek için 6 tane tris count gerekli. Neden (points.Count - 1) dedik onu hatırlıyamadım
            int[] otherTriangles = new int[18 * (points.Count - 1)]; // 3 kare çizmek için 6 * 3 = 18 tane tris count gerekli

            for (int i = 0; i < points.Count; i++)
            {
                Vector3 currPoint = points[i];
                Vector3 dirCurrToNext = (i < pointsCount - 1) ? (points[i + 1] - points[i]).normalized : (points[i] - points[i - 1]).normalized;
                Vector3 normalRight = space == Space.XZ ? RotateXZPlane(dirCurrToNext) : RotateXYPlane(dirCurrToNext);

                Vector3 localUp = Vector3.Cross(dirCurrToNext, normalRight);

                // vertSideA = sol üst , vertSideB = sağ üst , vertBottomA = sol alt , vertBottomB = sağ alt
                Vector3 vertSideA = currPoint - normalRight * roadHalfWidth;
                Vector3 vertSideB = currPoint + normalRight * roadHalfWidth;

                Vector3 vertBottomA = vertSideA - localUp * thickness;
                Vector3 vertBottomB = vertSideB - localUp * thickness;

                // Add Left of road vertices
                verts[vertIndex + 0] = vertBottomA;
                verts[vertIndex + 1] = vertSideA;
                // Add top of road vertices
                verts[vertIndex + 2] = vertSideA;
                verts[vertIndex + 3] = vertSideB;
                // Add Right of road vertices
                verts[vertIndex + 4] = vertSideB;
                verts[vertIndex + 5] = vertBottomB;
                // Add Bottom of road vertices
                verts[vertIndex + 6] = vertBottomB;
                verts[vertIndex + 7] = vertBottomA;

                float yUv = i / (float)(pointsCount - 1);
                float atTime = smoothLine.GetClosestTimeOnPath(currPoint, SLMoveType.Stop);
                uvs[vertIndex + 0] = new Vector2(0, atTime);
                uvs[vertIndex + 1] = new Vector2(1, atTime);
                uvs[vertIndex + 2] = new Vector2(0, atTime);
                uvs[vertIndex + 3] = new Vector2(1, atTime);
                uvs[vertIndex + 4] = new Vector2(0, atTime);
                uvs[vertIndex + 5] = new Vector2(1, atTime);
                uvs[vertIndex + 6] = new Vector2(0, atTime);
                uvs[vertIndex + 7] = new Vector2(1, atTime);

                // Left of road normal
                normals[vertIndex + 0] = -normalRight;
                normals[vertIndex + 1] = -normalRight;
                // Top of road normal
                normals[vertIndex + 2] = localUp;
                normals[vertIndex + 3] = localUp;
                // Right of road normal
                normals[vertIndex + 4] = normalRight;
                normals[vertIndex + 5] = normalRight;
                // Bottom of road normal
                normals[vertIndex + 6] = -localUp;
                normals[vertIndex + 7] = -localUp;

                // Set triangle indices
                if (i < pointsCount - 1) // o anki vertex ve bir sonraki vertex'in triangle'ını düzenlediğimiz için bu kontrol var
                {
                    for (int tri = 0; tri < roadTriangleMap.Length; tri++)
                    {
                        roadTriangles[triIndex + tri] = roadTriangleMap[tri] + i * 8; // 8 ile çarpamızın sebebi 8 tane vertex var
                    }
                    // Debug.Log(roadTriangles[i] + " , " + roadTriangles[i + 1] + " , " + roadTriangles[i + 2] + " , ");
                    // Debug.Log(roadTriangles[i + 3] + " , " + roadTriangles[i + 4] + " , " + roadTriangles[i + 5] + " , ");
                    for (int tri = 0; tri < otherTriangleMap.Length; tri++)
                    {
                        otherTriangles[otherTriIndex + tri] = otherTriangleMap[tri] + i * 8;
                    }
                }

                vertIndex += 8;
                triIndex += roadTriangleMap.Length;
                otherTriIndex += otherTriangleMap.Length;
            }

            Mesh mesh = new Mesh();
            mesh.Clear();
            mesh.name = "RoadMesh";
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            mesh.subMeshCount = 2;
            mesh.SetTriangles(roadTriangles, 0);
            mesh.SetTriangles(otherTriangles, 1);
            mesh.RecalculateBounds();
            meshFilter.mesh = mesh;
        }

        public Vector3 RotateXYPlane(Vector3 vector) => new Vector3(vector.y, -vector.x, 0);
        public Vector3 RotateXZPlane(Vector3 vector) => new Vector3(vector.z, 0, -vector.x);

        // private void OnDrawGizmos()
        // {
        //     for (int i = 0; i < verts.Length; i++)
        //     {
        //         Gizmos.DrawSphere(verts[i], .05f);
        //     }
        // }
    }
}

#if UNITY_EDITOR
namespace SmoothLineEditorNamespace
{
    [CustomEditor(typeof(SLRoadCreatorOptimized))]
    public class SLRoadCreatorOptimizedEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            SLRoadCreatorOptimized sLRoadCreator = (SLRoadCreatorOptimized)target;

            if (GUILayout.Button("Update Mesh"))
            {
                sLRoadCreator.UpdateMesh();
                SceneView.RepaintAll();
            }
        }
    }
}
#endif