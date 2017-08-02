using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour {

    [System.Serializable]
    public class ProjectileStage
    {
        public float speed;
        public float duration;
        public bool isHoming;
        public float turningTime;
    }

    public List<ProjectileStage> stages = new List<ProjectileStage>();

    public List<GameObject> spawnOnHit = new List<GameObject>();
    public List<GameObject> spawnAutoDestruct = new List<GameObject>();

    public bool waitForParticlesOnDestruct = true;

    public AudioClip fireSound = null;
    public float fireAudioVolume = 1;
    public AudioClip hitSound = null;
    public float hitAudioVolume = 1;

    Transform target = null;
    Transform source = null;
    float tick = 0;
    int damageOnHit = 0;
    Vector3 currentDirection = Vector3.up;
    bool pendingDestroy = false;

    AudioSource audioSource;
    static float lastSoundTime = 0;

    private void Awake()
    {
        if(audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.pitch = audioSource.pitch + Random.Range(-0.2f, 0.2f);
        }
    }

    private void Update()
    {
        if(pendingDestroy)
        {
            if(!audioSource.isPlaying)
            {
                Destroy(this.gameObject);
            }
        }
        tick += Time.deltaTime;

        ProjectileStage currentStage = GetCurrentStage();

        if (currentStage == null)
        {
            SelfDestruct();
            return;
        }

        if (target)
        {
            Vector3 targetVector = target.transform.position - this.transform.position;
            if(targetVector.sqrMagnitude < 0.1f)
            {
                OnHitTarget(targetVector.normalized);
            }
            else
            {
                if(currentStage.isHoming)
                {
                    if (currentStage.turningTime > 0)
                    {
                        currentDirection = Vector3.RotateTowards(currentDirection, targetVector, Mathf.PI * (Time.deltaTime / currentStage.turningTime), 0);
                    }
                    else
                    {
                        currentDirection = targetVector;
                    }
                }
            }
        }
        else
        {
            SelfDestruct();
        }
        Vector3 position = transform.position + currentDirection.normalized * currentStage.speed * Time.deltaTime;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(currentDirection));
    }

    public void OnFired(Transform targetObject, Transform sourceObject, int damage, Vector3 initialPosition, Vector3 initialDirection)
    {
        target = targetObject;
        source = sourceObject;
        damageOnHit = damage;
        currentDirection = initialDirection;
        transform.SetPositionAndRotation(initialPosition, Quaternion.LookRotation(initialDirection));

        int team = 0;
        if (source)
        {
            Ship ship = source.GetComponent<Ship>();
            if (ship)
            {
                team = ship.team;
            }
        }
        Color teamColor = GameManager.GetTeamColor(team);
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            foreach (Material mat in renderer.materials)
            {
                mat.color = teamColor;
            }
        }
        if(fireSound != null)
        {
            PlaySound(fireSound, fireAudioVolume);
        }
    }

    ProjectileStage GetCurrentStage()
    {
        float time = 0;
        foreach(ProjectileStage stage in stages)
        {
            time += stage.duration;
            if(tick < time)
            {
                return stage;
            }
        }
        return null;
    }

    void OnHitTarget(Vector3 direction)
    {
        foreach(GameObject objToSpawn in spawnOnHit)
        {
            GameObject gobj = Instantiate<GameObject>(objToSpawn);
            gobj.transform.position = this.transform.position;
        }

        if (target)
        {
            DamagePacket damagePacket = new DamagePacket();
            damagePacket.value = damageOnHit;
            damagePacket.direction = direction;
            damagePacket.Send(target, source);
        }

        float destroyDelay = 0;
        if (hitSound != null)
        {
            destroyDelay = hitSound.length;
            PlaySound(hitSound, hitAudioVolume);
        }

        HideAndDestroy();
        target = null;
    }

    void SelfDestruct()
    {
        foreach (GameObject objToSpawn in spawnAutoDestruct)
        {
            GameObject gobj = Instantiate<GameObject>(objToSpawn);
            gobj.transform.position = this.transform.position;
        }

        target = null;
        if (waitForParticlesOnDestruct)
        {
            AutoDestruct autoDestruct = gameObject.AddComponent<AutoDestruct>();
            autoDestruct.stopParticlesEmitting = true;
        }
        else
        {
            HideAndDestroy();
        }
    }

    void HideAndDestroy()
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
        pendingDestroy = true;
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if(Time.time > lastSoundTime + 0.15f)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
