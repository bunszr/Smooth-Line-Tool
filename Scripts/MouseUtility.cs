using UnityEditor;
using UnityEngine;

public static class MouseUtility 
{
    public static Vector3 GetMousePosWithMoveSpace(Ray ray , Vector2 guiMousePos, MovingSpace pathSpace, float planeHeight)
    {
        float dst = 0;
        if (pathSpace == MovingSpace.XY)
        {
            dst = (planeHeight - ray.origin.z) / ray.direction.z;
            return ray.origin + ray.direction * dst;
        }
        dst = (planeHeight - ray.origin.y) / ray.direction.y;
        return ray.origin + ray.direction * dst;
    }   
}