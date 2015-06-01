using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Kinect = Windows.Kinect;

public class WallCheck : MonoBehaviour
{
    public float FadeInDistance = 50;
    public float OpaqueDistance = 20;

    private int wallLayerMask;
    private List<Material> materials = new List<Material>();
    private Vector2 textureOffset = new Vector2(0, 0);
    private float textureAnimationSpeed = 0.3f;

    private Kinect.JointType[] CheckJoints = new Kinect.JointType[] { Kinect.JointType.HandRight, Kinect.JointType.HandLeft, Kinect.JointType.Head };
    private Vector3[] directions = new Vector3[] { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };

    private BodySourceView BodyView;

    private void Awake()
    {
        for (int index = 0; index < this.transform.childCount; index++)
        {
            Transform wall = this.transform.GetChild(index);

            Material material = wall.GetComponent<Renderer>().material;
            Color newcolor = material.color;
            newcolor.a = 0;
            material.color = newcolor;
            materials.Add(material);
        }

        wallLayerMask = 1 << this.gameObject.layer;
        BodyView = GameObject.FindObjectOfType<BodySourceView>();
    }

    void Update() 
    {
        if (BodyView == null || BodyView.CurrentAvatar == null)
            return;

        float smallestDistance = float.MaxValue;

        foreach (Kinect.JointType joint in CheckJoints)
        {
            float distance = CheckWallsFor(BodyView.CurrentAvatar.JointMapping[joint]);

            if (distance < smallestDistance)
                smallestDistance = distance;
        }


        Color newcolor = materials[0].color;
        if (smallestDistance <= OpaqueDistance)
            newcolor.a = 1;
        else if (smallestDistance <= FadeInDistance)
            newcolor.a = 1 - Mathf.InverseLerp(OpaqueDistance, FadeInDistance, smallestDistance);
        else
            newcolor.a = 0;

        textureOffset.y -= textureAnimationSpeed * Time.deltaTime; //animate walls moving up

        foreach (Material material in materials)
        {
            material.color = newcolor;
            material.mainTextureOffset = textureOffset;
        }
    }

    private float CheckWallsFor(Transform from)
    {
        float smallestDistance = float.MaxValue;

        foreach (Vector3 direction in directions)
        {
            RaycastHit hitInfo;
            bool hit = Physics.Raycast(from.position, direction, out hitInfo, 10000, wallLayerMask);

            if (hit)
            {
                if (hitInfo.distance < smallestDistance)
                {
                    smallestDistance = hitInfo.distance;
                }
            }
        }

        return smallestDistance;
    }
}
