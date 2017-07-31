using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mothership : Ship {

    [Tooltip("Power to move 1 world unit")]
    public float powerToMove = 1;
    public float movementSpeed = 2.0f;

    float power = 0;
}
