using Google.Protobuf.WellKnownTypes;
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

    // Start is called before the first frame update
    void Start()
    {
        HideGroup = new SimpleMultiAgentGroup();
        SeekGroup = new SimpleMultiAgentGroup();

        foreach (Agent agent in Hiders)
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
    void Update()
    {
        current_step++;
        // Debug.Log(current_step);

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
            }
            
            // Debug.Log("Start");
        }
        if (seekerActive)
        {
            bool seekerWin = false;
            // Debug.Log(current_step);
            foreach (var seeker in Seekers)
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

        seekerActive = false;
        foreach (var seeker in Seekers)
        {
            seeker.gameObject.SetActive(false);
        }
    }
}


