using System.Collections.Generic;
using System.Data.Common;
using Unity.VisualScripting;
using UnityEngine;

public class Blob : LevelObject
{
    [SerializeField] private SpriteRenderer blobSprite;

    [SerializeField] private GameObject eyePrefab;

    [SerializeField] private float colorTransitionDuration;
    [SerializeField] private float moveSpeed;

    [SerializeField] private float eyeCenterForceDistance;
    [SerializeField] private float eyeCenterForce;
    [SerializeField] private float eyesMargin;
    [SerializeField] private float eyesRepulsionForce;

    [SerializeField] private RandomRange eyesSize;


    private List<Eye> eyes;

    private void Update()
    {
        // Move eyes
        for (int i = 0; i < eyes.Count; i++)
        {
            Vector2 centerDirection = -eyes[i].transform.localPosition;
            Vector2 centerForce = centerDirection / eyeCenterForceDistance * eyeCenterForce;

            Vector2 forceSum = centerForce;

            for (int j = 0; j < eyes.Count; j++)
            {
                if (i == j) continue;

                Vector2 dir = eyes[j].transform.localPosition - eyes[i].transform.localPosition;
                float targetDist = eyesMargin + eyes[i].transform.localScale.x / 2 + eyes[j].transform.localScale.x / 2;
                float dist = dir.magnitude;
                float forceAmount = Mathf.Clamp(targetDist - dist, 0, targetDist) * eyesRepulsionForce;

                forceSum += -dir.normalized * forceAmount;
            }

            eyes[i].transform.localPosition += new Vector3(forceSum.x, forceSum.y, 0) * Time.deltaTime;
        }

        // TEST
        if (Input.GetKeyDown(KeyCode.Space))
        {
            AddEye();
        }
    }

    public override void Init(LevelObjectData data)
    {
        this.data = data;
        blobSprite.transform.localScale = Vector3.one * GameManager.i.tileSize;
        SetColor(data.color, true);

        eyes = new List<Eye>();

        for (int i = 0; i < data.eyes; i++)
            AddEye();
    }

    public override void Move(Vector2Int newPosition)
    {
        if (data.position == newPosition) return;

        Vector2 newScreenPosition = GameManager.i.GetScreenPosition(newPosition);
        float distance = (newPosition - data.position).magnitude;
        data.position = newPosition;
        LeanTween.move(gameObject, newScreenPosition, moveSpeed * distance);
    }

    public void SetColor(GameColor color, bool immediate = false)
    {
        data.color = color;

        if (immediate)
            blobSprite.color = GameManager.i.colors[(int)color];
        else
            LeanTween.color(blobSprite.gameObject, GameManager.i.colors[(int)color], colorTransitionDuration);
    }

    public void AddEye()
    {
        Eye newEye = Instantiate(eyePrefab, gameObject.transform).GetComponent<Eye>();
        newEye.transform.localScale = Vector3.one * eyesSize.GetRandom();
        newEye.transform.localPosition = new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f)); // Prevent eyes at exact same position

        eyes.Add(newEye);
    }
}
