using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Timeline;
public enum VehicleUser
{
    Human,
    AI
}
public enum WheelType
{
    Wheels,
    Tracks
}

public class WheeledVehicleController : MonoBehaviour
{
    public float horizontalInput;
    private float verticalinput;
    public bool brake;

    [Header("Driver Type (Player/AI)")]
    public VehicleUser DriverType;

    [Header("Steering")]
    public float maxSteerAngle = 30;
    public float steerSensitivity, speedDependencyFactor, steerAngleLimitingFactor, steerAngle;
    /// Tracks
    public float leftTrackSteeringFactor, rightTrackSteeringFactor;
    bool spinning;

    [Header("Speed")]
    public float motorForce = 50;
    float launchtorque;
    float drivetorque;

    public GameObject killbumper;

    public float brakeForce = 50;
    public float currSpeed;
    public float topSpeed;

    [Header("Engine")]
    public float SpoolFactor = 1;
    public float BrakeFactor = 1;
    public float engineRev, engineRevLimit;

    [Header("Drag")]
    public float topspeedrag;

    [Header("FWD/RWD/AWD, Wheels only")]
    public string Drivetrain;

    [Header("Wheels")]
    public WheelType wt;
    public WheelCollider[] Wheels;
    public Transform[] WheelGraphics;

    [Header("Tracks")]
    public Animator[] TracksAnimation;
    float originalStiffness;

    [Header("General")]
    public Rigidbody rb;

    /// //////////////////// FOR AI ////////////////////////////////////////////////////// ///

    [Header("FOR AI DRIVERS ONLY")]

    [Header("Navigation")]
    public Transform AiAgent;
    public NavMeshAgent agent;

    public float KeepDistanceRange;
    float DistanceFromAgent;

    [Header("Steering")]
    public float angleToTarget;
    public bool reversing;
    public float AISteerValue;

    private void Awake()
    {
        launchtorque = motorForce * 4;
        drivetorque = motorForce;
        killbumper.SetActive(false);
        originalStiffness = Wheels[0].sidewaysFriction.stiffness;
    }
    public void GetInput()
    {
        if(DriverType == VehicleUser.Human)
        {
            horizontalInput = Input.GetAxis("Horizontal");
            verticalinput = Input.GetAxis("Vertical");
            brake = Input.GetButton("Jump");
        }
        currSpeed = rb.velocity.magnitude;
    }
    void Steer()
    {   
        if(wt == WheelType.Wheels)
        {
            float x = horizontalInput * (maxSteerAngle - (currSpeed / topSpeed) * steerAngleLimitingFactor);
            float steerSpeed = steerSensitivity + (currSpeed / topSpeed) * speedDependencyFactor;

            steerAngle = Mathf.SmoothStep(steerAngle, x, steerSpeed);

            Wheels[0].steerAngle = steerAngle;
            Wheels[1].steerAngle = steerAngle;
        }

        if (wt == WheelType.Tracks)
        {
            if (horizontalInput == 0)
            {
                leftTrackSteeringFactor = 1;
                rightTrackSteeringFactor = 1;

                for (int i = 0; i < Wheels.Length; i++)
                {
                    Wheels[i].steerAngle = 0;
                }
            }
            else
            {
                leftTrackSteeringFactor = horizontalInput * steerSensitivity;
                rightTrackSteeringFactor = -horizontalInput * steerSensitivity;
            }
        }
        
    }
    void Accelerate()
    {
        if (wt == WheelType.Wheels)
        {
            if (Drivetrain == "FWD")
            {
                Wheels[0].motorTorque = verticalinput * motorForce;
                Wheels[1].motorTorque = verticalinput * motorForce;
            }
            if (Drivetrain == "RWD")
            {
                Wheels[2].motorTorque = verticalinput * motorForce;
                Wheels[3].motorTorque = verticalinput * motorForce;
            }
            if (Drivetrain == "AWD")
            {
                Wheels[0].motorTorque = verticalinput * motorForce;
                Wheels[1].motorTorque = verticalinput * motorForce;
                Wheels[2].motorTorque = verticalinput * motorForce;
                Wheels[3].motorTorque = verticalinput * motorForce;
            }
        }
        if (wt == WheelType.Tracks)
        {
            for (int i = 0; i < Wheels.Length/2; i++)
            {
                Wheels[i].motorTorque = verticalinput * motorForce * leftTrackSteeringFactor;
            }
            for (int i = Wheels.Length/2; i < Wheels.Length; i++)
            {
                Wheels[i].motorTorque = verticalinput * motorForce * rightTrackSteeringFactor;
            }
        }
    }
    void Dragcontrol()
    {
        if(currSpeed == 0)
        {
            rb.drag = 0;
        }
        else
        {
            rb.drag = Mathf.Abs(currSpeed / topSpeed) * topspeedrag;
        }
    }
    void Torquecontrol()
    {
        if (currSpeed <= topSpeed / 3)
        {
            motorForce = launchtorque;
        }
        if(currSpeed >= topSpeed / 3)
        {
            motorForce = drivetorque;
        }
    }
    void Brake()
    {
        if(brake)
        {
            for (int i = 0; i < Wheels.Length; i++)
            {
                Wheels[i].brakeTorque = brakeForce;
            }
        }
        else
        {
            for (int i = 0; i < Wheels.Length; i++)
            {
                Wheels[i].brakeTorque = 0;
            }
        }
    }
    void UpdateWheelPoses()
    {
        if(wt == WheelType.Wheels)
        {
            for (int i = 0; i < Wheels.Length; i++)
            {
                UpdateWheelPose(Wheels[i], WheelGraphics[i]);
            }

        }

        if(wt == WheelType.Tracks)
        {
            //forwards/backwards
            if (currSpeed < 0.2)
            {
                if(verticalinput != 0 && horizontalInput != 0)
                {
                    spinning = true;
                }
                else
                {
                    spinning = false;

                    for (int i = 0; i < TracksAnimation.Length; i++)
                    {
                        TracksAnimation[i].SetFloat("Direction", verticalinput);
                        TracksAnimation[i].speed = 0;
                    }
                }
            }
            else if (!spinning)
            {
                for (int i = 0; i < TracksAnimation.Length; i++)
                {
                    TracksAnimation[i].speed = currSpeed;
                }
            }

            if(spinning)
            {
                for (int i = 0; i < TracksAnimation.Length / 2; i++)
                {
                    TracksAnimation[i].SetFloat("Direction", rightTrackSteeringFactor);
                    TracksAnimation[i].speed = 2 + currSpeed;
                }
                for (int i = TracksAnimation.Length / 2; i < TracksAnimation.Length; i++)
                {
                    TracksAnimation[i].SetFloat("Direction", leftTrackSteeringFactor);
                    TracksAnimation[i].speed = 2 + currSpeed;
                }
            }
        }
    }

    void UpdateWheelPose(WheelCollider collider, Transform transform)
    {
        Vector3 pos = transform.position;
        Quaternion quat = transform.rotation;

        collider.GetWorldPose(out pos, out quat);

        transform.position = pos;// Vector3.Lerp(transform.position,pos, Time.deltaTime);
        transform.rotation = quat;// Quaternion.Slerp(transform.rotation, quat, Time.deltaTime);
    }
    void KillBySpeed()
    {
        if(currSpeed > topSpeed/3)
        {
            killbumper.SetActive(true);
        }
        else
        {
            killbumper.SetActive(false);
        }
    }
    void EngineWork()
    {
        if (verticalinput != 0)
        {
            if (engineRev < engineRevLimit)
            {
                engineRev += Time.deltaTime * SpoolFactor;
            }
        }
        else
        {
            if (engineRev > 0)
            {
                if (brake)
                {
                    engineRev -= Time.deltaTime * BrakeFactor;
                }
                else
                {
                    engineRev -= Time.deltaTime * 2;
                }
            }

            if (engineRev < 0.2)
            {
                engineRev = 0;
            }
        }
    }

    void AICheckDistance()
    {
        DistanceFromAgent = (transform.position - AiAgent.position).magnitude;

        if(DistanceFromAgent > KeepDistanceRange && !spinning)
        {
            brake = false;
            if(angleToTarget < maxSteerAngle && angleToTarget > -maxSteerAngle)
            {
                verticalinput = 1;

                if(wt == WheelType.Tracks)
                {
                    leftTrackSteeringFactor = 1;
                    rightTrackSteeringFactor = 1;
                }
            }
            else
            {
                verticalinput = -1;
            }
        }
        else
        {
            verticalinput = 0;
            brake = true;
        }
    }
    void AISteer()
    {

            Vector3 localTarget = transform.InverseTransformPoint(AiAgent.position);
            angleToTarget = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

            AISteerValue = angleToTarget * Mathf.Sign(currSpeed);
            steerAngle = Mathf.Clamp(AISteerValue, -maxSteerAngle, maxSteerAngle);

        if (wt == WheelType.Wheels)
        {
            if (Mathf.Abs(angleToTarget) < maxSteerAngle)
            {
                if (!reversing)
                {
                    Wheels[0].steerAngle = steerAngle;
                    Wheels[1].steerAngle = steerAngle;
                }
                else
                {
                    brake = true;
                    Invoke(nameof(BackToDrive), 1);
                }
            }
            else
            {
                reversing = true;
                Wheels[0].steerAngle = -steerAngle;
                Wheels[1].steerAngle = -steerAngle;
            }
        }
        if(wt == WheelType.Tracks)
        {
            if (Mathf.Abs(angleToTarget) < maxSteerAngle)
            {
                for (int i = 0; i < Wheels.Length; i++)
                {
                    WheelFrictionCurve swf = Wheels[i].sidewaysFriction;
                    swf.stiffness = 7;
                    Wheels[i].sidewaysFriction = swf;
                }

                if (!spinning)
                {
                    horizontalInput = 0;
                    if (angleToTarget > 5 && angleToTarget < maxSteerAngle)
                    {
                        leftTrackSteeringFactor = 0.5f * steerSensitivity;
                        rightTrackSteeringFactor = -0.5f * steerSensitivity;
                    }
                    if (angleToTarget < -5 && angleToTarget > -maxSteerAngle)
                    {
                        leftTrackSteeringFactor = -0.5f * steerSensitivity;
                        rightTrackSteeringFactor = 0.5f * steerSensitivity;
                    }
                }
                else
                {
                    brake = true;
                    spinning = false;

                    Invoke(nameof(BackToDrive), 2);
                }
            }
            else
            {
                if(currSpeed > 0.1)
                {
                    if(!spinning)
                    {
                        brake = true;
                    }
                }
                else
                {
                    spinning = true;
                    horizontalInput = 1;

                    for (int i = 0; i < Wheels.Length; i++)
                    {
                        WheelFrictionCurve swf = Wheels[i].sidewaysFriction;
                        swf.stiffness = originalStiffness;
                        Wheels[i].sidewaysFriction = swf;
                    }

                    if (angleToTarget > maxSteerAngle && angleToTarget < 179)
                    {
                        leftTrackSteeringFactor = -1.3f * steerSensitivity;
                        rightTrackSteeringFactor = 1.3f * steerSensitivity;
                    }
                    if (angleToTarget < -maxSteerAngle && angleToTarget > -179)
                    {
                        leftTrackSteeringFactor = 1.3f * steerSensitivity;
                        rightTrackSteeringFactor = -1.3f * steerSensitivity;
                    }
                }
            }
        }
    }

    void BackToDrive()
    {
        brake = false;
        reversing = false;

        for (int i = 0; i < Wheels.Length; i++)
        {
            WheelFrictionCurve swf = Wheels[i].sidewaysFriction;
            swf.stiffness = originalStiffness;
            Wheels[i].sidewaysFriction = swf;
        }
    }

    public void Respawn()
    {
        engineRev = 0;
        rb.velocity = Vector3.zero;
        brake = false;
        spinning = false;
        reversing = false;
        for (int i = 0; i < Wheels.Length; i++)
        {
            Wheels[i].steerAngle = 0;
        }
    }

    private void FixedUpdate()
    {
        GetInput();
        Accelerate();
        UpdateWheelPoses();
        Brake();
        Dragcontrol();
        Torquecontrol();
        KillBySpeed();
        EngineWork();

        //Player Only//
        if (DriverType == VehicleUser.Human)
        {
            Steer();
        }
        //AI ONLY//
        if (DriverType == VehicleUser.AI)
        {

            AICheckDistance();
            AISteer();
        }
    }
}
