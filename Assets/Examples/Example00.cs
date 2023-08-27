using UnityEngine;
using SmoothLineTool;

namespace SmoothLineExamples
{
    public class Example00 : MonoBehaviour
    {
        public SLSmoothLine smoothLine;
        public SLMoveType moveType;

        float time;
        public float speed = 3;

        private void Start()
        {
            transform.position = smoothLine.SmoothPoints[0];
            smoothLine.TransportWithTransform = true;
        }

        private void Update()
        {
            time += Time.deltaTime * speed;
            transform.position = smoothLine.GetPointAtTime(time, moveType);
            Vector3 lookDir = smoothLine.GetDirectionAtTime(time, transform.position, transform.forward, moveType);
            transform.rotation = Quaternion.LookRotation(lookDir);

            DoAnim();
        }

        void DoAnim()
        {
            smoothLine.transform.Rotate(Vector3.right * Time.deltaTime * 20);
            smoothLine.UpdateAllNodeWithTransform();
            // smoothLine.ConvertToSmoothCurve(); //smoothline nesnesinin rotasyonu ve posizyonu ile oynanırsa "UpdateWhenPathChanges" yerine buda çağrılabilir
            smoothLine.UpdateWhenPathChanges();
        }
    }
}