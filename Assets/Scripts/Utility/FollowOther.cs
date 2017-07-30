using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowOther : MonoBehaviour {

    public Transform other = null;
    public bool followPosition = true;
    public bool followRotation = true;
    public bool followScale = true;
    private void LateUpdate()
    {
        if (other)
        {
            if (followPosition)
            {
                transform.position = other.position;
            }
            if (followRotation)
            {
                transform.rotation = other.rotation;
            }
            if (followScale)
            {
                transform.localScale = other.localScale;
            }
        }
    }
}
