using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour 
{
    public float Force = 2000f;
    private int BuiltLayer;

    private Rigidbody MyRigidBody;
    private void Awake()
    {
        MyRigidBody = this.GetComponent<Rigidbody>();
        BuiltLayer = LayerMask.NameToLayer("Built");
    }

    public void Launch(Ray ray)
    {
        this.transform.position = ray.origin;
        this.transform.forward = ray.direction;

        MyRigidBody.AddForce(ray.direction * Force);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (col.gameObject.layer == BuiltLayer)
        {
            if (col.gameObject.GetComponent<Rigidbody>() == null)
            {
                Rigidbody newrb = col.gameObject.AddComponent<Rigidbody>();
                newrb.velocity = -col.relativeVelocity;
                col.gameObject.AddComponent<Bullet>();
            }
        }
    }
}
