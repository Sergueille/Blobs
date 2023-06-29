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

    [SerializeField] private float squashDuration;
    [SerializeField] private float squashAmount;
    [SerializeField] private LeanTweenType squashTweenType;

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
        eyes = new List<Eye>();
        int eyeCount = data.eyes;
        data.eyes = 0;
        AddEyes(eyeCount);
        AddEyeVisuals(); // Add manually first time
    }

    public void AddEyes(int count)
    {
        data.eyes += count;

        if (data.eyes < 0)
            throw new System.Exception("Removed too many eyes!");

        eyeVisualsToBeAdded += count;
    }

    public override void ApplyChanges()
    {
        Vector2 newScreenPosition = GameManager.i.GetScreenPosition(data.position);
        float distance = (data.position - oldPosition).magnitude;
        Vector2 direction = data.position - oldPosition;

        Vector3 squash;
        Vector3 squash2;
        if (direction.x == 0)
        {
            squash = new Vector3(1 - squashAmount, 1, 1);
            squash2 = new Vector3(1, 1 - squashAmount, 1);
        }
        else
        {
            squash = new Vector3(1, 1 - squashAmount, 1);
            squash2 = new Vector3(1 - squashAmount, 1, 1);
        }

        if (distance > 0)
            LeanTween.scale(blobSprite.gameObject, squash, Mathf.Min(moveSpeed * distance, squashDuration)).setEase(squashTweenType);

        LeanTween.move(gameObject, newScreenPosition, moveSpeed * distance).setOnComplete(() => {
            oldPosition = data.position;

            if (eyeVisualsToBeAdded > 0)
                AddEyeVisuals();

            base.ApplyChanges();

            // Apply force on eyes
            LeanTween.value(endMoveEyesForce, 0, endMoveEyesForceDuration)
                .setOnUpdate((val) => currentEndMoveEyesForce = direction.normalized * val);

            if (distance > 0)
            {
                LeanTween.moveLocal(blobSprite.gameObject, direction.normalized * squashAmount / 2, squashDuration).setEase(squashTweenType);
                LeanTween.scale(blobSprite.gameObject, squash2, squashDuration).setEase(squashTweenType).setOnComplete(() => {
                    LeanTween.scale(blobSprite.gameObject, Vector3.one, squashDuration).setEase(squashTweenType);
                    LeanTween.moveLocal(blobSprite.gameObject, Vector3.zero, squashDuration).setEase(squashTweenType);
                });
            }
        });
    }

    protected override void MoveVisual() {} // Already handled in ApplyChanges function

    protected override void DestroyImmediately()
    {
        shouldBeDestroyed = false;
        
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
        if (eyeVisualsToBeAdded < 0)
        {
            for (int i = 0; i < -eyeVisualsToBeAdded; i++)
                RemoveEyeVisual();
        }
        else
        {
            for (int i = 0; i < eyeVisualsToBeAdded; i++)
                AddEyeVisual();
        }


        eyeVisualsToBeAdded = 0;
    }

    private void AddEyeVisual()
    {
        Eye newEye = Instantiate(eyePrefab, gameObject.transform).GetComponent<Eye>();
        newEye.transform.localScale = Vector3.one * eyesSize.GetRandom() * blobSprite.transform.localScale.x;
        newEye.transform.localPosition = new Vector3(Random.Range(-0.01f, 0.01f), Random.Range(-0.01f, 0.01f)); // Prevent eyes at exact same position

        eyes.Add(newEye);
    }

    private void RemoveEyeVisual()
    {
        Destroy(eyes[0]);
        eyes.RemoveAt(0);
    }

    protected override void SetColorVisual(bool immediate = false)
    {        
        if (immediate)
            blobSprite.color = GameManager.i.colors[(int)data.color];
        else
            LeanTween.color(blobSprite.gameObject, GameManager.i.colors[(int)data.color], colorTransitionDuration);
    }
}
