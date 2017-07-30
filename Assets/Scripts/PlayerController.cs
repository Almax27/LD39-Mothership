using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public int Team = 0;
    public float CameraMovementSpeed = 1;

    private void Update()
    {
        UpdateCameraMove();
    }

    void UpdateCameraMove()
    {
        Vector3 InputVector = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.position += InputVector * CameraMovementSpeed * Time.deltaTime;
    }
}
