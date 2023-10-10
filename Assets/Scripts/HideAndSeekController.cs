using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class HideAndSeekController : MonoBehaviour
{

    // Groups
    private SimpleMultiAgentGroup HideGroup;
    private SimpleMultiAgentGroup SeekGroup;

    // Lists
    public List<Agent> Hiders;
    public List<Agent> Seekers;

    // Max Steps
    public int max_steps = 25000;
    public int sleep_steps = 10000;
    private int current_step = 0;
    private bool seekerActive = false;

    // Ray
    public RayPerceptionSensorComponent3D raySensor3d;

    bool seekerWin = false;

    // Start is called before the first frame update
    void Start()
    {
        HideGroup = new SimpleMultiAgentGroup();
        SeekGroup = new SimpleMultiAgentGroup();

        foreach (HiderAgent agent in Hiders)
        {
            HideGroup.RegisterAgent(agent);
        }
        foreach (var seeker in Seekers)
        {
            SeekGroup.RegisterAgent(seeker);
        }
        ResetEnv();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        current_step++;
        //Debug.Log("Agent" + current_step);

        /*        foreach (Agent seeker in Seekers)
                {
                    RayPerceptionOutput rayPerceptionOutput = new RayPerceptionOutput();

                    ReadOnlyCollection<float> observations = seeker.GetObservations();
                    int count = observations.Count;
                    Debug.Log(count);
                }*/

        // Sleep seeker at the start
        if ((current_step > sleep_steps) && !seekerActive)
        {
            seekerActive = true;
            foreach (var seeker in Seekers)
            {
                seeker.gameObject.SetActive(true);
                SeekGroup.RegisterAgent(seeker);
            }
            
            // Debug.Log("Start");
        }
        if (seekerActive)
        {
            
            // Debug.Log(current_step);
            foreach (var seeker in Seekers)
            {
                try
                {
                    RayPerceptionSensor raySensor = raySensor3d.RaySensor;
                    RayPerceptionOutput output = raySensor.RayPerceptionOutput;
                    RayPerceptionOutput.RayOutput[] rays = output.RayOutputs;

                    foreach (var observation in rays)
                    {
                        int tag = observation.HitTagIndex;
                        if (tag == 1)
                        {
                            seekerWin = true;
                            break;
                        }
                    }
                }
                catch (NullReferenceException) 
                {

                }


            }
            if (seekerWin)
            {
                SeekGroup.AddGroupReward(100.0f);
                SeekGroup.EndGroupEpisode();
                HideGroup.AddGroupReward(-100.0f);
                HideGroup.EndGroupEpisode();
                ResetEnv();
            }
        }
        if (current_step > max_steps)
        {
            SeekGroup.AddGroupReward(-100.0f);
            SeekGroup.EndGroupEpisode();
            HideGroup.AddGroupReward(100.0f);
            HideGroup.EndGroupEpisode();
            ResetEnv();
        }
    }
    void ResetEnv()
    {
        current_step = 0;
        seekerWin = false;

        seekerActive = false;
        foreach (HiderAgent hider in Hiders)
        {
            hider.gameObject.SetActive(false);
            hider.orientation.position = hider.startPos;
            hider.orientation.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            hider.rBody.velocity = Vector3.zero;
            hider.rBody.angularVelocity = Vector3.zero;
            hider.gameObject.SetActive(true);
            HideGroup.RegisterAgent(hider);
        }
        foreach (SeekerAgent seeker in Seekers)
        {
            seeker.gameObject.SetActive(false);
            seeker.orientation.position = seeker.startPos;
            seeker.orientation.rotation = Quaternion.Euler(0f, 180f, 0f);
            seeker.rBody.velocity = Vector3.zero;
            seeker.rBody.angularVelocity = Vector3.zero;
        }
    }
}


