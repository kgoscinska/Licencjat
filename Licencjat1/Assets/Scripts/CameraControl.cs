using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindTarget = true;

    [Header("Rotation")]
    [SerializeField] private float rotateSpeed = 200f;
    [SerializeField] private float verticalRotationRange = 80f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 1000f;
    [SerializeField] private float minZoomDistance = 2f;
    [SerializeField] private float maxZoomDistance = 100f;

    [Header("Pan")]
    [SerializeField] private float panSpeed = 0.3f;
    [SerializeField] private KeyCode forwardKey = KeyCode.W;
    [SerializeField] private KeyCode backwardKey = KeyCode.S;
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;

    [Header("Smooth")]
    [SerializeField] private float smoothSpeed = 0.125f;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private float currentZoomDistance;

    private Camera cam;
    private Vector3 rotation = Vector3.zero;
    private Vector3 targetPosition;

    private void Start()
    {
        cam = GetComponent<Camera>();

        if (autoFindTarget)
        {
            BuildingGrid grid = FindObjectOfType<BuildingGrid>();
            if (grid != null)
            {
                GameObject cameraTargetObj = new GameObject("CameraTarget");
                cameraTargetObj.transform.position = grid.transform.position;
                target = cameraTargetObj.transform;
            }
            else
            {
                Debug.LogWarning("CameraControl: No BuildingGrid found. Assign target manually.");
            }
        }

        if (target == null)
        {
            Debug.LogError("CameraControl: Target is null!");
            enabled = false;
            return;
        }

        Vector3 offset = transform.position - target.position;
        rotation.y = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        rotation.x = Mathf.Asin(offset.y / offset.magnitude) * Mathf.Rad2Deg;
        xRotation = rotation.x;
        yRotation = rotation.y;

        currentZoomDistance = offset.magnitude;
    }

    private void LateUpdate()
    {
        HandleRotation();
        HandleZoom();
        HandlePan();

        UpdateCameraPosition();
    }

    private void HandleRotation()
    {
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;

            yRotation += mouseX;
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -verticalRotationRange, verticalRotationRange);
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            currentZoomDistance -= scroll * zoomSpeed * Time.deltaTime;
            currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
        }
    }

    private void HandlePan()
    {
        Vector3 input = Vector3.zero;

        if (Input.GetKey(forwardKey)) input.z += 1f;
        if (Input.GetKey(backwardKey)) input.z -= 1f;
        if (Input.GetKey(leftKey)) input.x -= 1f;
        if (Input.GetKey(rightKey)) input.x += 1f;

        if (input.magnitude > 0.1f)
        {
            Vector3 panDirection = transform.right * input.x + transform.forward * input.z;
            panDirection.y = 0f;
            panDirection.Normalize();

            target.position += panDirection * panSpeed;
        }

        if (Input.GetMouseButton(2))
        {
            float mouseX = -Input.GetAxis("Mouse X") * panSpeed * 0.03f;
            float mouseY = Input.GetAxis("Mouse Y") * panSpeed * 0.03f;

            Vector3 pan = transform.right * mouseX + transform.up * mouseY;
            target.position += pan;
        }
    }

    private void UpdateCameraPosition()
    {
        Quaternion targetRotation = Quaternion.Euler(xRotation, yRotation, 0f);
        Vector3 desiredPosition = target.position + targetRotation * Vector3.back * currentZoomDistance;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.LookAt(target.position);
    }

    public void FocusOn(Vector3 worldPoint)
    {
        target.position = worldPoint;
    }

    public void FocusOnBuilding(Building building)
    {
        if (building != null)
        {
            FocusOn(building.transform.position);
        }
    }
}