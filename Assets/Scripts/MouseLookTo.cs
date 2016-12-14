using UnityEngine;
using System.Collections;

public class MouseLookTo : MonoBehaviour
{

    public Vector3 targetPoint;

    public float distance = 10f;
    public float speed = 10;
    public float rotationSpeed = 30;
    private Vector3 prevMousePosition;
    private Transform camera;
    // Use this for initialization
    void Start()
    {
        camera = Camera.main.transform;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            float dY = (Input.mousePosition.y - prevMousePosition.y) / Screen.height;

            distance = Mathf.Clamp(distance + dY * speed, 5, 25);
            camera.forward = targetPoint - camera.position;
            camera.position = targetPoint + -camera.forward * distance;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            float dY = (Input.mousePosition.y - prevMousePosition.y) / Screen.height;
            float dX = -(Input.mousePosition.x - prevMousePosition.x) / Screen.width;

            camera.RotateAround(targetPoint, Vector3.up, dX * rotationSpeed);
            camera.RotateAround(targetPoint, Vector3.right, dY * rotationSpeed);
        }
        prevMousePosition = Input.mousePosition;
    }
}
