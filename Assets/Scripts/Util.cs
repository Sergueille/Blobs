using UnityEngine;

public static class Util
{
    public static void DestroyChildren(GameObject go)
    {
        foreach (Transform child in go.transform) {
            GameObject.Destroy(child.gameObject);
        }
    }

    public static string FirstLetterUppercase(string text)
    {
        return char.ToUpper(text[0]) + text.Substring(1, text.Length - 1).ToLower();
    }
}

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