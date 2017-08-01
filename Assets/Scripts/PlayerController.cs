using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    
    public int team = 0;
    public float minCameraSpeed = 10;
    public float maxCameraSpeed = 50;

    public float zoomSpeed = 10;
    public float minZoomOut = 10;
    public float maxZoomOut = 30;
    public float targetZoom = 20;

    float currentZoom = 0;
    float zoomVelocity = 0;

    Camera playerCamera = null;
    
    Transform cameraTravelTarget = null;

    Vector3 cameraVelocity = Vector3.zero;

    private void Start()
    {
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera)
        {
            currentZoom = targetZoom;
            playerCamera.transform.position = playerCamera.transform.forward * targetZoom;
        }
    }

    private void Update()
    {
        UpdateCameraMove();

        if(Input.GetKeyDown(KeyCode.Space))
        {
            MothershipFleet mothershipFleet = FindObjectOfType<MothershipFleet>();
            FleetCommander commander = GetComponent<FleetCommander>();
            if(commander && mothershipFleet)
            {
                if(mothershipFleet.IsSelected)
                {
                    cameraTravelTarget = mothershipFleet.transform;
                }
                commander.Select(mothershipFleet);
            }
        }
        float zoomDelta = Input.mouseScrollDelta.y;
        if (playerCamera && zoomDelta != 0)
        {
            targetZoom = Mathf.Clamp(targetZoom - zoomDelta * zoomSpeed, minZoomOut, maxZoomOut);
        }
        currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, 0.2f);
        playerCamera.transform.localPosition = -playerCamera.transform.forward * currentZoom;
    }

    public void MoveCameraTo(Transform target)
    {
        cameraTravelTarget = target;
    }

    void UpdateCameraMove()
    {
        Vector3 rawInputVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (cameraTravelTarget && rawInputVector.sqrMagnitude <= 0.01f)
        {
            if (cameraTravelTarget)
            {
                Vector3.SmoothDamp(transform.position, cameraTravelTarget.position, ref cameraVelocity, 0.3f);
                if(cameraVelocity.sqrMagnitude < 0.1f)
                {
                    cameraTravelTarget = null;
                }
            }
        }
        else
        {
            cameraTravelTarget = null;

            float cameraSpeed = Mathf.Lerp(minCameraSpeed, maxCameraSpeed, Mathf.InverseLerp(minZoomOut, maxZoomOut, currentZoom));
            cameraVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * cameraSpeed;
        }

        transform.position += cameraVelocity * Time.deltaTime;
    }
}
