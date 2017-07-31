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

    Camera camera = null;

    bool returningToMotherShip = false;
    Mothership mothership = null;

    Vector3 cameraVelocity = Vector3.zero;

    private void Start()
    {
        camera = GetComponentInChildren<Camera>();
        if (camera)
        {
            currentZoom = targetZoom;
            camera.transform.position = camera.transform.forward * targetZoom;
        }
    }

    private void Update()
    {
        UpdateCameraMove();

        if(Input.GetKeyDown(KeyCode.Space))
        {
            mothership = FindObjectOfType<Mothership>();

            FleetCommander commander = GetComponent<FleetCommander>();
            if(commander && mothership)
            {
                Fleet mothershipFleet = mothership.GetComponentInParent<Fleet>();
                if(mothershipFleet.IsSelected)
                {
                    returningToMotherShip = true;
                }
                commander.Select(mothershipFleet);
            }
        }
        float zoomDelta = Input.mouseScrollDelta.y;
        if (camera && zoomDelta != 0)
        {
            targetZoom = Mathf.Clamp(targetZoom - zoomDelta * zoomSpeed, minZoomOut, maxZoomOut);
        }
        currentZoom = Mathf.SmoothDamp(currentZoom, targetZoom, ref zoomVelocity, 0.2f);
        camera.transform.localPosition = -camera.transform.forward * currentZoom;
    }

    void UpdateCameraMove()
    {
        Vector3 rawInputVector = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        if (returningToMotherShip && rawInputVector.sqrMagnitude <= 0.01f)
        {
            returningToMotherShip = false;
            if (mothership)
            {
                Vector3.SmoothDamp(transform.position, mothership.transform.position, ref cameraVelocity, 0.3f);
                returningToMotherShip = (transform.position - mothership.transform.position).sqrMagnitude > 1.0f;
            }
        }
        else
        {
            returningToMotherShip = false;
            float cameraSpeed = Mathf.Lerp(minCameraSpeed, maxCameraSpeed, Mathf.InverseLerp(minZoomOut, maxZoomOut, currentZoom));
            cameraVelocity = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")) * cameraSpeed;
        }

        transform.position += cameraVelocity * Time.deltaTime;
    }
}
