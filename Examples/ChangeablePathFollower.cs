using UnityEngine;

// time(percent) ile hareket ettiriyoruz.
public class ChangeablePathFollower : MonoBehaviour
{
    public SmoothLine smoothLine;
    public MoveType moveType;

    float time;
    int dir = 1;
    public float speed = 3;

    Vector3[] defaultNodes;

    private void Start()
    {
        smoothLine.OnPathChangedEvent += OnPathChanged;
        transform.position = smoothLine.SmoothPoints[0];
        defaultNodes = smoothLine.Nodes.ToArray();
    }

    private void Update()
    {
        time += Time.deltaTime * dir * speed;
        transform.position = smoothLine.GetPointAtTime(time, moveType);
        Vector3 lookDir = smoothLine.GetDirectionAtTime(time, transform.position, transform.forward, moveType);
        transform.rotation = Quaternion.LookRotation(lookDir);

        DoNodesAnim();
    }

    void DoNodesAnim()
    {
        for (int i = 0; i < defaultNodes.Length; i++)
        {
            Vector3 newPos = defaultNodes[i];
            newPos.y = defaultNodes[i].y + Mathf.PingPong(Time.time * .4f * (i + 1), 1.5f);
            smoothLine.UpdateNode(i, newPos);
        }
        smoothLine.UpdateWhenPathChanges();
    }

    public void OnPathChanged()
    {
        if (moveType == MoveType.Stop && time >= 1)
            return;
        else if (moveType == MoveType.Reverse)
        {
            if (time >= 1)
                dir = -1;
            else if (time < 0)
                dir = 1;
        }
        time = smoothLine.GetClosestTimeOnPath(transform.position, moveType);
    }
}