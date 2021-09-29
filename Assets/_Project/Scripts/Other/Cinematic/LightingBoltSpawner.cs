using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingBoltSpawner : MonoBehaviour {

    public float spawnHeight;
    public float minRadius;
    public float maxRadius;
    public float minTime;
    public float maxTime;

    public ParticleSystem lightningBolt;
    private float timeUntilNextLightning;
    
    private void Start () {
        timeUntilNextLightning = Random.Range(minTime, maxTime);
    }

    private void Update () {
        if((timeUntilNextLightning < 0f && !NetAssist.inst.Mode.HasFlag(NetAssistMode.Record)) || Input.GetKeyDown(KeyCode.O)) {
            float angle = Random.value * Mathf.PI * 2f;
            Vector2 circle = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(minRadius, maxRadius, Random.value);
            lightningBolt.transform.position = new Vector3(circle.x, spawnHeight, circle.y);
            lightningBolt.Play();
            MainLightManager.FlashLight();
            AudioManager.PlayEnvironmentSoundAt(lightningBolt.transform.position, EnvironmentSound.Thunder, 1f, 1000f);

            timeUntilNextLightning = Random.Range(minTime, maxTime);
        } else {
            timeUntilNextLightning -= Time.deltaTime;
        }
    }
}
