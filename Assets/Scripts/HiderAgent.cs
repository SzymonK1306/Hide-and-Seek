using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using static UnityEngine.GraphicsBuffer;

public class HiderAgent : Agent
{

    public float moveSpeed;
    public Transform orientation;
    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    public Rigidbody rBody;
    private Collider agentCollider;
    private MeshRenderer meshRenderer;

/*    private bool wallCollision = false;
    private bool seekerCollision = false;

    private bool active = true;*/

    public Vector3 startPos;

    // Start is called before the first frame update
    void Start()
    {
        rBody = GetComponent<Rigidbody>();
        startPos = this.transform.localPosition;
        /*        agentCollider = GetComponent<Collider>();
                meshRenderer = GetComponent<MeshRenderer>();*/
        // rBody.freezeRotation = true;
    }

    // public Transform Target;
/*    public override void OnEpisodeBegin()
    {
        this.rBody.velocity = Vector3.zero;
        // Move the target to a new spot
        Vector3 startPosition = startPos;
        Quaternion randomRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        //rBody.enabled = true;
*//*        agentCollider.enabled = true;
        meshRenderer.enabled = true;*//*

        orientation.position = startPosition;
        orientation.rotation = randomRotation;
    }*/

    public override void CollectObservations(VectorSensor sensor)
    {
        // Position
        sensor.AddObservation(this.transform.localPosition / 25.0f);

        // Velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.z);

    }
    public override void OnActionReceived(ActionBuffers actions)
    {

        // Vector3 controlSignal = Vector3.zero;
        // controlSignal.x = actions.ContinuousActions[0];
        // controlSignal.z = actions.ContinuousActions[1];
        // rBody.AddForce(controlSignal * forceMultiplier);
        // Get the continuous actions.
        // Debug.Log(this.GetCumulativeReward());
        // Debug.Log(this.StepCount);

        float moveX = actions.ContinuousActions[0]; // Move left or right.
        float moveY = actions.ContinuousActions[1]; // Move forward or backward.
        float rotate = actions.ContinuousActions[2]; // Rotate clockwise or counterclockwise.

        // Calculate movement direction.
        moveDirection = transform.forward * moveY + transform.right * moveX;
            
        // Apply force to the Rigidbody for movement.
        rBody.AddForce(moveDirection.normalized * moveSpeed, ForceMode.Force);

        // Apply rotation to the agent.
        transform.Rotate(Vector3.up * rotate * Time.fixedDeltaTime * 100f);
  


    }

    void OnCollisionEnter(Collision collision)
    {
        // Punish for colliding walls
/*        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-1.0f);
        }*/
/*        if (collision.gameObject.CompareTag("Seeker"))
        {
            active = false;
            AddReward(-50.0f);
            //rBody.enabled = false;
            agentCollider.enabled = false;
            meshRenderer.enabled = false;
        }*/
    }  


    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;

        // Reset continuous actions.
        // continuousActions[0] = Input.GetAxis("Horizontal"); // Horizontal movement (A/D)
        // continuousActions[1] = Input.GetAxis("Vertical");   // Vertical movement (W/S)
        // continuousActions[2] = Input.GetAxis("Rotate");     // Rotation (E/Q)
        
        // Reset continuous actions.
        continuousActions[0] = 0f; // Horizontal movement (A/D)
        continuousActions[1] = 0f; // Vertical movement (W/S)
        continuousActions[2] = 0f; // Rotation (E/Q)

        // Check for button presses and set corresponding actions.
        if (Input.GetKey(KeyCode.U))
        {
            continuousActions[1] = 1f; // Move forward
        }
        else if (Input.GetKey(KeyCode.J))
        {
            continuousActions[1] = -1f; // Move backward
        }

        if (Input.GetKey(KeyCode.H))
        {
            continuousActions[0] = -1f; // Move left
        }
        else if (Input.GetKey(KeyCode.K))
        {
            continuousActions[0] = 1f; // Move right
        }

        if (Input.GetKey(KeyCode.Y))
        {
            continuousActions[2] = -1f; // Rotate counterclockwise
        }
        else if (Input.GetKey(KeyCode.I))
        {
            continuousActions[2] = 1f; // Rotate clockwise
        }

    }

    public void calculateDistReward()
    {
        float totalDistance = Vector3.Distance(this.transform.localPosition, startPos);
        Debug.Log(totalDistance);
        AddReward(totalDistance / 2f);
    }
}

