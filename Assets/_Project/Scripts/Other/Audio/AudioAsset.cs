using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Sound", menuName = "Custom/Audio/Audio Asset")]
public class AudioAsset : ScriptableObject {

    public string description;
    public AudioClip[] audioClips;
    [Range(0.0f, 0.2f)] public float pitchVariationRange = 0.05f;

    public AudioClip GetRandomClip () {
        if(audioClips != null && audioClips.Length > 0) {
            return audioClips[Random.Range(0, audioClips.Length)];
        }
        return null;
    }
}
