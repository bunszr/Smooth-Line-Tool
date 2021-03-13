using UnityEngine;

// distanceTravelled(alınan yol) ile hareket ettiriyoruz.
public class ChangeablePathFollowerV2 : MonoBehaviour
{
    public SmoothLine smoothLine;
    public MoveType moveType;

    float distanceTravelled;
    public float speed = 3;
    int dir = 1;

    Vector3[] defaultNodes;

    private void Start()
    {
        smoothLine.OnPathChangedEvent += OnPathChanged;
        transform.position = smoothLine.SmoothPoints[0];
        defaultNodes = smoothLine.Nodes.ToArray();
    }

    private void Update()
    {
        distanceTravelled += Time.deltaTime * dir * speed;
        transform.position = smoothLine.GetPointAtTravelledDistance(distanceTravelled, moveType);
        Vector3 lookDir = smoothLine.GetDirectionAtDistanceTravelled(distanceTravelled, transform.position, transform.forward, moveType);
        transform.rotation = Quaternion.LookRotation(lookDir);

        // DoNodesAnim();
    }

    void DoNodesAnim()
    {
        for (int i = 0; i < defaultNodes.Length; i++)
        {
            Vector3 newPos = defaultNodes[i];
            newPos.y = defaultNodes[i].y + Mathf.PingPong(Time.time * .4f * (i + 1), 1.5f);
            smoothLine.UpdateNode(i, newPos);
            smoothLine.UpdateWhenPathChanges();
        }
    }

    public void OnPathChanged()
    {
        if (moveType == MoveType.Stop && distanceTravelled >= smoothLine.TotalPathDistance)
            return;
        else if (moveType == MoveType.Reverse)
        {
            if (distanceTravelled >= smoothLine.TotalPathDistance)
                dir = -1;
            else if (distanceTravelled < 0)
                dir = 1;
        }
        distanceTravelled = smoothLine.GetClosestDistanceTravelled(transform.position, moveType);
    }
}