using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager i;

    
    public float baseVolume = 0.5f;
    public float transitionDuration = 0.8f;

    public AudioSource mainMusic;
    public AudioSource menuMusic;

    
    private void Awake()
    {
        i = this;
    }

    private void Start() {
        mainMusic.volume = baseVolume;
        menuMusic.volume = 0;
    }
    
    public void SetMusic(bool menu) {
        if (menu) {
            VolumeTransition(mainMusic, 0);
            VolumeTransition(menuMusic, 1);
        }
        else {
            VolumeTransition(mainMusic, 1);
            VolumeTransition(menuMusic, 0);
        }
    }

    private void VolumeTransition(AudioSource source, float target) {
        LeanTween.value(gameObject, source.volume, target, transitionDuration).setOnUpdate(val => {
            source.volume = val * baseVolume;
        });
    }
}
