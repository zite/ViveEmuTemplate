using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Aim : MonoBehaviour 
{
    private int layermask;

    private void Awake()
    {
        layermask = 1 << LayerMask.NameToLayer("Built");
    }

    public void DoUpdate(Vector3 origin, Vector3 direction)
    {
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, 100, layermask);

        if (hits.Length > 0)
        {
            RaycastHit closest = hits.OrderBy(hit => hit.distance).First();
            this.transform.position = Vector3.Lerp(origin, closest.point, 0.5f);
            this.transform.localScale = new Vector3(0.1f, 0.1f, closest.distance * 2f);
        }
        else
        {
            this.transform.localScale = new Vector3(0.1f, 0.1f, 50f);

            this.transform.position = origin + (direction * 25f);
        }

        this.transform.forward = direction;
    }
}