using UnityEngine;
using System.Collections;

public class ColorWheel : MonoBehaviour 
{
    public Transform ActualColorWheel;
    public Transform ColorWheelSelector;
    public Transform ColorWheelSelectorPreview;

    private Material ColorWheelMaterial;
    private Texture2D ColorWheelTexture;
    private Material PreviewMaterial;

    private Color CurrentColor = Color.green;

    private void Awake()
    {
        ColorWheelMaterial = ActualColorWheel.GetComponent<Renderer>().material;
        ColorWheelTexture = (Texture2D)ColorWheelMaterial.mainTexture;
        PreviewMaterial = ColorWheelSelectorPreview.GetComponent<Renderer>().material;
    }

    public Color Select(Vector2 position)
    {
        ColorWheelSelector.localPosition = new Vector3(position.x, position.y, -0.1f);
        ColorWheelSelector.localPosition = ColorWheelSelector.localPosition * 0.35f;

        RaycastHit raycastHit;
        bool result = Physics.Raycast(ColorWheelSelector.position, -ColorWheelSelector.forward, out raycastHit);

        if (result)
        {
            CurrentColor = ColorWheelTexture.GetPixel((int)(raycastHit.textureCoord.x * ColorWheelTexture.width), (int)(raycastHit.textureCoord.y * ColorWheelTexture.height));
            PreviewMaterial.color = CurrentColor;
        }

        return CurrentColor;
    }
}
