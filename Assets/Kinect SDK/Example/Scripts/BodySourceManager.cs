﻿using UnityEngine;
using System.Collections;
using Windows.Kinect;

public class BodySourceManager : MonoBehaviour 
{
    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;
    private Body[] _Data = null;
    
    public Body[] GetData()
    {
        return _Data;
    }

    private void Awake()
    {
        /*Transform KinectRepresentation = this.transform.Find("Kinect");
        if (KinectRepresentation != null)
        {
            foreach (Renderer renderer in this.transform.Find("Kinect").GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
        }*/
    }

    private void Start () 
    {
        try
        {
            _Sensor = KinectSensor.GetDefault();

            if (_Sensor != null)
            {
                _Reader = _Sensor.BodyFrameSource.OpenReader();

                if (!_Sensor.IsOpen)
                {
                    _Sensor.Open();
                }
            }
        }
        catch
        {
            Debug.Log("No Kinect found.");
            Destroy(this.gameObject);
        }
    }

    private void Update() 
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                if (_Data == null)
                {
                    _Data = new Body[_Sensor.BodyFrameSource.BodyCount];
                }
                
                frame.GetAndRefreshBodyData(_Data);
                
                frame.Dispose();
                frame = null;
            }
        }    
    }

    private void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }
        
        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }
            
            _Sensor = null;
        }
    }
}
