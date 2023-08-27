using UnityEngine;
using SmoothLineTool;

namespace SmoothLineExamples
{
    public class PathFollower : MonoBehaviour
    {
        public SLSmoothLine smoothLine;
        public SLMoveType moveType;

        float distanceTravelled;
        public float speed = 3;

        private void Update()
        {
            distanceTravelled += Time.deltaTime * speed;
            transform.position = smoothLine.GetPointAtTravelledDistance(distanceTravelled, moveType);
            Vector3 lookDir = smoothLine.GetDirectionAtDistanceTravelled(distanceTravelled, transform.position, transform.forward, moveType);
            transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }
}