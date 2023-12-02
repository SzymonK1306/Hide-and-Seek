using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class SeekerAgent : Agent
{

    public float moveSpeed;
    public Transform orientation;

    Vector3 moveDirection;
    public Rigidbody rBody;
    
    // Params to calculate distance
    public Vector3 startPos;
    public Vector3 lastPos;
    public float distance_total;

    public LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
       rBody = GetComponent<Rigidbody>();
       startPos = orientation.localPosition;
       distance_total = 0;
       lastPos = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y, this.transform.localPosition.z);

        // Line renderer
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Set LineRenderer properties
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;
        lineRenderer.positionCount = 22;
    }

    /// <summary>
    /// Define observations (except lidar)
    /// </summary>
    public override void CollectObservations(VectorSensor sensor)
    {
        // Position
        sensor.AddObservation(this.transform.localPosition / 25.0f);

        // Velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);

    }
    /// <summary>
    /// Define actions
    /// </summary>
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0]; // Move left or right.
        float moveY = actions.ContinuousActions[1]; // Move forward or backward.
        float rotate = actions.ContinuousActions[2]; // Rotate clockwise or counterclockwise.

        // Calculate movement direction.
        moveDirection = transform.forward * moveY + transform.right * moveX;
            
        // Apply force to the Rigidbody for movement.
        rBody.AddForce(moveDirection.normalized * moveSpeed, ForceMode.Force);

        // Apply rotation to the agent.
        transform.Rotate(Vector3.up * rotate * Time.fixedDeltaTime * 100f);
        distance_total += Vector3.Distance(this.transform.localPosition, lastPos);
        AddReward(Vector3.Distance(this.transform.localPosition, lastPos) / 5);
        lastPos = new Vector3(this.transform.localPosition.x, this.transform.localPosition.y, this.transform.localPosition.z);
    }

    /// <summary>
    /// Heristic control of Agent - usefull in debug
    /// </summary>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        
        // Reset continuous actions.
        continuousActions[0] = 0f; // Horizontal movement (A/D)
        continuousActions[1] = 0f; // Vertical movement (W/S)
        continuousActions[2] = 0f; // Rotation (E/Q)

        // Check for button presses and set corresponding actions.
        if (Input.GetKey(KeyCode.W))
        {
            continuousActions[1] = 1f; // Move forward
        }
        else if (Input.GetKey(KeyCode.S))
        {
            continuousActions[1] = -1f; // Move backward
        }

        if (Input.GetKey(KeyCode.A))
        {
            continuousActions[0] = -1f; // Move left
        }
        else if (Input.GetKey(KeyCode.D))
        {
            continuousActions[0] = 1f; // Move right
        }

        if (Input.GetKey(KeyCode.Q))
        {
            continuousActions[2] = -1f; // Rotate counterclockwise
        }
        else if (Input.GetKey(KeyCode.E))
        {
            continuousActions[2] = 1f; // Rotate clockwise
        }

    }
}

