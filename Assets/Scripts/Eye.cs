using System;
using System.Collections.Generic;
using UnityEngine;

public class Eye : MonoBehaviour
{
    [SerializeField] private SpriteRenderer white;
    [SerializeField] private SpriteRenderer black;

    public RandomRange lookDirectionChangeTime;
    public RandomRange blinkTime;

    public RandomRange blinkSpeed;
    public RandomRange lookSpeed;

    public float maxBlackDistance;

    private float nextLookDirectionTime = 0;
    private float nextBlinkTime = 0;

    private float initialWhiteYSize;

    private void Start()
    {
        initialWhiteYSize = white.transform.localScale.y;

        nextLookDirectionTime = 0; // Change look direction now but blink later
        nextBlinkTime = Time.time + blinkTime.GetRandom();
    }

    private void Update()
    {
        if (Time.time > nextLookDirectionTime)
            LookAtRandomDirection();

        if (Time.time > nextBlinkTime)
            Blink();
    }

    public void LookAtRandomDirection()
    {
        Vector2 dir = UnityEngine.Random.onUnitSphere * maxBlackDistance;
        LeanTween.moveLocal(black.gameObject, dir, lookSpeed.GetRandom()).setEaseInOutQuad();

        nextLookDirectionTime = Time.time + lookDirectionChangeTime.GetRandom();
    }

    public void Blink()
    {
        LeanTween.scaleY(white.gameObject, 0, blinkSpeed.GetRandom()).setOnComplete(() => {
            LeanTween.scaleY(white.gameObject, initialWhiteYSize, blinkSpeed.GetRandom());
        });

        nextBlinkTime = Time.time + blinkTime.GetRandom();
    }
}

