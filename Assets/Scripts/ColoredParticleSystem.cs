using System.Collections.Generic;
using UnityEngine;

public class ColoredParticleSystem : MonoBehaviour
{
    [SerializeField] ParticleSystem ps;
    [SerializeField] ParticleSystem child;

    public void Play(GameColor color)
    {
        ps.startColor = GameManager.i.colors[(int)color];
        child.startColor = GameManager.i.colors[(int)color];
        ps.Play();
        Destroy(this.gameObject, ps.main.duration + ps.main.startLifetime.constant); // FIXME: probably don't work with random
    }
}
