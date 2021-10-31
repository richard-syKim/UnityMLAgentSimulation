using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class SelfishGeneEnvController : MonoBehaviour
{
    [System.Serializable]
    public class PlayerInfo
    {
        public SelfishGene Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
        [HideInInspector]
        public Collider Col;
    }

    /// <summary>
    /// Max Academy steps before this platform resets
    /// </summary>
    /// <returns></returns>
    [Header("Max Environment Steps")] public int MaxEnvironmentSteps = 25000;
    private int m_ResetTimer;

    /// <summary>
    /// The area bounds.
    /// </summary>
    [HideInInspector]
    public Bounds areaBounds;
    /// <summary>
    /// The ground. The bounds are used to spawn the elements.
    /// </summary>
    public GameObject ground;
    public GameObject hallway;
    public GameObject wall1;
    public GameObject wall2;
    public GameObject WallKey;
    public GameObject CoopKey;
    public GameObject SelfishKey;
    public bool KeyActivate = false;
    public bool FirstToKey = false;

    Material m_GroundMaterial; //cached on Awake()
    Material m_HallwayMaterial;

    /// <summary>
    /// We will be changing the ground material based on success/failue
    /// </summary>
    Renderer m_GroundRenderer;
    Renderer m_HallwayRenderer;

    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();
    private Dictionary<SelfishGene, PlayerInfo> m_PlayerDict = new Dictionary<SelfishGene, PlayerInfo>();
    public bool UseRandomAgentRotation = true;
    public bool UseRandomAgentPosition = true;
    PushBlockSettings m_PushBlockSettings;

    private int m_NumberOfRemainingPlayers;
    private SimpleMultiAgentGroup m_AgentGroup;
    void Start()
    {

        // Get the ground's bounds
        areaBounds = ground.GetComponent<Collider>().bounds;

        // Get the ground renderer so we can change the material when a goal is scored
        m_GroundRenderer = ground.GetComponent<Renderer>();
        m_HallwayRenderer = hallway.GetComponent<Renderer>();
        // Starting material
        m_GroundMaterial = m_GroundRenderer.material;
        m_HallwayMaterial = m_HallwayRenderer.material;
        m_PushBlockSettings = FindObjectOfType<PushBlockSettings>();

        //Reset Players Remaining
        m_NumberOfRemainingPlayers = AgentsList.Count;

        //Hide The Key
        KeyActivate = false;

        wall1.gameObject.SetActive(true);
        wall2.gameObject.SetActive(true);

        // Initialize TeamManager
        m_AgentGroup = new SimpleMultiAgentGroup();
        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            item.Col = item.Agent.GetComponent<Collider>();
            // Add to team manager
            m_AgentGroup.RegisterAgent(item.Agent);
        }

        ResetScene();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        m_ResetTimer += 1;
        if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
        {
            m_AgentGroup.GroupEpisodeInterrupted();
            ResetScene();
        }
    }

    public void SelfishButton(SelfishGene agent)
    {
        KeyActivate = true;
        m_NumberOfRemainingPlayers--;
        SelfishKey.gameObject.SetActive(false);
        WallKey.gameObject.SetActive(false);
        CoopKey.gameObject.SetActive(false);
        foreach (var item in AgentsList)
        {
            if (!GameObject.ReferenceEquals(item.Agent, agent))
            {
                item.Agent.gameObject.SetActive(false);
            }
        }

        if (m_NumberOfRemainingPlayers == 0)
        {
            m_AgentGroup.EndGroupEpisode();
            ResetScene();
        }
    }

    public void UnlockDoor()
    {
        foreach (var item in AgentsList)
        {
            if (!item.Agent)
            {
                item.Agent.SetReward(100f);
                Debug.Log("Reward from unlocking door");
            }
        }

        StartCoroutine(GoalScoredSwapGroundMaterial(m_PushBlockSettings.goalScoredMaterial, 0.5f));

        print("Unlocked Door");
        m_AgentGroup.EndGroupEpisode();

        ResetScene();
    }

    public void RemoveWall()
    {
        wall1.gameObject.SetActive(false);
        wall2.gameObject.SetActive(false);
        //StartCoroutine(TimeDelayWall(5f));
    }


    public void RemoveWallPermenant()
    {
        wall1.gameObject.SetActive(false);
        wall2.gameObject.SetActive(false);
    }


    /// <summary>
    /// Use the ground's bounds to pick a random spawn position.
    /// </summary>
    public Vector3 GetRandomSpawnPos()
    {
        var foundNewSpawnLocation = false;
        var randomSpawnPos = Vector3.zero;
        while (foundNewSpawnLocation == false)
        {
            var randomPosX = Random.Range(-areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.x * m_PushBlockSettings.spawnAreaMarginMultiplier);

            var randomPosZ = Random.Range(-areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier,
                areaBounds.extents.z * m_PushBlockSettings.spawnAreaMarginMultiplier);
            randomSpawnPos = ground.transform.position + new Vector3(randomPosX, 1f, randomPosZ);
            if (Physics.CheckBox(randomSpawnPos, new Vector3(2.5f, 0.01f, 2.5f)) == false)
            {
                foundNewSpawnLocation = true;
            }
        }
        return randomSpawnPos;
    }

    /// <summary>
    /// Swap ground material, wait time seconds, then swap back to the regular material.
    /// </summary>
    IEnumerator GoalScoredSwapGroundMaterial(Material mat, float time)
    {
        m_GroundRenderer.material = mat;
        m_HallwayRenderer.material = mat;
        yield return new WaitForSeconds(time); // Wait for 2 sec
        m_GroundRenderer.material = m_GroundMaterial;
        m_HallwayRenderer.material = m_HallwayMaterial;
    }

    IEnumerator DelayAction(float delayTime)
    {
        //Wait for the specified delay time before continuing.
        yield return new WaitForSeconds(delayTime);

        //Do the action after the delay time has finished.
    }

    IEnumerator TimeDelayWall(float time)
    {
        wall1.gameObject.SetActive(false);
        wall2.gameObject.SetActive(false);
        yield return new WaitForSeconds(time);
        if(KeyActivate == false)
        {
            wall1.gameObject.SetActive(true);
            wall2.gameObject.SetActive(true);
        }
        else
        {
            WallKey.gameObject.SetActive(false);
            CoopKey.gameObject.SetActive(false);
            /*
            foreach (var item in AgentsList)
            {
                if (!item.Agent)
                {
                    item.Agent.SetReward(0.75f);
                    Debug.Log("Reward to both from CoopGoal");
                }
            }
            */
        }
    }

    Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
    }

    void ResetScene()
    {

        //Reset counter
        m_ResetTimer = 0;

        //Reset Players Remaining
        m_NumberOfRemainingPlayers = AgentsList.Count;

        //Random platform rot
        var rotation = Random.Range(0, 4);
        var rotationAngle = rotation * 90f;
        transform.Rotate(new Vector3(0f, rotationAngle, 0f));

        //Reset Agents
        foreach (var item in AgentsList)
        {
            var pos = UseRandomAgentPosition ? GetRandomSpawnPos() : item.StartingPos;
            var rot = UseRandomAgentRotation ? GetRandomRot() : item.StartingRot;

            item.Agent.transform.SetPositionAndRotation(pos, rot);
            item.Rb.velocity = Vector3.zero;
            item.Rb.angularVelocity = Vector3.zero;
            item.Agent.IHaveAKey = false;
            item.Agent.gameObject.SetActive(true);
            m_AgentGroup.RegisterAgent(item.Agent);
        }

        //Reset Key
        KeyActivate = false;

        wall1.gameObject.SetActive(true);
        wall2.gameObject.SetActive(true);
        WallKey.gameObject.SetActive(true);
        CoopKey.gameObject.SetActive(true);
        SelfishKey.gameObject.SetActive(true);
    }
}
