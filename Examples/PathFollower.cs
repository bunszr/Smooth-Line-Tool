using UnityEngine;

public class PathFollower : MonoBehaviour
{
    public SmoothLine smoothLine;
    public MoveType moveType;

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