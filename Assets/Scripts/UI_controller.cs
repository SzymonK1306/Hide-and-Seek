using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using Unity.MLAgents;
using UnityEngine;
using Grpc.Core;
using Unity.VisualScripting;
using System.IO;
using Google.Protobuf.WellKnownTypes;
using System.Globalization;
using Unity.Barracuda;
using Unity.MLAgents.Policies;
using UnityEngine.UI;

public class UI_controller : MonoBehaviour
{

    public Toggle[] gameModeToggles;

    // Lists
    public List<Agent> Hiders;
    public List<SeekerAgent> Seekers;

    // Params
    public int max_steps = 2500;
    public int sleep_steps = 0;
    private int current_step = 0;
    private bool seekerActive = false;
    int leftHiders = 0;

    // Fake seeker
    public List<GameObject> fakeSeekers;
    private bool fakeSeekerActive = false;

    // Last seen hider
    string lastSeenHider = "";

    // Random possitions
    public List<GameObject> hiderPossitions = new List<GameObject>();

    // Simulation params
    public bool const_pos;
    public bool const_orientation;

    // Game active
    private bool gameActive = false;


    // Start is called before the first frame update
    void Start()
    {
        ResetEnv();
        gameActive = false;

    }

    public void StartGame()
    {
        gameActive = true;
        ResetEnv();
        activate_objestr(true, false, true);

    }

    public void ExitApplication()
    {
        // This function exits the application.
        #if UNITY_EDITOR
             UnityEditor.EditorApplication.isPlaying = false;
        #else
             Application.Quit();
        #endif
    }

    public void const_set()
    {
        if (gameModeToggles[0].isOn)
        {
            foreach (Toggle toggle in gameModeToggles)
            {
                if (toggle.name != "Const")
                {
                    toggle.isOn = false;
                }
            }
            const_pos = true;
            const_orientation = true;

            Model_change("const");
        }
    }
    public void orient_set()
    {
        if (gameModeToggles[1].isOn)
        {
            foreach (Toggle toggle in gameModeToggles)
            {
                if (toggle.name != "Orient")
                {
                    toggle.isOn = false;
                }
            }
            const_pos = true;
            const_orientation = false;

            Model_change("orient_change");

        }
    }
    public void pos_set()
    {
        if (gameModeToggles[2].isOn)
        {
            foreach (Toggle toggle in gameModeToggles)
            {
                if (toggle.name != "Pos")
                {
                    toggle.isOn = false;
                }
            }
            const_pos = false;
            const_orientation = true;
        }
        Model_change("pos_change");

    }

    private void Model_change(string dir)
    {
        foreach (Agent agent in Hiders)
        {
            BehaviorParameters behaviorParameters = agent.GetComponent<BehaviorParameters>();
            string path = $"Models/{dir}/HiderBehavior";
            if (behaviorParameters != null)
            {
                behaviorParameters.Model = Resources.Load<NNModel>(path);
            }
        }
        foreach (Agent agent in Seekers)
        {
            BehaviorParameters behaviorParameters = agent.GetComponent<BehaviorParameters>();
            string path = $"Models/{dir}/SeekerBehavior";
            if (behaviorParameters != null)
            {
                behaviorParameters.Model = Resources.Load<NNModel>(path);
            }
        }
    }


    // Update is called once per frame
    void FixedUpdate()
    {
        if (gameActive)
        {
            current_step++;

            // Delete fake seekers
            if (((current_step + 5) > sleep_steps) && !fakeSeekerActive)
            {
                fakeSeekerActive = true;
                foreach (GameObject fakeSeeker in fakeSeekers)
                {
                    fakeSeeker.SetActive(false);
                }

            }


            // Sleep seeker at the start
            if ((current_step > sleep_steps) && !seekerActive)
            {
                seekerActive = true;
                foreach (SeekerAgent Seeker in Seekers)
                {
                    Seeker.gameObject.SetActive(true);
                }

            }

            // Seek faze started
            if (seekerActive)
            {
                float currentReward = 0f;
                foreach (SeekerAgent Seeker in Seekers)
                {
                    // Check if any hider found
                    try
                    {
                        Transform lidar_transform = Seeker.transform.Find("Lidar");

                        // Get information from sensor
                        GameObject lidar = lidar_transform.gameObject;

                        RayPerceptionSensorComponent3D raySensor3d = lidar.GetComponent<RayPerceptionSensorComponent3D>();
                        RayPerceptionSensor raySensor = raySensor3d.RaySensor;
                        RayPerceptionOutput output = raySensor.RayPerceptionOutput;
                        RayPerceptionOutput.RayOutput[] rays = output.RayOutputs;

                        DrawMultipleLines(Seeker.lineRenderer, lidar);
                        foreach (var observation in rays)
                        {
                            bool found = false;
                            int tag = observation.HitTagIndex;
                            if (tag == 1)
                            {
                                // Found hider actions
                                GameObject HiderObj = observation.HitGameObject;
                                if ((HiderObj != null) && (HiderObj.name != lastSeenHider))
                                {
                                    // Eliminate hider
                                    HiderAgent hider = HiderObj.GetComponent<HiderAgent>();
                                    hider.calculateDistReward();
                                    HiderObj.SetActive(false);
                                    leftHiders--;

                                    // Rewards
                                    currentReward += 50f;
                                    found = true;
                                    lastSeenHider = HiderObj.name;
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
                }

                foreach (SeekerAgent Seeker in Seekers)
                {
                    Seeker.AddReward(currentReward);
                }

                // Seeker wins
                if (leftHiders <= 0)
                {
                    foreach (SeekerAgent Seeker in Seekers)
                    {
                        Seeker.EndEpisode();
                    }

                    gameActive = false;
                }
            }

            foreach (HiderAgent Hider in Hiders)
            {
                Transform lidar_transform = Hider.transform.Find("Lidar");

                // Get information from sensor
                GameObject lidar = lidar_transform.gameObject;
                DrawMultipleLines(Hider.lineRenderer, lidar);
            }

            // Hiders win
            if (current_step > max_steps)
            {
                foreach (HiderAgent hider in Hiders)
                {
                    if (hider.gameObject.activeSelf == true)
                    {
                        hider.calculateDistReward();    // rewards for distance
                    }
                }

                float reward = leftHiders * 50.0f;

                foreach (SeekerAgent Seeker in Seekers)
                {
                    Seeker.AddReward(reward * (-1));
                    Seeker.EndEpisode();
                }


                gameActive = false;
            }
        }
        else
        {
            activate_objestr(false, false, false);
        }
        
    }
    void activate_objestr(bool active_hiders, bool active_seekers, bool active_fake)
    {
        foreach (HiderAgent hider in Hiders)
        {
            hider.gameObject.SetActive(active_hiders);
        }
        foreach (SeekerAgent seeker in Seekers)
        {
            seeker.gameObject.SetActive(active_seekers);
        }
        foreach (GameObject fakeSeeker in fakeSeekers)
        {
            fakeSeeker.SetActive(active_fake);
        }
    }
    /// <summary>
    /// Draw rays in game mode
    /// </summary>
    void DrawMultipleLines(LineRenderer lineRenderer, GameObject lidar)
    {
        // animate rays
        RayPerceptionSensorComponent3D raySensor3d = lidar.GetComponent<RayPerceptionSensorComponent3D>();

        var input = raySensor3d.GetRayPerceptionInput();
        var outputs = RayPerceptionSensor.Perceive(input);

        for (var rayIndex = 0; rayIndex < outputs.RayOutputs.Length; rayIndex++)
        {
            var extents = input.RayExtents(rayIndex);
            Vector3 startPositionWorld = extents.StartPositionWorld;
            Vector3 endPositionWorld = extents.EndPositionWorld;
            var rayOutput = outputs.RayOutputs[rayIndex];
            if ((rayOutput.HasHit))
            {

                endPositionWorld = new Vector3((endPositionWorld.x - startPositionWorld.x) * rayOutput.HitFraction + startPositionWorld.x,
                    (endPositionWorld.y - startPositionWorld.y) * rayOutput.HitFraction + startPositionWorld.y,
                    (endPositionWorld.z - startPositionWorld.z) * rayOutput.HitFraction + startPositionWorld.z);
                lineRenderer.SetPosition(rayIndex * 2, startPositionWorld);
                lineRenderer.SetPosition(rayIndex * 2 + 1, endPositionWorld);
            }

        }
    }

    /// <summary>
    /// Generate initial conditions in env
    /// </summary>
    void ResetEnv()
    {
        current_step = 0;
        // Initial vlaues
        leftHiders = Hiders.Count;
        lastSeenHider = "";
        seekerActive = false;
        fakeSeekerActive = false;

        // Randomise possition
        List<Vector3> hiderNewPos = GetRandomPositions();
        foreach (HiderAgent hider in Hiders)
        {
            int currentIndex = Hiders.IndexOf(hider);
            hider.gameObject.SetActive(false);
            if (const_pos)
            {
                hider.orientation.localPosition = hider.startPos;
            }
            else
            {
                hider.orientation.localPosition = hiderNewPos[currentIndex];
            }
            hider.maxDistance = 0f;
            if (const_orientation)
            {
                hider.orientation.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                hider.orientation.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            }

            hider.rBody.velocity = Vector3.zero;
            hider.rBody.angularVelocity = Vector3.zero;
            // hider.gameObject.SetActive(true);
        }

        List<Vector3> trans = new List<Vector3>();
        foreach (SeekerAgent Seeker in Seekers)
        {
            Vector3 pos = Seeker.startPos;
            Seeker.gameObject.SetActive(false);
            Seeker.orientation.localPosition = pos;
            trans.Add(pos);
            if (const_orientation)
            {
                Seeker.orientation.rotation = Quaternion.Euler(0f, 180.0f, 0f);
            }
            else
            {
                Seeker.orientation.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
            }
            Seeker.rBody.velocity = Vector3.zero;
            Seeker.rBody.angularVelocity = Vector3.zero;
            Seeker.gameObject.SetActive(false);
        }

        foreach (GameObject fakeSeeker in fakeSeekers)
        {
            int idx = fakeSeekers.IndexOf(fakeSeeker);
            fakeSeeker.transform.localPosition = trans[idx];
            fakeSeeker.SetActive(true);
        }

    }

    /// <summary>
    /// Choose random possition for Hiders from list of points
    /// </summary>
    public List<Vector3> GetRandomPositions()
    {
        List<Vector3> randomPositions = new List<Vector3>();

        System.Random random = new System.Random();

        GameObject obj1, obj2;

        do
        {
            obj1 = hiderPossitions[random.Next(hiderPossitions.Count)];
            obj2 = hiderPossitions[random.Next(hiderPossitions.Count)];
        } while (obj1.GetInstanceID() == obj2.GetInstanceID());

        randomPositions.Add(obj1.transform.localPosition);
        randomPositions.Add(obj2.transform.localPosition);


        return randomPositions;
    }

}
