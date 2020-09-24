using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using UnityEngine;
using UnityEngine.AI;

public class AiHeliController : MonoBehaviour
{
    [Header("Components")]
    public HelicopterController Heli;
    public ShooterAIFOV PilotFOV;
    public AiWeaponFire GroundWeapon, AirWeapon;
    public float GroundWeaponAttackRange, AirWeaponAttackRange;

    public bool hasWeapons = true;

    [Header("Transforms")]
    public Vector3 destination;
    public Transform self;
    public Transform body;

    [Header("Range Values")]
    public float originalKDR;
    float keepDistanceRange;
    public float DesiredHeight;
    public float hoverForce;
    public LayerMask groundlayer;

    [Header("Destination Targeting")]
    GameObject[] ObjList;
    public string ObjectiveTag;

    public Transform closestTarget;
    public Transform ObjectiveWaypoint;

    public bool Targeting, MovingToObjective;

    float a;

    private void OnEnable()
    {
        a = 10;
        keepDistanceRange = originalKDR;
        Heli.Operator = VehicleUser.AI;
        Heli.enabled = true;

        InvokeRepeating(nameof(ResetObjList), 5, 5);

        InvokeRepeating(nameof(Search), Random.Range(5, 13), 0.5f);
    }
    private void OnDisable()
    {
        body.rotation = self.rotation;
        Heli.Operator = VehicleUser.Human;
        Heli.enabled = false;
    }

    private void FixedUpdate()
    {
        LiftOff();
        ActionField();
        SetParameters();

        if (!Heli.IsOnGround && Heli.ReadyToFly)
        {
            if(destination != Vector3.zero)
            {
                Turning();
                Moving();
            }

            if (hasWeapons)
            {
                OffensiveAction();
            }
        }
    }

    float Distance(Vector3 a)
    {
        float distanceFromTarget = (a - self.position).magnitude;

        return distanceFromTarget;
    }

    float AngleToTarget(Vector3 a)
    {
        Vector3 localTarget = self.InverseTransformPoint(a);
        float angleToTarget = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

        return angleToTarget;
    }

    Quaternion BodyRotation(Vector3 approximateForward, Vector3 exactUp)
    {
        Quaternion rotateZToUp = Quaternion.LookRotation(exactUp, -approximateForward);
        Quaternion rotateYToZ = Quaternion.Euler(90f, 0f, 0f);

        return rotateZToUp * rotateYToZ;
    }

   

    void LiftOff()
    {
        RaycastHit hit;
        Ray landingRay = new Ray(self.transform.position, Vector3.down);

        if (Physics.Raycast(landingRay, out hit, Heli.EffectiveHeight, groundlayer))
        {
            if(hit.distance < DesiredHeight / 2 || hit.distance > DesiredHeight * 1.5f)
            {

                if(hit.distance < DesiredHeight / 2)
                {
                    Heli.EngineForce += Time.deltaTime * (5 + Heli.HelicopterModel.velocity.magnitude);
                }
                if(hit.distance > DesiredHeight * 1.5f)
                {
                    Heli.EngineForce -= Time.deltaTime;
                }
            }
            else
            {
                Heli.EngineForce = hoverForce;
            }
        }
    }

    void Turning()
    {
        Vector3 offsetToTarget = destination - self.position;

        Vector3 up = self.up;

        Quaternion desiredOrientation = BodyRotation(offsetToTarget, up);

        self.rotation = Quaternion.RotateTowards(
                                self.rotation,
                                desiredOrientation,
                                Heli.turnForcePercent * Time.deltaTime * 10);
    }

    void Moving()
    {
        float i = Distance(destination);

        if (i > keepDistanceRange)
        {
            if (Mathf.Abs(AngleToTarget(destination)) < 10)
            {
                Heli.Forwards = true;
            }
        }
        else
        {
            Heli.Forwards = false;
        }
    }

    void OffensiveAction()
    {
      
        if (closestTarget && Targeting)
        {
            float a = Distance(closestTarget.position);
            float b = AngleToTarget(closestTarget.position);

                body.rotation = Quaternion.Slerp(body.rotation, Quaternion.LookRotation(closestTarget.position - self.position), Heli.turnForcePercent * 0.5f * Time.deltaTime);

            if (closestTarget.parent.gameObject.tag == "Flyer")
            {
                if (a < AirWeaponAttackRange && b < 20)
                {

                    if (AirWeapon)
                    {
                        AirWeapon.AIFiring();
                    }
                    else
                    {
                        if (GroundWeapon)
                        {
                            GroundWeapon.AIFiring();
                        }
                    }
                }
            }
            else
            {
                if (GroundWeapon && a < GroundWeaponAttackRange && b < 10)
                {
                    GroundWeapon.AIFiring();
                }
            }

        }
        else
        {
            body.rotation = Quaternion.Slerp(body.rotation, self.rotation, Heli.turnForcePercent * 0.1f * Time.deltaTime);
        }
    }

    void ActionField()
    {
        if (a < 10)
        {
            a += Time.deltaTime;
        }

        if (closestTarget)
        {
            if(!Targeting)
            {
                Targeting = true;

                if (MovingToObjective)
                {
                    MovingToObjective = false;
                }
            }

            if(a > 5)
            {
                keepDistanceRange = originalKDR;
                destination = closestTarget.position;
                a = 0;
            }
        }
        
        if(!closestTarget)
        {
            keepDistanceRange = originalKDR;
            MovingToObjective = true;

            if (Targeting)
            {
                Targeting = false;
            }

            if (ObjectiveWaypoint)
            {
                destination = ObjectiveWaypoint.position;
            }
            else
            {
                Search();
            }
        }

        if(Distance(destination) < originalKDR && a > 2)
        {
            a = 0;
            keepDistanceRange = 5;
            destination = new Vector3
                (
                closestTarget.position.x + Random.Range(-originalKDR, originalKDR), 
                self.position.y, 
                closestTarget.position.z + Random.Range(-originalKDR,  originalKDR)
                );
        }
    }

    void Search()
    {
        if(!Targeting && hasWeapons)
        {
            float distanceToClosestEnemy = Mathf.Infinity;
            GameObject closestEnemy = null;

            foreach (GameObject currentEnemy in PilotFOV.visibleTargets)
            {
                float distanceToEnemy = (currentEnemy.transform.position - this.transform.position).sqrMagnitude;
                if (distanceToEnemy < distanceToClosestEnemy)
                {
                    distanceToClosestEnemy = distanceToEnemy;
                    closestEnemy = currentEnemy;
                }
            }

            if (closestEnemy)
            {
                closestTarget = closestEnemy.transform;
            }

            if (closestTarget)
            {
                if (closestTarget.tag != PilotFOV.EnemyTag)
                {
                    closestTarget = null;
                }
            }
        }
      

        //

        ObjList = GameObject.FindGameObjectsWithTag(ObjectiveTag);

        if(ObjList != null)
        {
            ObjectiveWaypoint = ObjList[(Random.Range(0, ObjList.Length))].transform;
        }
    }

    void SetParameters()
    {

        if(ObjectiveWaypoint && !ObjectiveWaypoint.gameObject.activeInHierarchy)
        {
            if (destination == ObjectiveWaypoint.position)
            {
                destination = Vector3.zero;
            }
                ObjectiveWaypoint = null;
       
        }
        if(closestTarget)
        {
            if(!closestTarget.gameObject.activeInHierarchy || closestTarget.gameObject.tag != PilotFOV.EnemyTag)
            {
                if (destination == closestTarget.position)
                {
                    destination = Vector3.zero;
                }
                closestTarget = null;
            }
        }
    }

    void ResetObjList()
    {
        if (destination == ObjectiveWaypoint.position)
        {
            destination = Vector3.zero;
        }

        ObjectiveWaypoint = null;
    }
}
