using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using InControl;

public class Builder : MonoBehaviour
{
    public GameObject ColorWheelPrefab;
    public GameObject AimPrefab;
    public GameObject BulletPrefab;

    private BodySourceView BodyView;

    private Transform[] SelectedObject = new Transform[2];
    private Color[] CurrentColors = new Color[2];
    private ColorWheel[] ColorWheels = new ColorWheel[2];
    private Aim[] GunAim = new Aim[2];

    private int BuiltLayer;

    private void Awake()
    {
        BodyView = GameObject.FindObjectOfType<BodySourceView>();
        BuiltLayer = LayerMask.NameToLayer("Built");
    }

    private void Update()
    {
        if (BodyView.CurrentAvatar == null)
            return;

        InputDevice leftController = null;
        InputDevice rightController = null;

        if (InputManager.Devices.Count >= 2)
        {
            leftController = InputManager.Devices[0];
            rightController = InputManager.Devices[1];
        }
        else if (InputManager.Devices.Count == 1)
        {
            leftController = InputManager.Devices[0];
            rightController = InputManager.Devices[0];
        }
        else
        {
            //no controllers attached
            return;
        }

        CheckController(leftController.LeftTrigger, leftController.LeftBumper, leftController.LeftStickButton, leftController.LeftStick, 0, BodyView.CurrentAvatar.GetLeftHandRay());
        CheckController(rightController.RightTrigger, rightController.RightBumper, rightController.RightStickButton, rightController.RightStick, 1, BodyView.CurrentAvatar.GetRightHandRay());
    }

    private IEnumerator PopIn(Transform obj)
    {
        float overTime = 0.15f;
        float startTime = Time.time;
        float endTime = startTime + overTime;
        
        Vector3 startScale = Vector3.one * 0.01f;
        Vector3 endScale = Vector3.one;

        obj.localScale = startScale;

        while (Time.time < endTime)
        {
            obj.localScale = Vector3.Lerp(startScale, endScale, (Time.time - startTime) / overTime);
            yield return null;
        }

        obj.localScale = endScale;
    }

    private void CheckController(InputControl trigger, InputControl bumper, InputControl stickButton, TwoAxisInputControl stick, int selectedNum, Ray controller)
    {
        if (trigger.IsPressed)
        {
            if (SelectedObject[selectedNum] == null)
            {
                Collider[] hits = Physics.OverlapSphere(controller.origin, 0.1f, 1 << BuiltLayer);;
                if (hits.Length > 0)
                {
                    Collider closest = hits.OrderBy(hit => Vector3.Distance(hit.transform.position, controller.origin)).First();
                    SelectedObject[selectedNum] = closest.transform;
                }
                else
                {
                    SelectedObject[selectedNum] = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                    SelectedObject[selectedNum].GetComponent<Renderer>().material.color = CurrentColors[selectedNum];
                    SelectedObject[selectedNum].gameObject.layer = BuiltLayer;

                    StartCoroutine(PopIn(SelectedObject[selectedNum]));
                }
            }

            SelectedObject[selectedNum].position = controller.origin;
        }
        else
            SelectedObject[selectedNum] = null;

        if (stickButton.IsPressed)
        {
            if (ColorWheels[selectedNum] == null)
            {
                ColorWheels[selectedNum] = GameObject.Instantiate(ColorWheelPrefab).GetComponent<ColorWheel>();
            }

            CurrentColors[selectedNum] = ColorWheels[selectedNum].Select(stick.Vector);

            if (SelectedObject[selectedNum] != null)
                SelectedObject[selectedNum].GetComponent<Renderer>().material.color = CurrentColors[selectedNum];

            ColorWheels[selectedNum].transform.position = controller.origin;
            ColorWheels[selectedNum].transform.LookAt(BodyView.CurrentAvatar.Head);
        }
        else
        {
            if (ColorWheels[selectedNum] != null)
                Destroy(ColorWheels[selectedNum].gameObject);
        }

        if (bumper.IsPressed)
        {
            if (GunAim[selectedNum] == null)
            {
                GunAim[selectedNum] = GameObject.Instantiate(AimPrefab).GetComponent<Aim>();
            }

            GunAim[selectedNum].DoUpdate(controller.origin, controller.direction);
        }
        else
        {
            if (GunAim[selectedNum] != null)
            {
                Destroy(GunAim[selectedNum].gameObject);

                GameObject bullet = GameObject.Instantiate<GameObject>(BulletPrefab);
                bullet.GetComponent<Bullet>().Launch(controller);
            }

        }
    }
}
