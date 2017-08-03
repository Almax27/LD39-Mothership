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

    public ParticleSystem particlesToWaitForOnKill = null;
    public GameObject contentToHideOnKill = null;

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
            bool keepAlive = audioSource.isPlaying;
            keepAlive |= particlesToWaitForOnKill && particlesToWaitForOnKill.IsAlive(true);
            if(!keepAlive)
            {
                //Destroy(this.gameObject);
                this.gameObject.SetActive(false); //make inactive to be pooled
                pendingDestroy = false;
            }
            return;
        }
        tick += Time.deltaTime;

        ProjectileStage currentStage = GetCurrentStage();

        if (currentStage == null)
        {
            KillProjectile();
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
            KillProjectile();
        }
        Vector3 position = transform.position + currentDirection.normalized * currentStage.speed * Time.deltaTime;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(currentDirection));
    }

    public void OnFired(Transform targetObject, Transform sourceObject, int damage, Vector3 initialPosition, Vector3 initialDirection)
    {
        pendingDestroy = false;
        tick = 0;

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

        if (particlesToWaitForOnKill)
        {
            particlesToWaitForOnKill.Play(true);
        }
        if (contentToHideOnKill)
        {
            contentToHideOnKill.SetActive(true);
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

        if (hitSound != null)
        {
            PlaySound(hitSound, hitAudioVolume);
        }

        KillProjectile();
    }

    void KillProjectile()
    {
        target = null;
        pendingDestroy = true;
        if(particlesToWaitForOnKill)
        {
            particlesToWaitForOnKill.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if(contentToHideOnKill)
        {
            contentToHideOnKill.SetActive(false);
        }
    }

    void PlaySound(AudioClip clip, float volume)
    {
        if(Time.time > lastSoundTime + 0.15f)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
