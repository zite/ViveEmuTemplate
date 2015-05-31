using UnityEngine;
using System.Collections;

using InControl;

public class Builder : MonoBehaviour 
{
    public GameObject ColorWheelPrefab;

    private BodySourceView BodyView;

    private Transform[] SelectedObject = new Transform[2];
    private Color[] CurrentColors = new Color[2];
    private ColorWheel[] ColorWheels = new ColorWheel[2];

    private void Awake()
    {
        BodyView = GameObject.FindObjectOfType<BodySourceView>();
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

        CheckController(leftController.LeftTrigger, leftController.LeftStickButton, leftController.LeftStick, 0, BodyView.CurrentAvatar.LeftHand.position);
        CheckController(rightController.RightTrigger, rightController.RightStickButton, rightController.RightStick, 1, BodyView.CurrentAvatar.RightHand.position);
    }

    private void CheckController(InputControl trigger, InputControl stickButton, TwoAxisInputControl stick, int selectedNum, Vector3 controllerPosition)
    {
        if (trigger.IsPressed)
        {
            if (SelectedObject[selectedNum] == null)
            {
                SelectedObject[selectedNum] = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
                SelectedObject[selectedNum].GetComponent<Renderer>().material.color = CurrentColors[selectedNum];
            }

            SelectedObject[selectedNum].position = controllerPosition;
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

            ColorWheels[selectedNum].transform.position = controllerPosition;
            ColorWheels[selectedNum].transform.LookAt(BodyView.CurrentAvatar.Head);
        }
        else
        {
            if (ColorWheels[selectedNum] != null)
                Destroy(ColorWheels[selectedNum].gameObject);
        }
    }
}
