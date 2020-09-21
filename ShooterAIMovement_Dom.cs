using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum Objectivesearchtype
{
    Closest,
    Random,
    Guard
}

public class ShooterAIMovement_Dom : MonoBehaviour
{
    [Header("Objectives")]
    public bool CTF = false;
    public string ObjectiveTag = "Objective";
    public string Guardtag;
    public float Capture_range;
    public float DistanceToObjective;
    public GameObject Objective, self;
    public float objListUpdateRate = 1.0f;
    [HideInInspector] public float objListUpdateTimer = 0.0f;
    private GameObject[] ObjectivesList;
    private List<GameObject> AvailableObjectives = new List<GameObject>();
    public bool randombehaviour, randomjobs;

    [Header("Animations")]
    public GameObject Character;
    public GameObject Torso;
    public GameObject[] gadgetite;

    [Header("Timers")]
    private float timer;
    private float wanderTimer;

    [Header("VehicleInteractions")]
    public bool recentlydeparted = false;
    private float departureTimer;
    float departureInterval;

    [Header("Wander")]
    public float wanderRadius;

    [Header("Components")]
    public Objectivesearchtype ost;
    public Job job;
    public NavMeshAgent agent;
    Animator anim2;
    [HideInInspector] public Animator anim;
    [SerializeField] ShooterAIAttack Atk;
    [SerializeField] Health Hth;
    public Allyscanner AS;

    [Header("Finding Medkits")]
    public bool findingmedkits = false;
    private GameObject[] medkitslist;
    private List<GameObject> AvailableMedkits = new List<GameObject>();
    public GameObject medkit;

    [Header("Finding MGs")]
    public bool FindingMGs = false;
    private GameObject[] MGlist;
    private List<GameObject> AvailableMGs = new List<GameObject>();
    public GameObject MG;

    public bool navigatingToMG = false;


    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = Character.GetComponent<Animator>();
        anim2 = Torso.GetComponent<Animator>();

        if (randombehaviour)
        {
            ost = (Objectivesearchtype)Random.Range(0, 3);
        }

        if (CTF)
        {
            if (ost != Objectivesearchtype.Guard)
            {
                Capture_range = 2;
            }

            if (ost == Objectivesearchtype.Guard)
            {
                Capture_range = Random.Range(4, 8);
            }
        }
        if (!CTF)
        {
            Capture_range = Random.Range(4, 8);
        }

        job = (Job)Random.Range(0, 8);

        if (job == Job.Assault)
        {
            gadgetite[0].SetActive(true);
        }
        if (job == Job.Grenadier)
        {
            gadgetite[1].SetActive(true);
        }
        if (job == Job.Engineer)
        {
            gadgetite[2].SetActive(true);
        }
        if (job == Job.Landminer)
        {
            gadgetite[3].SetActive(true);
        }
        if (job == Job.Smoker)
        {
            gadgetite[4].SetActive(true);
        }
        if (job == Job.Inhibitor)
        {
            gadgetite[5].SetActive(true);
        }
        if (job == Job.Knight)
        {
            gadgetite[6].SetActive(true);
        }
        if (job == Job.Armor)
        {
            gadgetite[7].SetActive(true);
        }

        departureInterval = Random.Range(10, 60);
        agent.Warp(transform.position);
        findingmedkits = false;
        //GC = Character.GetComponent<GroundCheck>();

    }

    public void Respawn()
    {
        for (int i = 0; i < gadgetite.Length; i++)
        {
            gadgetite[i].SetActive(false);
        }

        Atk.FOV.viewAngle = 120;
        ResetMgList();
        findingmedkits = false;
        Objective = null;
        AvailableObjectives.Clear();
        ObjectivesList = null;
        objListUpdateTimer = 0;

        agent = GetComponent<NavMeshAgent>();
        agent.Warp(transform.position);
        agent.isStopped = true;

        if (randombehaviour)
        {
            ost = (Objectivesearchtype)Random.Range(0, 3);
        }

        if (CTF)
        {
            if (ost != Objectivesearchtype.Guard)
            {
                Capture_range = 2;
            }

            if (ost == Objectivesearchtype.Guard)
            {
                Capture_range = Random.Range(4, 8);
            }
        }
        if (!CTF)
        {
            Capture_range = Random.Range(4, 8);
        }

        if (randomjobs)
        {
            job = (Job)Random.Range(0, 8);
        }

        if (job == Job.Assault)
        {
            gadgetite[0].SetActive(true);
        }
        if (job == Job.Grenadier)
        {
            gadgetite[1].SetActive(true);
        }
        if (job == Job.Engineer)
        {
            gadgetite[2].SetActive(true);
        }
        if (job == Job.Landminer)
        {
            gadgetite[3].SetActive(true);
        }
        if (job == Job.Smoker)
        {
            gadgetite[4].SetActive(true);
        }
        if (job == Job.Inhibitor)
        {
            gadgetite[5].SetActive(true);
        }
        if (job == Job.Knight)
        {
            gadgetite[6].SetActive(true);
        }
        if (job == Job.Armor)
        {
            gadgetite[7].SetActive(true);
        }

    }

    private void FixedUpdate()
    {
        PersistentActions();
        ObjectAnimator();
    }

    void PersistentActions()
    {
        if (Pausegame.IsOn)
        {
            return;
        }
        if (Objective)
        {
            DistanceToObjective = (Objective.transform.position - transform.position).magnitude;
        }
        if (!Character)
        {
            Debug.LogWarning("Missing Character!!!");
            Destroy(gameObject);
        }
        if (navigatingToMG)
        {
            if ((MG.transform.position - transform.position).magnitude < 20)
            {
                agent.SetDestination(MG.transform.position);
            }
            else
            {
                ResetMgList();
            }
        }

        objListUpdateTimer += Time.deltaTime;
        timer += Time.deltaTime;

        if (recentlydeparted)
        {
            departureTimer += Time.deltaTime;
        }
        if(departureTimer > departureInterval)
        {
            departureTimer = 0;
            recentlydeparted = false;
        }
    }

    void ObjectAnimator()
    {
        if (anim != null && anim2 != null && !Hth.dead && agent.isOnNavMesh)
        {
            if (!Atk.Alert)
            {
                anim.SetBool("Alert", false);
                anim2.SetBool("Alert", false);
            }

            if (Atk.Alert && !Atk.Shooting)
            {
                anim.SetBool("Alert", true);
                anim2.SetBool("Alert", true);

            }

            if (Atk.Shooting && !Atk.closerange)
            {
                anim.SetBool("Shooting", true);
            }

            if (!Atk.Shooting)
            {
                anim.SetBool("Shooting", false);
            }

            if (agent.isStopped == true && !Hth.dead && agent.isOnNavMesh)
            {
                anim.SetBool("Moving", false);
                anim2.SetBool("Moving", false);
            }

            if (agent.isStopped == false && !Atk.closerange && !Atk.Alert && !Hth.dead && agent.isOnNavMesh)
            {
                anim2.SetBool("Moving", true);
            }

            if (agent.isStopped == false && !Hth.dead && agent.isOnNavMesh)
            {
                anim.SetBool("Moving", true);
            }

            if (Atk.closerange)
            {
                anim.SetBool("Moving", false);
                anim2.SetBool("Moving", false);
            }

            if (Atk.meleeattacking)
            {
                anim2.Play(Atk.awf.weaponname + "Melee", 0, 0f);

            }

        }
    }

    // Update is called once per frame
    void Update()
    {
    
        if (Objective && Objective.activeInHierarchy == false)
        {
            Objective = null;
            UpdateobjList();
        }

        if (objListUpdateTimer >= objListUpdateRate)
        {
            if (Hth.currentHealth < 65 && !findingmedkits)
            {
                ResetMgList();
                Findnearesthealth();
            }
            if (ost == Objectivesearchtype.Guard && !findingmedkits && !FindingMGs)
            {
                Invoke(nameof(FindMG), 1);
            }
            if (!Objective && !findingmedkits && !FindingMGs)
            {
                UpdateobjList();
            }
        }

        if(medkit && (Character.transform.position - medkit.transform.position).magnitude < 2 && findingmedkits)
        {
            if(job != Job.Armor)
            {
                Hth.ChangeHealth(40);
            }
            else
            {
                Hth.ChangeHealth(28);
            }

            findingmedkits = false;
            medkit.SetActive(false);
            medkit = null;
            AvailableMedkits.Clear();
            medkitslist = null;

            UpdateobjList();
        }
        if(medkit && !medkit.activeInHierarchy)
        {
            findingmedkits = false;
            medkit = null;
            AvailableMedkits.Clear();
            medkitslist = null;

            UpdateobjList();
        }
        if ( MG && (Character.transform.position - MG.transform.position).magnitude <= 3 && FindingMGs && !recentlydeparted)
        {
            if (Atk.team == "Ally")
            {
                MGturretUse mgtu = MG.GetComponent<MGturretUse>();
                GameObject parentobj = transform.parent.gameObject;

                agent.Warp(parentobj.transform.position);

                Character.transform.position = transform.position;
                Character.transform.rotation = mgtu.gameObject.transform.rotation;

                mgtu.cachedobj = parentobj;
                mgtu.Use("Ally");
                parentobj.SetActive(false);
            }
            if (Atk.team == "Enemy")
            {
                MGturretUse mgtu = MG.GetComponent<MGturretUse>();
                GameObject parentobj = transform.parent.gameObject;

                agent.Warp(parentobj.transform.position);

                Character.transform.position = transform.position;
                Character.transform.rotation = mgtu.gameObject.transform.rotation;

                mgtu.cachedobj = parentobj;
                mgtu.Use("Enemy");
                parentobj.SetActive(false);
            }
        }

        if (MG)
        {
            MGturretUse mgt = MG.GetComponent<MGturretUse>();
            if (mgt.isbeingused)
            {
                ResetMgList();
            }
        }

        if (Atk.FOV.visibleTargets.Count > 3 && !FindingMGs && !findingmedkits && Atk.behaviour != AIBehaviour.Objpriority && !recentlydeparted)
        {
            FindMG();
        }
        if(DistanceToObjective < Capture_range && AS.nearbyallies.Count > 2 && !FindingMGs && !findingmedkits && Atk.behaviour != AIBehaviour.Objpriority && Atk.behaviour != AIBehaviour.Berserk && !recentlydeparted)
        {
            Invoke(nameof(FindMG), 1);
        }

        if (!Objective || !Atk.Shooting || DistanceToObjective < Capture_range && !findingmedkits && !FindingMGs) 
        {
            PeacefulAction();

            if (job == Job.Landminer && !Atk.abilityused)
            {
                Atk.abilityused = true;
                Atk.Ability();
            }
        }

        if (Character)
        {
            Character.transform.rotation = transform.rotation;
        }

        if (job == Job.Assault && !Atk.abilityused && AS.nearbyallies.Count >= 1)
        {
            Health h = AS.nearbyallies[Random.Range(0, AS.nearbyallies.Count)].GetComponentInParent<Health>();
            PlayerHealth ph = AS.nearbyallies[Random.Range(0, AS.nearbyallies.Count)].GetComponentInParent<PlayerHealth>();

            if (h && h.currentHealth < 50)
            {
                Atk.abilityused = true;
                Atk.Ability();
            }
            else if(ph && ph.currentHealth < 50)
            {
                Atk.abilityused = true;
                Atk.Ability();

            }
        }
    }

    void PeacefulAction()
    {
        
        if (!Objective && !Hth.dead && !findingmedkits && !FindingMGs)
        {
            Wander();
        }

        if (DistanceToObjective < Capture_range && !Hth.dead && !findingmedkits && !FindingMGs)
        {
            Wander();
        }
        

        if (Objective && DistanceToObjective > Capture_range && !findingmedkits && !FindingMGs)
        {
            Invoke(nameof(MoveToObjective), Random.Range(1, 5));
        }

       if (AS.nearbyallies.Count >= Random.Range(3, 5) && ost == Objectivesearchtype.Guard && !FindingMGs)
        {
            ResetMgList();
            ost = (Objectivesearchtype)Random.Range(0, 2);
            Objective = null;
            UpdateobjList();
        }

        if (job == Job.Inhibitor && AS.nearbyallies.Count > 3 && Atk.FOV.visibleTargets.Count > 1 && !Atk.abilityused)
        {
            Atk.abilityused = true;
            Atk.Ability();
        }

        if (ost == Objectivesearchtype.Random && !Objective)
        {
            Objective = null;
            UpdateobjList();
        }

        if (!findingmedkits && !FindingMGs )
        {
            Invoke(nameof(FindMG), 1);
        }


    }

    void MoveToObjective()
    {

        if (Objective && !Hth.dead && !Hth.dead && agent.isOnNavMesh)
        {

            if (DistanceToObjective > Capture_range && !findingmedkits && !FindingMGs && !Hth.dead && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(Objective.transform.position);
            }

            if (DistanceToObjective <= Capture_range)
            {
                if(!findingmedkits && !FindingMGs && !Hth.dead && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                    agent.isStopped = true;
                }

                if (job == Job.Engineer && !Atk.abilityused)
                {
                    Atk.abilityused = true;
                    Atk.Ability();
                }

                if (job == Job.Smoker && AS.nearbyallies.Count > 4 &&!Atk.abilityused)
                {
                    Atk.abilityused = true;
                    Atk.Ability();
                }

            }

        }

        if (!Objective && !Atk.Alert && !Hth.dead && !findingmedkits && !FindingMGs)
        {
            agent.ResetPath();
            agent.isStopped = true;
            PeacefulAction();
        }
    }

    void Wander()
    {
        if(!Hth.dead && agent.isOnNavMesh)
        {
        if(FindingMGs)
        {
                ResetMgList();
        }

        Vector3 newPos = RandomNavSphere(transform.position, wanderRadius, -1);
        float dist = agent.remainingDistance;

        if (timer >= wanderTimer && !Hth.dead && agent.isOnNavMesh)
        {
            agent.SetDestination(newPos);
            if (transform.position != newPos)
            {
                agent.isStopped = false;
            }
 
            timer = 0;
            wanderTimer = Random.Range(5, 10);
        }

        if (dist != 99999 && agent.pathStatus == NavMeshPathStatus.PathComplete && agent.remainingDistance <= 0.5 && !Hth.dead && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        if (Objective && DistanceToObjective > Capture_range && !findingmedkits && !FindingMGs && !Hth.dead && agent.isOnNavMesh)
        {
            MoveToObjective();
        }

        }
        return;
    }

    void ResetMgList()
    {
        navigatingToMG = false;
        FindingMGs = false;
        MG = null;
        AvailableMGs.Clear();
        MGlist = null;

        UpdateobjList();
        return;
    }

    void UpdateobjList()
    {
        AvailableObjectives.Clear();

        #region Closest
        if (ost == Objectivesearchtype.Closest)
        {
            ObjectivesList = GameObject.FindGameObjectsWithTag(ObjectiveTag);
            for (int i = 0; i < ObjectivesList.Length; i++)
            {
                AvailableObjectives.Add(ObjectivesList[i]);
            }

                objListUpdateTimer = 0.0f;

            float distanceToClosestObjective = 99999;
            GameObject ClosestObjective = null;

            foreach (GameObject CurrentObjective in AvailableObjectives)
            {
                float distancetoObjective = (CurrentObjective.transform.position - transform.position).magnitude;
                if (distancetoObjective < distanceToClosestObjective)
                {
                    distanceToClosestObjective = distancetoObjective;
                    ClosestObjective = CurrentObjective;
                }
            }

            if (ClosestObjective != null)
            {
                Debug.DrawLine(transform.position, ClosestObjective.transform.position, Color.magenta);
            }

            Objective = ClosestObjective;
        }
        #endregion

        #region Random
        if (ost == Objectivesearchtype.Random)
        {
            ObjectivesList = GameObject.FindGameObjectsWithTag(ObjectiveTag);
            objListUpdateTimer = 0.0f;

            if (!Objective && ObjectivesList.Length != 0 )
            {
                Objective = ObjectivesList[(Random.Range(0, ObjectivesList.Length))];
            }
        }
        #endregion

        #region Guard
        if (ost == Objectivesearchtype.Guard)
        {
            ObjectivesList = GameObject.FindGameObjectsWithTag(Guardtag);
            for (int i = 0; i < ObjectivesList.Length; i++)
            {
                AvailableObjectives.Add(ObjectivesList[i]);            }

            objListUpdateTimer = 0.0f;

            float distanceToClosestObjective = 99999;
            GameObject ClosestObjective = null;

            foreach (GameObject CurrentObjective in AvailableObjectives)
            {
                float distancetoObjective = (CurrentObjective.transform.position - transform.position).magnitude;
                if (distancetoObjective < distanceToClosestObjective)
                {
                    distanceToClosestObjective = distancetoObjective;
                    ClosestObjective = CurrentObjective;
                }
            }

            if (ClosestObjective != null)
            {
                Debug.DrawLine(transform.position, ClosestObjective.transform.position, Color.magenta);
            }

            Objective = ClosestObjective;
        }

        #endregion

    }

    void Findnearesthealth()
    {
        medkitslist = GameObject.FindGameObjectsWithTag("Medkit");
            for (int i = 0; i < medkitslist.Length; i++)
            {
                AvailableMedkits.Add(medkitslist[i]);
            }

            float distanceToClosestMedkit = 99999;
            GameObject ClosestMedkit = null;

            foreach (GameObject CurrentMedkit in AvailableMedkits)
            {
                float distancetoMedkit = (CurrentMedkit.transform.position - transform.position).magnitude;
                if (distancetoMedkit < distanceToClosestMedkit)
                {
                    distanceToClosestMedkit = distancetoMedkit;
                    ClosestMedkit = CurrentMedkit;
                }
            }

            medkit = ClosestMedkit;

            if (ClosestMedkit != null)
            {
                Debug.DrawLine(transform.position, ClosestMedkit.transform.position, Color.magenta);
            }

        if(medkit && (medkit.transform.position - transform.position).magnitude < 20 && !Hth.dead)
        {
            Objective = null;

            findingmedkits = true;
            agent.isStopped = false;
            agent.SetDestination(medkit.transform.position);
        }
        else
        {
            findingmedkits = false;
        }
    }

    void FindMG()
    {
            MGlist = GameObject.FindGameObjectsWithTag("Machinegun");
            for (int i = 0; i < MGlist.Length; i++)
            {
                AvailableMGs.Add(MGlist[i]);
            }

            float distanceToClosestMG = 99999;
            GameObject ClosestMG = null;

            foreach (GameObject CurrentMG in AvailableMGs)
            {
                float distancetoMG = (CurrentMG.transform.position - transform.position).magnitude;
                if (distancetoMG < distanceToClosestMG)
                {
                    distanceToClosestMG = distancetoMG;
                    ClosestMG = CurrentMG;
                }
            }

            MG = ClosestMG;

        if (MG && !MG.GetComponent<MGturretUse>().isbeingused && (MG.transform.position - transform.position).magnitude < 20 && !Hth.dead && !recentlydeparted)
        {
            Debug.DrawLine(transform.position, MG.transform.position, Color.red);
            Objective = null;
            FindingMGs = true;
            agent.isStopped = false;
            agent.SetDestination(MG.transform.position);

            navigatingToMG = true;
        }
        else
        {
            int x = Random.Range(0, 1);

            if (x == 0)
            {
                ResetMgList();
            }
            else
            {
                FindMG();
            }
        }
    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layermask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;

        randDirection += origin;

        NavMeshHit navHit;

        NavMesh.SamplePosition(randDirection, out navHit, dist, layermask);

        return navHit.position;
    }

}
