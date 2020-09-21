using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public enum AIBehaviour
{
    Ranger,
    Berserk,
    Objpriority
}

public enum Job
{
    Assault,
    Grenadier,
    Engineer,
    Landminer,
    Smoker,
    Inhibitor,
    Knight,
    Armor
}

public class ShooterAIAttack : MonoBehaviour
{
    [HideInInspector] public bool Alert = false;
    [HideInInspector] public bool Shooting = false;
    public bool randomizebehav = false;
    [HideInInspector] public bool closerange = false;
    [HideInInspector] public bool meleeattacking = false;
    public float Distance;
    public float RotSpeed;
    [SerializeField] private float meleeinterval;

    public float Attack_range, Chase_Range, Melee_Range, Melee_Strength;
    public GameObject Character, Target, Model, Head;
    float Alerttimer = 2.0f;

    private GameObject[] SoundSourceList;
    public GameObject SoundSource;
    Vector3 SDirection;
    float SoundDistance;
    public string SoundTag = "Noise";
    [HideInInspector] public float peacetime;

    Quaternion NormD = Quaternion.Euler(0, 10, 0);
    public AIBehaviour behaviour;

    [Header("Scripts")]
    public GroundCheck gc;
    public AudioSource aud;
    public AudioClip meleesound;
    public ShooterAIFOV FOV;
    public ShooterAIMovement_Dom MOV;
    public AiWeaponFire awf;
    public GameObject weaponsystem;
    public Transform throwspot;
    public Health hth;

    [Header("Abilities")]
    float ActionTimer;
    public float timeBetweenAbilities;
    public Rigidbody rb;
    public bool evasivemaneuvers = true;
    public bool abilityused = false;

    [Header("Accuracy")]
    [SerializeField]int accuracy;
    [HideInInspector]public float useraccuracy;

    [Header("Naming")]
    public string playername;
    public string team;
    public NamingArchive na;
    public Text nametext;
    public Color nametextcolor;

    [Header("Minimap")]
    public bool minimapicon;
    public Animator minimapiconanimator;

    ObjectPooler OP;

    // Start is called before the first frame update
    void Start()
    {
        OP = ObjectPooler.instance;

        if(na != null)
        {
            playername = na.nameslist[Random.Range(1, na.nameslist.Length)];
        }

        Invoke("Decidebehav", 1f);
        
        meleeattacking = false;
        meleeinterval = 1.8f;

        if(nametext)
        {
            nametext.text = playername;
            nametext.color = nametextcolor;
        }

        if (minimapicon && FOV.EnemyTag == "Team2")
        {
            minimapiconanimator.SetBool("Enemy?", true);
        }
    }

    void Decidebehav()
    {
            SDirection = Vector3.zero;
            if (randomizebehav)
            {
                behaviour = (AIBehaviour)Random.Range(0, 3);
            }


            if (!randomizebehav && awf.weaponType == "Assault")
            {
                behaviour = (AIBehaviour)Random.Range(0, 3);
            }
            else if (!randomizebehav && awf.weaponType == "Shotgun")
            {
                behaviour = AIBehaviour.Berserk;
            }
            else if (!randomizebehav && awf.weaponType == "SMG")
            {
                behaviour = (AIBehaviour)Random.Range(1, 3);
            }
            else if (!randomizebehav && awf.weaponType == "Sniper")
            {
                behaviour = AIBehaviour.Ranger;
            }

            if (behaviour == AIBehaviour.Berserk || behaviour == AIBehaviour.Objpriority)
            {
                Attack_range = Random.Range(30, 60);

                Chase_Range = Attack_range + 30;
            }
            if (behaviour == AIBehaviour.Ranger)
            {
                Attack_range = Random.Range(40, 80);

                Chase_Range = Attack_range + 10;
            }
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Pausegame.IsOn)
        {
            return;
        }

        if (!awf)
        {
            awf = weaponsystem.GetComponentInChildren<AiWeaponFire>();
        }

        SoundSourceList = GameObject.FindGameObjectsWithTag(SoundTag);
        ActionTimer += Time.deltaTime;

        if (SoundSource == null)
        {
            SoundSource = null;
        }

        if (SoundSource != null)
        {
            SoundDistance = (SoundSource.transform.position - transform.position).magnitude;
        }

        if (!Target || meleeinterval < 1.4)
        {
            meleeattacking = false;
        }

        float distanceToClosestEnemy = 99999;
        GameObject ClosestEnemy = null;

        if(FOV != null)
        {
            if (FOV.visibleTargets != null)
            {
                foreach (GameObject target in FOV.visibleTargets)
                {

                    if (target)
                    {
                        float distancetoEnemy = (target.transform.position - FOV.transform.position).magnitude;
                        if (distancetoEnemy < distanceToClosestEnemy)
                        {
                            distanceToClosestEnemy = distancetoEnemy;

                            ClosestEnemy = target;
                        }

                    }
                }
            }
        }
      

        if (ClosestEnemy != null)
            Debug.DrawLine(FOV.gameObject.transform.position, ClosestEnemy.transform.position, Color.black);

        Target = ClosestEnemy;

        //

        if (Target)
        {
            Distance = (Target.transform.position - transform.position).magnitude;

            useraccuracy = Distance / accuracy;
        }
   
        if (SoundSource == null)
            SoundSource = null;

        if (Shooting == false && Alert == false)
            peacetime += Time.deltaTime;

        if(SoundDistance < Chase_Range && SoundSource)
        {
            SDirection = SoundSource.transform.position - transform.position;
        }

        if (Target == null && SDirection != Vector3.zero)
        {
            Alert = true;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(SDirection), RotSpeed * Time.deltaTime);

            Model.transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
            Invoke(nameof(Removesound), 1f);
        }

        if (behaviour == AIBehaviour.Objpriority && !Target)
        {
                if (ActionTimer >= timeBetweenAbilities && gc.isgrounded && MOV.DistanceToObjective > 20)
                {
                Pounce();
                }

        }

        //Chasing
        if (Distance <= Chase_Range && Distance > Attack_range && !hth.dead )
        {
            if(gc.isgrounded)
            {
                MOV.agent.isStopped = false;
            }
            else
            {
                MOV.agent.isStopped = true;
            }

            Alert = true;
            Shooting = false;
            peacetime = 0;

            if (Target && behaviour != AIBehaviour.Objpriority && !hth.dead && !MOV.findingmedkits && !MOV.FindingMGs)
            {
                MOV.agent.SetDestination(Target.transform.position);
            }

        }

        if (Distance > Chase_Range)
        {
            Target = null;
            Shooting = false;
        }

        if(Distance < Melee_Range)
        {
            closerange = false;
        }

        //Attacking
        if (Distance <= Attack_range && Target && !hth.dead)
        {
            meleeinterval += Time.deltaTime;

            Vector3 Direction = Target.transform.position - transform.position; // the defference of position of these two objects, in order to use in rotation 
                                                                                //Direction.y = 0; // so the target won't rotate in the y-axis
            if (behaviour == AIBehaviour.Ranger)
            {
                MOV.agent.isStopped = true;

                if(!abilityused && MOV.job == Job.Engineer)
                {
                    Ability();
                }

                if(Distance > Melee_Range)
                {
                    if (ActionTimer >= timeBetweenAbilities && gc.isgrounded && evasivemaneuvers)
                    {
                        Dodge();
                    }
                }
            }
            else if (behaviour == AIBehaviour.Berserk && !hth.dead)
            {

                if (!abilityused && MOV.job == Job.Grenadier)
                {
                    Ability();
                }

                if (!abilityused && MOV.job == Job.Inhibitor)
                {
                    Ability();
                }

                if (Distance <= Melee_Range)
                {
                    MOV.agent.isStopped = true;
                }
                else
                {
                    if(!MOV.findingmedkits && !MOV.FindingMGs)
                    {
                        MOV.agent.SetDestination(Target.transform.position);
                    }

                    if (ActionTimer >= timeBetweenAbilities && gc.isgrounded && evasivemaneuvers)
                    {
                        Strafe();
                    }
                }

            }
            else if (behaviour == AIBehaviour.Objpriority && !hth.dead)
            {

                if (!abilityused && MOV.job == Job.Landminer)
                {
                    Ability();
                }

                if(!abilityused && MOV.job == Job.Smoker && FOV.visibleTargets.Count > 3)
                {
                    Ability();
                }

                if (Distance <= Melee_Range)
                {
                    MOV.agent.isStopped = true;
                }
                else
                {
                    if (!MOV.findingmedkits && !MOV.FindingMGs)
                    {
                        MOV.agent.SetDestination(Target.transform.position);
                    }
                    if (ActionTimer >= timeBetweenAbilities && gc.isgrounded && evasivemaneuvers)
                    {
                        Strafe();
                    }
                }

            }

            if (Distance > Melee_Range)
            {
                Invoke(nameof(Firing), Random.Range(0, 0.4f));
            }

            if (Distance <= Melee_Range && !hth.dead)
            {
                closerange = true;
                Shooting = false;
                MOV.agent.ResetPath();

                if (meleeinterval >= 1.4 && Target)
                {
                    meleeattacking = true;
                    MeleeHit();
                    meleeinterval = 0;
                }
            }
            peacetime = 0;

            Alert = true;
            CancelInvoke(nameof(Alertdisable));

            // rotate the target toward the player
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Direction), RotSpeed * Time.deltaTime);
            Head.transform.rotation = Quaternion.Slerp(Head.transform.rotation, Quaternion.LookRotation(Direction), RotSpeed * Time.deltaTime);
            Model.transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }

        else
        {
            Invoke(nameof(Alertdisable), Alerttimer);
            Shooting = false;
            closerange = false;
        }

        if (SoundSource == null && Target == null)
        {
            SoundDistance = 0.0f;
            Invoke(nameof(Alertdisable), Alerttimer);
            Shooting = false;
            closerange = false;
        }


    }

    void Removesound()
    {
        SDirection = Vector3.zero;
    }

    void Alertdisable()
    {
        if (Alert == true && Shooting == false && Head != null)
        {
            Head.transform.rotation = Quaternion.Slerp(transform.rotation, NormD, RotSpeed / 4 * Time.deltaTime);
            Alert = false;
        }
    }

    void Firing()
    { 
        Shooting = true;
        closerange = false;

        if(minimapicon && FOV.EnemyTag == "Team2" && !awf.suppressor)
        {
            minimapiconanimator.Play("NPCReveal", 0, 0f);
        }
    }

    void Strafe()
    {
        gc.gctimer = 0;
        gc.isgrounded = false;
        timeBetweenAbilities = Random.Range(0.8f, 2f);
        rb.AddForce(Random.Range(-250, 250), 250, Random.Range(-250, 250), ForceMode.Impulse);

        ActionTimer = 0f;
    }

    void Dodge()
    {
        gc.gctimer = 0;
        gc.isgrounded = false;
        rb.AddForce(Random.Range(-300, 300), 100, Random.Range(-300, 300), ForceMode.Impulse);

        ActionTimer = 0f;
    }

    void Pounce()
        {
            gc.gctimer = 0;
            gc.isgrounded = false;
            rb.AddForce(0, 300, 0, ForceMode.Impulse);
            rb.AddForce(Character.transform.forward* 400, ForceMode.Impulse);

            ActionTimer = 0f;
        }

    void MeleeHit()
    {
        aud.PlayOneShot(meleesound);
        if(Target)
        {
            Target.SendMessageUpwards("Attackedby", playername, SendMessageOptions.DontRequireReceiver);
            Target.SendMessageUpwards("Attackedbyteam", team, SendMessageOptions.DontRequireReceiver);
            Target.SendMessageUpwards("ChangeHealth", -Melee_Strength, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void FixedUpdate()
    {

        float distanceToClosestSound = 99999;
        GameObject ClosestSound = null;

        if (SoundSourceList != null)
        {

            foreach (GameObject CurrentSound in SoundSourceList)
            {
                if (CurrentSound != null)
                {
                    float distancetoSound = (CurrentSound.transform.position - transform.position).magnitude;
                    if (distancetoSound < distanceToClosestSound)
                    {
                        distanceToClosestSound = distancetoSound;
                        ClosestSound = CurrentSound.gameObject;
                    }
                }
            }
        }


        if (ClosestSound != null)
            Debug.DrawLine(transform.position, ClosestSound.transform.position, Color.cyan);

        SoundSource = ClosestSound;
    }

    public void Ability()
    {
        abilityused = true;
        string str;

        if(MOV.job == Job.Engineer)
        {
            if(team == "Enemy")
            {
                str = "T1ts";
                GameObject proj = OP.SpawnFromPool(str, throwspot.position, throwspot.rotation);

                Exploder e = proj.GetComponent<Exploder>();

                if (e)
                {
                    e.shootername = playername;
                    e.team = team;
                }
            }

            if(team == "Ally")
            {
                str = "T2ts";
                GameObject proj = OP.SpawnFromPool(str, throwspot.position, throwspot.rotation);

                Exploder e = proj.GetComponent<Exploder>();

                if (e)
                {
                    e.shootername = playername;
                    e.team = team;
                }
            }


            Invoke(nameof(Resetability), 60);
        }
      
        if(MOV.job == Job.Grenadier)
        {
            str = "FRG";

            GameObject proj = OP.SpawnFromPool(str, throwspot.position, throwspot.rotation);

            Transform pt = proj.transform;
            Rigidbody prb = proj.GetComponent<Rigidbody>();
            Grenade g = proj.GetComponent<Grenade>();
            Exploder e = proj.GetComponent<Exploder>();

            if (g)
            {
                e.shootername = playername;
                e.team = team;
                g.speed = 200;
            }

            Invoke(nameof(Resetability), 30);
        }

        if (MOV.job == Job.Smoker)
        {
            str = "SMK";

            GameObject proj = OP.SpawnFromPool(str, throwspot.position, throwspot.rotation);

            Transform pt = proj.transform;
            Rigidbody prb = proj.GetComponent<Rigidbody>();
            Grenade g = proj.GetComponent<Grenade>();
            Exploder e = proj.GetComponent<Exploder>();

            if (g)
            {
                e.shootername = playername;
                e.team = team;
                g.speed = 200;
            }

            Invoke(nameof(Resetability), 90);
        }

        if (MOV.job == Job.Landminer)
        {
            str = "LDM";

            GameObject proj = OP.SpawnFromPool(str, throwspot.position, throwspot.rotation);

            Transform pt = proj.transform;
            Rigidbody prb = proj.GetComponent<Rigidbody>();
            Grenade g = proj.GetComponent<Grenade>();
            Exploder e = proj.GetComponent<Exploder>();

            if (g)
            {
                e.shootername = playername;
                e.team = team;


                if (team == "Enemy")
                {
                    g.seekTag = "Team2";
                }

                if (team == "Ally")
                {
                    g.seekTag = "Enemy";
                }
                g.speed = 60;
            }

            Invoke(nameof(Resetability), 45);
        }

        if (MOV.job == Job.Inhibitor)
        {
            str = "INH";

            GameObject proj = OP.SpawnFromPool(str, throwspot.position, throwspot.rotation);

            Transform pt = proj.transform;
            Rigidbody prb = proj.GetComponent<Rigidbody>();
            Grenade g = proj.GetComponent<Grenade>();

            if (g)
            { 
                g.speed = 30;
            }

            Invoke(nameof(Resetability), 40);
        }

        if (MOV.job == Job.Assault)
        {
            str = "MDK";

            GameObject proj = OP.SpawnFromPool(str, throwspot.position, throwspot.rotation);
            Invoke(nameof(Resetability), 35);
        }
    }

    void Resetability()
    {
        abilityused = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(Character.transform.position, Attack_range);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Character.transform.position, Chase_Range);

        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(Character.transform.position, Melee_Range);
    }
}
