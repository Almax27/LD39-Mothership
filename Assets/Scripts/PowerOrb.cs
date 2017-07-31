using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerOrb : MonoBehaviour {

    public float perpendicularDeviation = 1.0f;
    public float collectionTime = 1.0f;
    public int powerValue = 0;
    public Transform target = null;

    Vector3 startPosition;
    float deviationTarget = 0;
    float tick = 0;

    private void Start()
    {
        startPosition = transform.position;
        deviationTarget = perpendicularDeviation * Random.Range(-1.0f, 1.0f);
    }

    // Update is called once per frame
    void Update ()
    {
		if(target)
        {
            if (tick < collectionTime)
            {
                tick += Time.deltaTime;
                float t = Mathf.Clamp01(tick / collectionTime);
                if (t >= 1)
                {
                    OnCollected();
                }
                else
                {
                    Vector3 position = Easing.Ease(t, startPosition, target.position, 1.0f, Easing.Method.QuadIn);
                    float deviation = 0;
                    if (t < 0.5f)
                    {
                        deviation = Easing.Ease(t * 2.0f, 0, deviationTarget, 1.0f, Easing.Method.QuadInOut);
                    }
                    else
                    {
                        deviation = Easing.Ease((t - 0.5f) * 2.0f, deviationTarget, 0, 1.0f, Easing.Method.QuadIn);
                    }
                    position += Vector3.Cross((target.position - startPosition).normalized, Vector3.up) * deviation;
                    transform.position = position;
                }
            }
            else
            {
                transform.position = target.position;
            }
        }
        else
        {
            Destroy(gameObject);
        }
	}

    void OnCollected()
    {
        AutoDestruct();
    }

    void AutoDestruct()
    {
        AutoDestruct autoDestruct = gameObject.AddComponent<AutoDestruct>();
    }
}
