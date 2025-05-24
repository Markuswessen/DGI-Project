using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TankController : MonoBehaviour
{
    public float m_Speed = 22f;
    public float m_TurnSpeed = 180f;
    public float m_WheelRotateSpeed = 90f;

    private Rigidbody m_Rigidbody; // Reference used to move the tank.
    private string m_MovementAxisName; // The name of the input axis for moving forward and back.
    private string m_TurnAxisName; // The name of the input axis for turning.
    private float m_MovementInputValue; // The current value of the movement input.
    private float m_TurnInputValue; // The current value of the turn input.
    private float rotationSpeed = 90f; // Degrees per second
    private Vector3 m_MouseInputValue; // The current value of the mouse input
    private int floorMask; // A layer mask so that a ray can be cast just at gameobjects on the floor layer.
    private bool isTransforming = false;
    private bool isTransformed = false;
    private List<Quaternion> targetWheelRotations = new List<Quaternion>();
    private List<Vector3> targetWheelPositions = new List<Vector3>();
    private List<GameObject> m_wheels = new List<GameObject>();
    private GameObject pivotWheel;
    private float pivotWheelStartingRotation;
    private List<GameObject> m_bodyWheelStructure = new List<GameObject>();
    private List<GameObject> m_body = new List<GameObject>();
    private List<GameObject> m_turretParts = new List<GameObject>();
    private List<GameObject> m_rwheels = new List<GameObject>();
    private List<GameObject> m_lwheels = new List<GameObject>();
    private List<Vector3> m_OriginalWheelPositions = new List<Vector3>();
    private List<Quaternion> m_OriginalWheelRotations = new List<Quaternion>();
    private Vector3 m_OriginalTurretPosition;
    private Quaternion m_OriginalTurretRotation;
    private float legSwingTimer = 0f;
    private float swingDuration = 0.5f; // Duration of one swing direction (seconds)
    private bool swingForward = true;

    private List<GameObject> m_tankParts = new List<GameObject>();

    private GameObject m_turret;
    private float camRayLength = 100f; // The length of the ray from the camera into the scene.

    private void Awake()
    {
        // Create a layer mask for the floor layer.
        floorMask = LayerMask.GetMask("Ground");

        m_Rigidbody = GetComponent<Rigidbody>();

        Transform[] children = GetComponentsInChildren<Transform>();
        // Adding the tank parts to the appropriate lists
        for (var i = 0; i < children.Length; i++)
        {
            if (children[i].name.Contains("rwheel1"))
            {
                pivotWheel = children[i].gameObject;
            }

            if (children[i].name.Contains("rwheel"))
            {
                m_tankParts.Add(children[i].gameObject);
                m_rwheels.Add(children[i].gameObject);
                m_wheels.Add(children[i].gameObject);
            }
            if (children[i].name.Contains("lwheel"))
            {
                m_tankParts.Add(children[i].gameObject);
                m_lwheels.Add(children[i].gameObject);
                m_wheels.Add(children[i].gameObject);
            }

            if (children[i].name.Contains("ORUGA"))
            {
                m_tankParts.Add(children[i].gameObject);
                m_bodyWheelStructure.Add(children[i].gameObject);
                if (children[i].name.Contains("1"))
                {
                    m_lwheels.Add(children[i].gameObject);
                }
                else
                {
                    m_rwheels.Add(children[i].gameObject);
                }
            }

            if (children[i].name.Contains("lateral"))
            {
                m_tankParts.Add(children[i].gameObject);
                m_bodyWheelStructure.Add(children[i].gameObject);
                if (children[i].name.Contains("ierdo"))
                {
                    m_lwheels.Add(children[i].gameObject);
                }
                else if (children[i].name.Contains("derecho"))
                {
                    m_rwheels.Add(children[i].gameObject);
                }
            }

            if ((children[i].name.Contains("carcasa")) || (children[i].name.Contains("CARCASA")))
            {
                m_tankParts.Add(children[i].gameObject);
                m_bodyWheelStructure.Add(children[i].gameObject);
                m_body.Add(children[i].gameObject);
            }

            if (children[i].name.Contains("trasera"))
            {
                m_tankParts.Add(children[i].gameObject);
                m_bodyWheelStructure.Add(children[i].gameObject);
                m_body.Add(children[i].gameObject);
            }

            if (children[i].name.Contains("Turret"))
            {
                m_turret = children[i].gameObject;

                Transform[] turretChildren = m_turret.GetComponentsInChildren<Transform>();

                foreach (Transform turretPart in turretChildren)
                {
                    m_turretParts.Add(turretPart.gameObject);
                }
            }
        }
    }
    // Start is called before the first frame update
    private void Start()
    {
        m_MovementAxisName = "Vertical";
        m_TurnAxisName = "Horizontal";
    }

    private void Update()
    {
        // Store the value of both input axes.
        m_MovementInputValue = Input.GetAxis(m_MovementAxisName);
        m_TurnInputValue = Input.GetAxis(m_TurnAxisName);
        m_MouseInputValue = Input.mousePosition;
        if (Input.GetKeyDown(KeyCode.T) && !isTransforming)
        {
            if (!isTransformed)
            {
                StartCoroutine(TransformToRobot());
            }
        }
    }

    private void FixedUpdate()
    {
        // Adjust the rigidbodies position and orientation in FixedUpdate.
        if (!isTransforming)
        {
            Move();
            Turn();
            RotateTurret();
            if (!isTransformed)
            {
                RotateWheels();
            }
            else
            {
                Walking();
            }
        }
    }

    private void OnEnable()
    {
        // When the tank is turned on, make sure it's not kinematic.
        m_Rigidbody.isKinematic = false;

        // Also reset the input values.
        m_MovementInputValue = 0f;
        m_TurnInputValue = 0f;
    }

    private void OnDisable()
    {
        // When the tank is turned off, set it to kinematic so it stops moving.
        m_Rigidbody.isKinematic = true;
    }

    private void Move()
    {
        // Create a vector in the direction the tank is facing with a magnitude based on the input, speed and the time between frames.
        Vector3 movement = transform.forward * m_MovementInputValue * Time.deltaTime;

        // Apply this movement to the rigidbody's position.
        m_Rigidbody.MovePosition(m_Rigidbody.position + movement);
    }

    private void Turn()
    {
        // Determine the number of degrees to be turned based on the input, speed and time between frames.
        float turn = m_TurnInputValue * m_TurnSpeed * Time.deltaTime;

        // Make this into a rotation in the y axis.
        Quaternion turnRotation = Quaternion.Euler(0f, turn, 0f);

        // Apply this rotation to the rigidbody's rotation.
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * turnRotation);
    }

    private void RotateWheels()
    {
        // Get the current input value for movement (forward: positive, backward: negative)
        float input = -m_MovementInputValue;

        // Determine rotation amount per frame, scaled by input direction and deltaTime
        float rotationAmount = m_WheelRotateSpeed * input * Time.deltaTime;

        // Construct a Quaternion representing rotation around the correct axis
        Quaternion turnRotation = Quaternion.Euler(rotationAmount, 0f, 0f); // Assuming wheels rotate around local X

        // Apply rotation to each wheel using quaternion multiplication
        for (int i = 0; i < m_wheels.Count; ++i)
        {
            m_wheels[i].transform.localRotation *= turnRotation;
        }
    }

    private void RotateTurret()
    {
        // Create a ray from the mouse cursor on screen in the direction of the camera
        Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        // Create a RaycastHit variable to store information about what was hit by the ray
        RaycastHit floorHit;

        // Perform the raycast and check if it hits something on the floor layer
        if (Physics.Raycast(camRay, out floorHit, camRayLength, floorMask))
        {
            //Get direction form the turret to the point on the floor that was hit by the ray
            Vector3 targetDir = floorHit.point - m_turret.transform.position;

            // Ensure the direction is only horizontal
            if (!isTransformed)
                targetDir.y = 0f;

            // If there's a direction to look toward
            if (targetDir != Vector3.zero)
            {
                // Create the rotation to look at the point
                Quaternion newRotation = Quaternion.LookRotation(targetDir);

                // Apply the rotation to the turret
                m_turret.transform.rotation = newRotation;
            }
        }
    }

    private void Walking()
    {
        float input = -m_MovementInputValue;
        // Only animate legs when there is input
        if (Mathf.Abs(input) < 0.01f)
            return;

        legSwingTimer += Time.deltaTime;

        if (legSwingTimer >= swingDuration)
        {
            // Toggle swing direction
            swingForward = swingForward ? false : true;
            legSwingTimer = 0f;
        }

        float rotationAmount = m_WheelRotateSpeed * input * Time.deltaTime;

        // Determine rotation direction based on swing state
        float rRotation = swingForward ? rotationAmount : -rotationAmount;
        float lRotation = -rRotation;

        for (int i = 0; i < m_rwheels.Count; ++i)
        {
            m_rwheels[i]
                .transform.RotateAround(
                    pivotWheel.transform.position,
                    pivotWheel.transform.right,
                    rRotation
                );

            m_lwheels[i]
                .transform.RotateAround(
                    pivotWheel.transform.position,
                    pivotWheel.transform.right,
                    lRotation
                );
        }
    }

    private IEnumerator TransformToRobot()
    {
        isTransforming = true;
        List<Quaternion> startRotations = new List<Quaternion>();
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> endPositions = new List<Vector3>();

        Transform[] tankChildren = GetComponentsInChildren<Transform>();
        // Store initial states for all parts
        foreach (var part in tankChildren) // Implement this to return wheels/body parts
        {
            startRotations.Add(part.transform.rotation);
            startPositions.Add(part.transform.position);
            endPositions.Add(part.transform.position + Vector3.up * 1f); // Move up 1 unit
        }

        // Move all parts up to their new positions
        float moveAmount = 1f; // seconds
        float moveElapsed = 0f;
        while (moveElapsed < moveAmount)
        {
            moveElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(moveElapsed / moveAmount);

            for (int i = 0; i < tankChildren.Length; i++)
            {
                tankChildren[i].transform.position = Vector3.Lerp(
                    startPositions[i],
                    endPositions[i],
                    progress
                );
            }
            yield return null;
        }

        // Reset all wheels to their original rotation
        foreach (GameObject part in m_wheels)
        {
            part.transform.localRotation = Quaternion.identity;
        }

        // Rotate the tank parts to their new positions
        while (true)
        {
            float maxAngle = 0f;
            float deltaAngle = rotationSpeed * Time.deltaTime;
            float targetAngle = 90f;
            Vector3 pivot = transform.position;
            Vector3 rotateVector = m_Rigidbody.rotation * Vector3.right;
            // Rotate all wheels
            foreach (GameObject part in m_tankParts)
            {
                // Get current rotation in Euler angles
                Vector3 currentEuler = part.transform.localEulerAngles;
                float currentX = Mathf.DeltaAngle(0, currentEuler.x);

                // Calculate remaining rotation needed
                float remaining = targetAngle - currentX;
                if (remaining <= 0)
                    continue;

                // Calculate actual rotation for this frame
                float actualRotation = Mathf.Min(deltaAngle, remaining);

                // Apply rotation around common pivot
                part.transform.RotateAround(pivot, rotateVector, -actualRotation);

                // Update maximum current angle
                maxAngle = Mathf.Max(maxAngle, currentX + actualRotation);
            }

            // Break when all wheels reach target
            if (maxAngle >= targetAngle - Mathf.Epsilon)
                break;

            yield return null;
        }

        List<Vector3> startPositions2 = new List<Vector3>();
        List<Vector3> endPositions2 = new List<Vector3>();

        // Get the start and end positions for the body parts
        foreach (var part in m_body)
        {
            startPositions2.Add(part.transform.position);
            endPositions2.Add(part.transform.position + Vector3.up * 1f);
        }

        float moveDuration2 = 1f; // seconds
        float moveElapsed2 = 0f;

        // Move all body parts up to their new positions
        while (moveElapsed2 < moveDuration2)
        {
            moveElapsed2 += Time.deltaTime;
            float t = Mathf.Clamp01(moveElapsed2 / moveDuration2);

            for (int i = 0; i < m_body.Count; i++)
            {
                m_body[i].transform.position = Vector3.Lerp(
                    startPositions2[i],
                    endPositions2[i],
                    t
                );
            }

            yield return null;
        }

        List<Vector3> turretStartPositions = new List<Vector3>();
        List<Vector3> turretEndPositions = new List<Vector3>();

        // Move the turret parts up
        foreach (var part in m_turretParts)
        {
            turretStartPositions.Add(part.transform.position);
            turretEndPositions.Add(part.transform.position + Vector3.up * 1.3f);
        }

        float moveDuration3 = 1f; // seconds
        float moveElapsed3 = 0f;

        // Move all turret parts up to their new positions
        while (moveElapsed3 < moveDuration3)
        {
            moveElapsed3 += Time.deltaTime;
            float t = Mathf.Clamp01(moveElapsed3 / moveDuration3);

            for (int i = 0; i < m_turretParts.Count; i++)
            {
                m_turretParts[i].transform.position = Vector3.Lerp(
                    turretStartPositions[i],
                    turretEndPositions[i],
                    t
                );
            }

            yield return null;
        }
        isTransforming = false;
        isTransformed = true;
        pivotWheelStartingRotation = pivotWheel.transform.localRotation.x;
    }
}
