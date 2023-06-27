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
    [SerializeField] private float endMoveyesForce;

    [SerializeField] private RandomRange eyesSize;

    private List<Eye> eyes;
    private bool shouldBeDestroyed = false; // Used to delay destroy
    private bool isMoving = false; // Used to delay actions after moving

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
    }

    public override void Init(LevelObjectData data)
    {
        this.data = data;
        blobSprite.transform.localScale = Vector3.one * GameManager.i.tileSize;
        SetColor(data.color, true);

        eyes = new List<Eye>();

        int eyeCount = data.eyes;
        data.eyes = 0;
        AddEyes(eyeCount);
    }

    public override void Move(Vector2Int newPosition)
    {
        if (data.position == newPosition) return;

        isMoving = true;

        Vector2 newScreenPosition = GameManager.i.GetScreenPosition(newPosition);
        float distance = (newPosition - data.position).magnitude;
        data.position = newPosition;
        LeanTween.move(gameObject, newScreenPosition, moveSpeed * distance).setOnComplete(OnFinishedMove);
    }

    public void SetColor(GameColor color, bool immediate = false)
    {
        data.color = color;

        if (immediate)
            blobSprite.color = GameManager.i.colors[(int)color];
        else
            LeanTween.color(blobSprite.gameObject, GameManager.i.colors[(int)color], colorTransitionDuration);
    }

    public void AddEyes(int count)
    {
        for (int i = 0; i < count; i++)
            AddEye();
    }

    public void AddEye()
    {
        Eye newEye = Instantiate(eyePrefab, gameObject.transform).GetComponent<Eye>();
        newEye.transform.localScale = Vector3.one * eyesSize.GetRandom() * blobSprite.transform.localScale.x;
        newEye.transform.localPosition = new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f)); // Prevent eyes at exact same position

        eyes.Add(newEye);

        data.eyes++;
    }

    public override void DestroyObject()
    {
        shouldBeDestroyed = true;

        if (!isMoving)
            DestroyImmediately();
    }

    private void OnFinishedMove()
    {
        isMoving = false;

        if (shouldBeDestroyed)
        {
            DestroyImmediately();
        }

        // Apply force on eyes
    }

    private void DestroyImmediately()
    {
        shouldBeDestroyed = false;
        // TODO: add particles
        Destroy(gameObject);
    }
}
