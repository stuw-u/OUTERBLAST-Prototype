using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Unity.Mathematics;
using Blast.ObjectPooling;

public class AudioManager : MonoBehaviour {

    [Header("Outputs")]
    public AudioMixerGroup sfxOutput;

    [Header("Audio Assets")]
    public AudioAsset digAsset;
    public AudioAsset explosionAsset;
    public AudioAsset launchAsset;
    public AudioAsset jumpingAsset;
    public AudioAsset hittingAsset;
    public AudioAsset playerTickAsset;
    public AudioAsset playerBounceAsset;
    public AudioAsset hurryAsset;
    public AudioAsset menuYEAsset;
    public AudioAsset menuNAAsset;
    public AudioAsset menuTICKAsset;
    public AudioAsset falloffAsset;
    public AudioAsset epicPrankAsset;
    public AudioAsset thunderAsset;
    public AudioAsset shieldHitAsset;
    public AudioAsset shieldBreakAsset;
    public AudioAsset unstackingAsset;
    public AudioAsset stackingAsset;
    public AudioAsset[] stepsAsset;

    [Header("Prefabs")]
    public PoolableObject sourceEnvPrefab;
    public PoolableObject sourceMenuPrefab;

    private static AudioManager inst;
    private ObjectPool sourceEnvClipPool;
    private ObjectPool sourceMenuClipPool;
    private void Awake () {
        inst = this;
        sourceEnvClipPool = new ObjectPool(sourceEnvPrefab, transform, 8);
        sourceMenuClipPool = new ObjectPool(sourceMenuPrefab, transform, 8);
    }

    public static void PlayClientEnvironmentSoundAt (int clientId, float3 position, EnvironmentSound sound) {
        if(SimulationManager.inst.IsReplayingFrame)
            return;

        if(NetAssist.IsClient) {
            PlayEnvironmentSoundAt(position, sound);
        }
        if(NetAssist.IsServer) {
            if(LobbyManager.inst == null)
                return;
            if(clientId == -1) {
                LobbyManager.inst.PlayEnvironnementSoundOnEveryoneExcept(clientId, position, sound);
            } else {
                LobbyManager.inst.PlayEnvironnementSoundOnEveryoneExcept(clientId, position, sound);
            }
        }
    }

    public static void PlayEnvironmentSoundAt (float3 position, EnvironmentSound sound, float volume = 1f, float maxDist = 100f) {
        switch(sound) {
            case EnvironmentSound.Dig:
            inst.PlayEnvClipAt(position, inst.digAsset, volume);
            break;
            case EnvironmentSound.Explosion:
            inst.PlayEnvClipAt(position, inst.explosionAsset, volume * 0.5f);
            break;
            case EnvironmentSound.Launch:
            // Networked
            inst.PlayEnvClipAt(position, inst.launchAsset, volume);
            break;
            case EnvironmentSound.Jumping:
            inst.PlayEnvClipAt(position, inst.jumpingAsset, volume);
            break;
            // Networked
            case EnvironmentSound.Hitting:
            inst.PlayEnvClipAt(position, inst.hittingAsset, volume);
            break;
            case EnvironmentSound.PlayerTick:
            // Networked
            inst.PlayEnvClipAt(position, inst.playerTickAsset, volume);
            break;
            case EnvironmentSound.PlayerBounce:
            inst.PlayEnvClipAt(position, inst.playerBounceAsset, volume * 0.5f);
            break;
            case EnvironmentSound.Hurry:
            inst.PlayEnvClipAt(position, inst.hurryAsset, volume);
            break;
            case EnvironmentSound.Step:
            inst.PlayEnvClipAt(position, inst.stepsAsset[0], 1f);
            break;
            case EnvironmentSound.Thunder:
            inst.PlayEnvClipAt(position, inst.thunderAsset, volume, maxDist);
            break;
            case EnvironmentSound.ShieldHit:
            inst.PlayEnvClipAt(position, inst.shieldHitAsset, volume, maxDist);
            break;
            case EnvironmentSound.ShieldBreak:
            inst.PlayEnvClipAt(position, inst.shieldBreakAsset, volume, maxDist);
            break;
            case EnvironmentSound.Stacking:
            // Networked
            inst.PlayEnvClipAt(position, inst.stackingAsset, volume, maxDist);
            break;
            case EnvironmentSound.Unstacking:
            // Networked
            inst.PlayEnvClipAt(position, inst.unstackingAsset, volume, maxDist);
            break;
        }
    }

    public static void PlayMenuSound (MenuSound sound) {
        switch(sound) {
            case MenuSound.YE:
            inst.PlayMenuClipAt(inst.menuYEAsset);
            break;
            case MenuSound.NA:
            inst.PlayMenuClipAt(inst.menuNAAsset);
            break;
            case MenuSound.TICK:
            inst.PlayMenuClipAt(inst.menuTICKAsset);
            break;
            case MenuSound.Falloff:
            //Networked ?
            inst.PlayMenuClipAt(inst.falloffAsset);
            break;
            case MenuSound.EpicPrank:
            inst.PlayMenuClipAt(inst.epicPrankAsset);
            break;
        }
    }

    private void PlayEnvClipAt (float3 position, AudioAsset audioAsset, float volume = 1f, float maxDist = 100f) {
        AudioSourceObject source = (AudioSourceObject)sourceEnvClipPool.GetNewInstance();
        if(source == null)
            return;
        source.transform.position = position;
        source.SetOutput(sfxOutput);
        source.source.volume = volume;
        source.source.maxDistance = maxDist;
        source.Play(audioAsset);
    }

    private void PlayMenuClipAt (AudioAsset audioAsset) {
        AudioSourceObject source = (AudioSourceObject)sourceMenuClipPool.GetNewInstance();
        if(source == null)
            return;
        source.SetOutput(sfxOutput);
        source.Play(audioAsset);
    }
}

public enum EnvironmentSound {
    Dig,
    Explosion,
    Launch,
    Jumping,
    Hitting,
    PlayerTick,
    PlayerBounce,
    Hurry,
    Step,
    Thunder,
    ShieldHit,
    ShieldBreak,
    Stacking,
    Unstacking
}

public enum MenuSound {
    YE,
    NA,
    TICK,
    Falloff,
    ClaimScore,
    TopScore,
    EpicPrank
}
