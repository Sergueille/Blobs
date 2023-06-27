using UnityEngine;

[System.Serializable]
public class RandomRange
{
    public float min;
    public float max;

    public RandomRange(float a, float b)
    {
        if (a > b)
        {
            min = b;
            max = a;
        }
        else
        {
            min = a;
            max = b;
        }
    }

    public float GetRandom()
    {
        return Random.Range(min, max);
    }
}