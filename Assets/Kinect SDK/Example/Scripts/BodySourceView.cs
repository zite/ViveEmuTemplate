﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kinect = Windows.Kinect;

public class BodySourceView : MonoBehaviour 
{
    public GameObject AvatarPrefab;

    public BodySourceManager BodyManager;

    public KinectAvatar CurrentAvatar
    {
        get
        {
            if (Avatars.ContainsKey(CurrentAvatarID))
                return Avatars[CurrentAvatarID];
            else
                return null;
        }
    }

    private Dictionary<ulong, KinectAvatar> Avatars = new Dictionary<ulong, KinectAvatar>();
    private ulong CurrentAvatarID;
    
    private void Awake()
    {
        if (BodyManager == null)
        {
            BodyManager = GameObject.FindObjectOfType<BodySourceManager>();
            if (BodyManager == null)
                Debug.LogError("Error: No body manager script found in scene.");
        }
    }

    private void Update() 
    {
        Kinect.Body[] data = BodyManager.GetData();
        if (data == null)
            return; // no data?
        
        List<ulong> trackedIds = new List<ulong>();
        foreach (Kinect.Body body in data)
        {
            if (body == null)
                continue;
                
            if (body.IsTracked)
                trackedIds.Add(body.TrackingId);
        }
        
        List<ulong> knownIds = new List<ulong>(Avatars.Keys);
        
        // First delete untracked bodies
        foreach(ulong trackingId in knownIds)
        {
            if(trackedIds.Contains(trackingId) == false)
            {
                Avatars[trackingId].Kill();
                Avatars.Remove(trackingId);
            }
        }

        // Then add bodies / update joint positions for bodies that exist
        foreach (Kinect.Body body in data)
        {
            if (body == null)
                continue;
            
            if(body.IsTracked)
            {
                if (Avatars.ContainsKey(body.TrackingId) == false)
                {
                    GameObject newAvatar = GameObject.Instantiate<GameObject>(AvatarPrefab);
                    newAvatar.transform.parent = this.transform; //localize position to the kinect manager
                    newAvatar.transform.localPosition = Vector3.zero;
                    newAvatar.transform.localRotation = Quaternion.identity;

                    KinectAvatar avatar = newAvatar.GetComponent<KinectAvatar>();
                    avatar.Id = body.TrackingId;

                    Avatars.Add(body.TrackingId, avatar);
                }

                Avatars[body.TrackingId].UpdateBodyData(body);
            }
        }

        if (Avatars.Count > 0 && Avatars.ContainsKey(CurrentAvatarID) == false)
        {
            CurrentAvatarID = Avatars.OrderBy(avatar => avatar.Value.GetDistanceToKinect()).First().Key;
            Avatars[CurrentAvatarID].SetActiveAvatar();
        }
    }
}
