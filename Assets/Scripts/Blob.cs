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
    [SerializeField] private float endMoveEyesForce;
    [SerializeField] private float endMoveEyesForceDuration;

    [SerializeField] private RandomRange eyesSize;

    private List<Eye> eyes;
    private int eyeVisualsToBeAdded = 0;
    
    private Vector2 currentEndMoveEyesForce = Vector2.zero;

    private void Update()
    {
        // Move eyes
        for (int i = 0; i < eyes.Count; i++)
        {
            Vector2 centerDirection = -eyes[i].transform.localPosition;
            Vector2 centerForce = centerDirection / eyeCenterForceDistance * eyeCenterForce;

            Vector2 forceSum = centerForce + currentEndMoveEyesForce;

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
        base.Init(data);

        blobSprite.transform.localScale = Vector3.one * GameManager.i.tileSize;
        
        eyes = new List<Eye>();
        int eyeCount = data.eyes;
        data.eyes = 0;
        AddEyes(eyeCount);
        AddEyeVisuals(); // Add manually first time
    }

    public void AddEyes(int count)
    {
        data.eyes += count;
        eyeVisualsToBeAdded += count;
    }

    public override void ApplyChanges()
    {
        Vector2 newScreenPosition = GameManager.i.GetScreenPosition(data.position);
        float distance = (data.position - oldPosition).magnitude;
        Vector2 direction = data.position - oldPosition;

        LeanTween.move(gameObject, newScreenPosition, moveSpeed * distance).setOnComplete(() => {
            oldPosition = data.position;

            if (eyeVisualsToBeAdded > 0)
                AddEyeVisuals();

            base.ApplyChanges();

            // Apply force on eyes
            LeanTween.value(endMoveEyesForce, 0, endMoveEyesForceDuration)
                .setOnUpdate((val) => currentEndMoveEyesForce = direction.normalized * val);
        });
    }

    protected override void MoveVisual() {} // Already handled in ApplyChanges function

    protected override void DestroyImmediately()
    {
        shouldBeDestroyed = false;
        // TODO: add particles
        
        // Remove eyes
        for (int i = 0; i < eyes.Count; i++)
        {
            Destroy(eyes[i].gameObject);
        }

        eyes.Clear();

        // Remove background
        blobSprite.enabled = false;
    }

    private void AddEyeVisuals()
    {
        for (int i = 0; i < eyeVisualsToBeAdded; i++)
            AddEyeVisual();

        eyeVisualsToBeAdded = 0;
    }

    private void AddEyeVisual()
    {
        Eye newEye = Instantiate(eyePrefab, gameObject.transform).GetComponent<Eye>();
        newEye.transform.localScale = Vector3.one * eyesSize.GetRandom() * blobSprite.transform.localScale.x;
        newEye.transform.localPosition = new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f)); // Prevent eyes at exact same position

        eyes.Add(newEye);
    }

    protected override void SetColorVisual(bool immediate = false)
    {        
        if (immediate)
            blobSprite.color = GameManager.i.colors[(int)data.color];
        else
            LeanTween.color(blobSprite.gameObject, GameManager.i.colors[(int)data.color], colorTransitionDuration);
    }
}
