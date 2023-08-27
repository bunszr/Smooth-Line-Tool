using UnityEngine;
using SmoothLineTool;


namespace SmoothLineExamples
{
    /* Bu scriptin düzgün çalışılabilmesi için SmoothLine'e objejisi seçip başka bir yere tıklanmalı = (SmoothLineEditor içerisindeki OnDisable metodu çağrılmalı yani) 
        yada autoUpdate = true olmalı */

    [ExecuteInEditMode]
    public class PathPlacer : MonoBehaviour
    {
        public SLSmoothLine smoothLine;
        public int numObject = 20;
        public float radius = .5f;

        public bool autoUpdate = true;

        public void Init()
        {
            smoothLine = GetComponent<SLSmoothLine>();
        }

        private void OnDrawGizmosSelected()
        {
            if (autoUpdate)
            {
                smoothLine.UpdateWhenPathChanges();
            }

            Gizmos.color = Color.yellow;

            float stepPercent = 1f / numObject;
            for (int i = 0; i < numObject; i++)
            {
                Vector3 pos = smoothLine.GetPointAtTime(stepPercent * i); ;
                Gizmos.DrawSphere(pos, radius);
            }
        }
    }
}