using UnityEngine;

[System.Serializable]
public struct PathInfo
{
    public float percent;
    public float distance;
    public int smoothIndex;

    public PathInfo(float percent, float distance, int smoothIndex)
    {
        this.percent = percent;
        this.distance = distance;
        this.smoothIndex = smoothIndex;
    }
}