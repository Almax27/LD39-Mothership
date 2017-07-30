using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public int team = 0;
    public float cameraMovementSpeed = 10;
    public float maxCameraSpeed = 50;

    bool returningToMotherShip = false;
    Mothership mothership = null;

    Vector3 cameraVelocity = Vector3.zero;

    private void Update()
    {
        UpdateCameraMove();

        if(Input.GetKeyDown(KeyCode.Space))
        {
            returningToMotherShip = true;
            mothership = FindObjectOfType<Mothership>();
        }
    }

    void UpdateCameraMove()
    {
        Vector3 rawInputVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (returningToMotherShip && rawInputVector.sqrMagnitude <= 0.01f)
        {
            returningToMotherShip = false;
            if (mothership)
            {
                Vector3.SmoothDamp(transform.position, mothership.transform.position, ref cameraVelocity, 0.3f, maxCameraSpeed);
                returningToMotherShip = (transform.position - mothership.transform.position).sqrMagnitude > 1.0f;
            }
        }
        else
        {
            returningToMotherShip = false;
            cameraVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * cameraMovementSpeed;
        }

        transform.position += cameraVelocity * Time.deltaTime;
    }
}
