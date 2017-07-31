using UnityEngine;
using System.Collections;

public class AutoDestruct : MonoBehaviour {

    public float delay;
    public bool waitForParticles = true;
    public bool stopParticlesEmitting = false;
    private float tick;

    private void Start()
    {
    }

    // Update is called once per frame
    void Update () 
    {
        if (tick > delay)
        {
            bool destroy = true;
            if (waitForParticles)
            {
                foreach (var ps in GetComponentsInChildren<ParticleSystem>())
                {
                    if (ps.IsAlive())
                    {
                        destroy = false;
                        break;
                    }
                }
            }
            if (destroy)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            tick += Time.deltaTime;
            if(tick > delay)
            {
                if (stopParticlesEmitting)
                {
                    foreach (var ps in GetComponentsInChildren<ParticleSystem>())
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    }
                }
            }
        }
	}
}
