using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using Unity.MLAgents;
using UnityEngine;
using Grpc.Core;

public class HideAndSeek_ppo : MonoBehaviour
{

    // Groups
    private SimpleMultiAgentGroup HideGroup;
    //private SimpleMultiAgentGroup SeekGroup;

    // Lists
    public List<Agent> Hiders;
    public SeekerAgent Seeker;

    // Max Steps
    public int max_steps = 2500;
    public int sleep_steps = 0;
    private int current_step = 0;
    private bool seekerActive = false;

    // Ray
    public RayPerceptionSensorComponent3D raySensor3d;

    // bool seekerWin = false;
    int leftHiders = 0;

    // Fake seeker
    // public GameObject fakeSeeker;
    // private bool fakeSeekerActive = false;

    // Last seen hider
    string lastSeenHider = "";

    // Start is called before the first frame update
    void Start()
    {
        HideGroup = new SimpleMultiAgentGroup();
        // SeekGroup = new SimpleMultiAgentGroup();

        foreach (HiderAgent agent in Hiders)
        {
            HideGroup.RegisterAgent(agent);
        }
/*        foreach (var seeker in Seekers)
        {
            SeekGroup.RegisterAgent(seeker);
        }*/

        ResetEnv();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        current_step++;
        //Debug.Log("Agent" + current_step);

        /*        foreach (HiderAgent hider in Hiders)
                {
                    Debug.Log(hider.startPos);
                }*/
/*        if (((current_step + 25) > sleep_steps) && !fakeSeekerActive)
        {
            fakeSeekerActive = true;
            fakeSeeker.SetActive(false);

        }*/
        // Sleep seeker at the start
        if ((current_step > sleep_steps) && !seekerActive)
        {
            seekerActive = true;
            Seeker.gameObject.SetActive(true);
            /*            foreach (var seeker in Seekers)
                        {
                            seeker.gameObject.SetActive(true);
                            SeekGroup.RegisterAgent(seeker);
                        }*/

        }
        if (seekerActive)
        {

            // Debug.Log(current_step);
            try
            {
                RayPerceptionSensor raySensor = raySensor3d.RaySensor;
                RayPerceptionOutput output = raySensor.RayPerceptionOutput;
                RayPerceptionOutput.RayOutput[] rays = output.RayOutputs;

                foreach (var observation in rays)
                {
                    bool found = false;
                    int tag = observation.HitTagIndex;
                    if (tag == 1)
                    {

                        GameObject HiderObj = observation.HitGameObject;
                        if ((HiderObj != null) && (HiderObj.name != lastSeenHider))
                        {
                            HiderAgent hider = HiderObj.GetComponent<HiderAgent>();
                            hider.calculateDistReward();
                            HiderObj.SetActive(false);
                            leftHiders--;
                            Seeker.AddReward(50.0f);
                            Debug.Log(Seeker.GetCumulativeReward());
                            HideGroup.AddGroupReward(-50.0f);
                            found = true;
                            lastSeenHider = HiderObj.name;
                            // Debug.Log("id " + lastSeenHider);
                            break;
                        }
                    }
                    if (found)
                    {
                        break;
                    }
                }
            }
            catch (NullReferenceException)
            {

            }


            
            if (leftHiders <= 0)
            {
                Seeker.EndEpisode();
                HideGroup.EndGroupEpisode();
                ResetEnv();
            }
        }
        if (current_step > max_steps)
        {
            foreach (HiderAgent hider in Hiders)
            {
                if (hider.gameObject.activeSelf == true)
                {
                    hider.calculateDistReward();
                }
            }
            float reward = leftHiders * 50.0f;
            // Debug.Log("Reward" + reward);
            Seeker.AddReward(reward * (-1));
            Seeker.EndEpisode();
            HideGroup.AddGroupReward(reward);
            HideGroup.EndGroupEpisode();
            ResetEnv();
        }
    }
    void ResetEnv()
    {
        current_step = 0;
        // seekerWin = false;
        leftHiders = Hiders.Count;
        // Debug.Log(leftHiders);
        lastSeenHider = "";

        seekerActive = false;
        // fakeSeekerActive = false;
        foreach (HiderAgent hider in Hiders)
        {
            hider.gameObject.SetActive(false);
            hider.orientation.localPosition = hider.startPos;
            // hider.orientation.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            hider.orientation.rotation = Quaternion.Euler(0f, 0f, 0f);
            hider.rBody.velocity = Vector3.zero;
            hider.rBody.angularVelocity = Vector3.zero;
            hider.gameObject.SetActive(true);
            HideGroup.RegisterAgent(hider);
        }

        Seeker.gameObject.SetActive(false);
        Seeker.orientation.localPosition = Seeker.startPos;
        Seeker.orientation.rotation = Quaternion.Euler(0f, 180f, 0f);
        Seeker.rBody.velocity = Vector3.zero;
        Seeker.rBody.angularVelocity = Vector3.zero;
        
        // fakeSeeker.SetActive(true);
    }
}