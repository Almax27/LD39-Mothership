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

    Transform target = null;
    Transform source = null;
    float tick = 0;
    int damageOnHit = 0;
    Vector3 currentDirection = Vector3.up;

    public void OnFired(Transform targetObject, Transform sourceObject, int damage, Vector3 initialPosition, Vector3 initialDirection)
    {
		target = targetObject;
        source = sourceObject;
        damageOnHit = damage;
        currentDirection = initialDirection;
        transform.SetPositionAndRotation(initialPosition, Quaternion.LookRotation(initialDirection));
    }

    private void Update()
    {
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
                OnHitTarget(target, targetVector.normalized);
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
        Vector3 position = transform.position + currentDirection.normalized * currentStage.speed * Time.deltaTime;
        transform.SetPositionAndRotation(position, Quaternion.LookRotation(currentDirection));
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

    void OnHitTarget(Transform target, Vector3 direction)
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

        target = null;
        Destroy(this.gameObject);
    }

    void SelfDestruct()
    {
        foreach (GameObject objToSpawn in spawnAutoDestruct)
        {
            GameObject gobj = Instantiate<GameObject>(objToSpawn);
            gobj.transform.position = this.transform.position;
        }

        target = null;
        Destroy(this.gameObject);
    }
}
