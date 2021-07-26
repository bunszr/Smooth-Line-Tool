using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SmoothLineCreation;

namespace SmoothLineExamples
{
    public class Example01 : MonoBehaviour
    {
        public SmoothLine smoothLine;
        public float speed = 3;

        float angle;

        void Update()
        {
            angle = Time.deltaTime * speed;
            Vector3 closestPoint = Utility.ClosestPointOnLineSegment(smoothLine.Nodes[1], smoothLine.Nodes[0], smoothLine.Nodes[2]);
            Vector3 axis = smoothLine.Nodes[2] - smoothLine.Nodes[0];
            Vector3 newPoint = closestPoint + Quaternion.AngleAxis(speed, axis) * (smoothLine.Nodes[1] - closestPoint);

            Vector3 node2 = smoothLine.Nodes[2];
            node2.x = Mathf.PingPong(Time.time * 2, 10) - 5;
            smoothLine.UpdateNode(2, node2);
            smoothLine.UpdateNode(1, newPoint);
            smoothLine.UpdateWhenPathChanges();
        }

        IEnumerator Show()
        {
            int index = 1;
            while (true)
            {
                if (index % 2 == 0)
                    smoothLine.AddNode(Vector3.right * index * 10 + Vector3.up * 5);
                else
                    smoothLine.AddNode(Vector3.right * index * 10);
                smoothLine.DeleteNode(0);
                smoothLine.UpdateRadius(1, 100);
                smoothLine.UpdateWhenPathChanges();
                index++;

                yield return new WaitForSeconds(1.5f);
            }
        }
    }
}