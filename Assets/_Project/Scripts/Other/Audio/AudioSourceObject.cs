using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Blast.ObjectPooling;

public class AudioSourceObject : PoolableObject {
    public AudioSource source;
    const float maxExtraEffectTime = 0.1f;

    public void Play (AudioAsset audioAsset) {
        AudioClip clip = audioAsset.GetRandomClip();

        source.clip = clip;
        source.pitch = 1f + Random.Range(-audioAsset.pitchVariationRange, audioAsset.pitchVariationRange);
        source.Play();

        Invoke("DonePlaying", clip.length + maxExtraEffectTime);
    }

    public void SetOutput (AudioMixerGroup output) {
        source.outputAudioMixerGroup = output;
    }

    public void PlayAfterDelay (AudioAsset audioAsset, float delay) {

    }

    public void DonePlaying () {
        source.Stop();
        ReturnToPool();
    }
}
