using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

using Kinect = Windows.Kinect;
using InControl;

public class KinectAvatar : MonoBehaviour 
{
    [Tooltip("This avatar's ID as assigned by the Kinect")]
    public ulong Id;

    [Tooltip("The number of units to multiply each joint's position by")]
    public float BodyScale = 6f;

    [Tooltip("Shows cubes at joint locations")]
    public bool ShowJoints = true;

    [Tooltip("Connects joints with line renderers to show tracking confidence")]
    public bool ShowJointConnections = true;

    [Tooltip("How much smoothing to apply to position data (larger numbers mean faster movement)")]
    [Range(0, 20)]
    public float PositionSmoothing = 10f;

    [Tooltip("How much smoothing to apply to rotational data (larger numbers mean faster rotation)")]
    [Range(0, 20)]
    public float RotationSmoothing = 10f;

    /// <summary>The Kinect.JointType to Unity transform mapping</summary>
    public Dictionary<Kinect.JointType, Transform> JointMapping = new Dictionary<Kinect.JointType, Transform>();

    /// <summary>The Kinect.JointType to Unity line renderer mapping</summary>
    public Dictionary<Kinect.JointType, LineRenderer> JointLineMapping = new Dictionary<Kinect.JointType, LineRenderer>();

    /// <summary>The last instance of the kinect data for this avatar</summary>
    protected Kinect.Body LastBodyData;

    /// <summary>Whether or not we've gotten any data for this avatar yet</summary>
    private bool FirstDataSet = false;

    /// <summary>Whether or not this is the avatar that gets the OVR tracking</summary>
    private bool CurrentlyActiveAvatar = false;

    public Transform RightHand
    {
        get
        {
            return JointMapping[Kinect.JointType.HandRight];
        }
    }

    public Transform LeftHand
    {
        get
        {
            return JointMapping[Kinect.JointType.HandLeft];
        }
    }

    public Transform Head
    {
        get
        {
            return JointMapping[Kinect.JointType.Head];
        }
    }

    /// <summary>Used to order joints and connect them with line renderers</summary>
    protected Dictionary<Kinect.JointType, Kinect.JointType> JointConnections = new Dictionary<Kinect.JointType, Kinect.JointType>()
    {
        { Kinect.JointType.FootLeft, Kinect.JointType.AnkleLeft },
        { Kinect.JointType.AnkleLeft, Kinect.JointType.KneeLeft },
        { Kinect.JointType.KneeLeft, Kinect.JointType.HipLeft },
        { Kinect.JointType.HipLeft, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.FootRight, Kinect.JointType.AnkleRight },
        { Kinect.JointType.AnkleRight, Kinect.JointType.KneeRight },
        { Kinect.JointType.KneeRight, Kinect.JointType.HipRight },
        { Kinect.JointType.HipRight, Kinect.JointType.SpineBase },
        
        { Kinect.JointType.HandTipLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.ThumbLeft, Kinect.JointType.HandLeft },
        { Kinect.JointType.HandLeft, Kinect.JointType.WristLeft },
        { Kinect.JointType.WristLeft, Kinect.JointType.ElbowLeft },
        { Kinect.JointType.ElbowLeft, Kinect.JointType.ShoulderLeft },
        { Kinect.JointType.ShoulderLeft, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.HandTipRight, Kinect.JointType.HandRight },
        { Kinect.JointType.ThumbRight, Kinect.JointType.HandRight },
        { Kinect.JointType.HandRight, Kinect.JointType.WristRight },
        { Kinect.JointType.WristRight, Kinect.JointType.ElbowRight },
        { Kinect.JointType.ElbowRight, Kinect.JointType.ShoulderRight },
        { Kinect.JointType.ShoulderRight, Kinect.JointType.SpineShoulder },
        
        { Kinect.JointType.SpineBase, Kinect.JointType.SpineMid },
        { Kinect.JointType.SpineMid, Kinect.JointType.SpineShoulder },
        { Kinect.JointType.SpineShoulder, Kinect.JointType.Neck },
        { Kinect.JointType.Neck, Kinect.JointType.Head },
    };

    protected virtual void Awake()
    {
        // Initialize the joint mapping and set renderer states
        for (int index = 0; index < this.transform.childCount; index++)
        {
            Transform joint = this.transform.GetChild(index);

            if (Enum.IsDefined(typeof(Kinect.JointType), joint.name))
            {
                Kinect.JointType jointType = (Kinect.JointType)Enum.Parse(typeof(Kinect.JointType), joint.name);

                JointMapping.Add(jointType, joint);

                LineRenderer jointLineRenderer = joint.GetComponent<LineRenderer>();
                JointLineMapping.Add(jointType, jointLineRenderer);

                if (ShowJoints == false)
                {
                    MeshRenderer jointMeshRenderer = joint.GetComponent<MeshRenderer>();

                    if (jointMeshRenderer != null)
                        jointMeshRenderer.enabled = false;
                }

                if (ShowJointConnections == false)
                {
                    if (jointLineRenderer != null)
                        jointLineRenderer.enabled = false;
                }
            }
        }
    }

    protected virtual void Initialize()
    {
        //do extra initialization stuff here. This is called after this avatar has processed its first data set.
    }

    public void UpdateBodyData(Kinect.Body body)
    {
        LastBodyData = body;

        for (Kinect.JointType jt = Kinect.JointType.SpineBase; jt <= Kinect.JointType.ThumbRight; jt++)
        {
            Kinect.Joint sourceJoint = body.Joints[jt];
            Kinect.Vector4 sourceOrientation = body.JointOrientations[jt].Orientation;
            Kinect.Joint? targetJoint = null;

            if (JointConnections.ContainsKey(jt))
            {
                targetJoint = body.Joints[JointConnections[jt]];
            }

            Transform jointObj = JointMapping[jt];
            Vector3 mewPosition = Unmirror(sourceJoint.Position.GetVector3()) * BodyScale;
            Quaternion mewRotation = sourceOrientation.GetQuaternion();
            if (FirstDataSet)
            {
                jointObj.localPosition = mewPosition;
                jointObj.localRotation = mewRotation;
            }
            else
            {
                jointObj.localPosition = Vector3.Lerp(jointObj.localPosition, mewPosition, Time.deltaTime * PositionSmoothing);
                jointObj.localRotation = Quaternion.Lerp(jointObj.localRotation, mewRotation, Time.deltaTime * RotationSmoothing);
            }

            if (ShowJointConnections == true)
            {
                LineRenderer lr = JointLineMapping[jt];
                if (targetJoint.HasValue)
                {
                    Transform targetJointObj = JointMapping[targetJoint.Value.JointType];
                    lr.SetPosition(0, jointObj.position);
                    lr.SetPosition(1, targetJointObj.position);
                    lr.SetColors(GetColorForState(sourceJoint.TrackingState), GetColorForState(targetJoint.Value.TrackingState));
                }
                else
                {
                    lr.enabled = false;
                }
            }
        }

        if (FirstDataSet == true)
        {
            FirstDataSet = false;

            Initialize();
        }

        if (CurrentlyActiveAvatar == true)
        {
            OVRManager.instance.transform.position = JointMapping[Kinect.JointType.Head].position;
        }
    }

    private void Update()
    {
        if (InputManager.Devices != null && InputManager.Devices.Count > 0)
        {
            if (InputManager.Devices.Any(device => device.MenuWasPressed))
            {
                OVRManager.display.RecenterPose();
            }
        }
    }

    public virtual void Kill()
    {
        CurrentlyActiveAvatar = false;
        Destroy(this.gameObject);
    }

    public Vector3 GetBasePosition()
    {
        Kinect.JointType left = GetClosestPreviousTrackedJoint(Kinect.JointType.FootLeft);
        Kinect.JointType right = GetClosestPreviousTrackedJoint(Kinect.JointType.FootRight);
        return Vector3.Lerp(JointMapping[left].position, JointMapping[right].position, 0.5f);
    }

    private Kinect.JointType GetClosestPreviousTrackedJoint(Kinect.JointType jointtype)
    {
        if (LastBodyData == null)
            return jointtype;

        while (LastBodyData.Joints[jointtype].TrackingState != Kinect.TrackingState.Tracked)
        {
            if (JointConnections.ContainsValue(jointtype))
            {
                jointtype = JointConnections.First(kvp => kvp.Value == jointtype).Key;
            }
            else
            {
                break;
            }
        }

        return jointtype;
    }

    private Kinect.JointType GetClosestNextTrackedJoint(Kinect.JointType jointtype)
    {
        if (LastBodyData == null)
            return jointtype;

        while (LastBodyData.Joints[jointtype].TrackingState != Kinect.TrackingState.Tracked)
        {
            if (JointConnections.ContainsKey(jointtype))
            {
                jointtype = JointConnections[jointtype];
            }
            else
            {
                break;
            }
        }

        return jointtype;
    }

    public void SetActiveAvatar()
    {
        CurrentlyActiveAvatar = true;
    }

    public void SetInactiveAvatar()
    {
        CurrentlyActiveAvatar = false;
    }

    public float GetDistanceToKinect()
    {
        return Vector3.Distance(Vector3.zero, JointMapping[Kinect.JointType.Head].localPosition);
    }

    public Ray GetRightHandRay()
    {
        return GetHandRay(Kinect.JointType.ElbowRight, Kinect.JointType.HandRight);
    }
    public Ray GetLeftHandRay()
    {
        return GetHandRay(Kinect.JointType.ElbowLeft, Kinect.JointType.HandLeft);
    }

    public Ray GetHandRay(Kinect.JointType hand)
    {
        if (hand == Kinect.JointType.HandLeft)
            return GetHandRay(Kinect.JointType.ElbowLeft, Kinect.JointType.HandLeft);
        else if (hand == Kinect.JointType.HandRight)
            return GetHandRay(Kinect.JointType.ElbowRight, Kinect.JointType.HandRight);
        else
        {
            Debug.LogError("Error: GetHandRay Method only takes HandLeft and HandRight. Got: " + hand.ToString());
            return GetHandRay(Kinect.JointType.ElbowRight, Kinect.JointType.HandRight);
        }
    }
    public Ray GetHandRay(Kinect.JointType elbowJoint, Kinect.JointType handJoint)
    {
        Vector3 elbow = JointMapping[elbowJoint].position;
        Vector3 hand = JointMapping[handJoint].position;

        Vector3 direction = (hand - elbow).normalized;

        return new Ray(hand, direction);
    }

    private static Color GetColorForState(Kinect.TrackingState state)
    {
        switch (state)
        {
            case Kinect.TrackingState.Tracked:
                return Color.green;

            case Kinect.TrackingState.Inferred:
                return Color.red;

            default:
                return Color.black;
        }
    }

    private static Vector3 Unmirror(Vector3 mirrored)
    {
        mirrored.z = -mirrored.z;
        return mirrored;
    }
}
