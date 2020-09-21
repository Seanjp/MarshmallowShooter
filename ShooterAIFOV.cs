using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShooterAIFOV : MonoBehaviour
{
    public float viewRadius = 500;
    [Range(0, 360)]
    public float viewAngle;

    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public GameObject Self;

    public string EnemyTag = "Enemy";
    public bool Segregate = true;

    public List<GameObject> visibleTargets = new List<GameObject>();

    private void OnEnable()
    {
        InvokeRepeating(nameof(FindTargets), 0.2f, 0.2f);
    }

    void FindTargets()
    {
        if(!Pausegame.IsOn)
        FindVisibleTargets();
    }

    void FindVisibleTargets()
    {
        visibleTargets.Clear();
        Collider[] targetsInViewRadius = Physics.OverlapSphere(Self.transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            GameObject target = targetsInViewRadius[i].gameObject;
            Vector3 dirToTarget = (target.transform.position - Self.transform.position).normalized;
            if (Vector3.Angle(Self.transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = (Self.transform.position - target.transform.position).magnitude;

                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    if (Segregate)
                    {
                        if (target.CompareTag(EnemyTag))
                        {
                            visibleTargets.Add(target);
                        }
                    }
                    else
                    {
                        visibleTargets.Add(target);
                    }

                    visibleTargets.Remove(Self);
                }
            }
        }
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
