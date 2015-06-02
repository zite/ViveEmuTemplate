using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kinect = Windows.Kinect;

public class KinectManager : MonoBehaviour 
{
    public static KinectManager Instance;

    public GameObject AvatarPrefab;

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

    protected Dictionary<ulong, KinectAvatar> Avatars = new Dictionary<ulong, KinectAvatar>();
    protected ulong CurrentAvatarID;

    protected Kinect.KinectSensor Sensor;
    protected Kinect.BodyFrameReader Reader;
    protected Kinect.Body[] Data = null;

    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Debug.Log("Error. There can be only one KinectManager.");
            Debug.Log("Kinect Manager #1", Instance.gameObject);
            Debug.Log("Kinect manager #2", this.gameObject);
        }
        else
        {
            Instance = this;
        }

        /*Transform KinectRepresentation = this.transform.Find("Kinect");
        if (KinectRepresentation != null)
        {
            foreach (Renderer renderer in this.transform.Find("Kinect").GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
        }*/
    }

    protected virtual void Start()
    {
        try
        {
            Sensor = Kinect.KinectSensor.GetDefault();

            if (Sensor != null)
            {
                Reader = Sensor.BodyFrameSource.OpenReader();

                if (!Sensor.IsOpen)
                {
                    Sensor.Open();
                }
            }
        }
        catch
        {
            Debug.Log("Error: No Kinect found.");
        }
    }

    protected virtual void Update()
    {
        if (Reader != null)
        {
            var frame = Reader.AcquireLatestFrame();
            if (frame != null)
            {
                if (Data == null)
                {
                    Data = new Kinect.Body[Sensor.BodyFrameSource.BodyCount];
                }

                frame.GetAndRefreshBodyData(Data);

                frame.Dispose();
                frame = null;
            }
        }

        if (Data == null)
            return; //No data yet
        
        List<ulong> trackedIds = new List<ulong>();
        foreach (Kinect.Body body in Data)
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
        foreach (Kinect.Body body in Data)
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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnApplicationQuit()
    {
        if (Reader != null)
        {
            Reader.Dispose();
            Reader = null;
        }

        if (Sensor != null)
        {
            if (Sensor.IsOpen)
            {
                Sensor.Close();
            }

            Sensor = null;
        }
    }
}
