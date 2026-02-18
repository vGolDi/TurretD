using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image fill;
    [SerializeField] private Image background;

    private Camera mainCamera;

    // Art Deco color scheme
    private static readonly Color HealthFull =
        new Color(0.29f, 0.87f, 0.5f, 0.9f);
    private static readonly Color HealthMid =
        new Color(0.96f, 0.75f, 0.14f, 0.9f);
    private static readonly Color HealthLow =
        new Color(0.97f, 0.44f, 0.44f, 0.9f);
    private static readonly Color BgColor =
        new Color(0.04f, 0.06f, 0.1f, 0.7f);

    private void Start()
    {
        mainCamera = Camera.main;

        if (background != null)
            background.color = BgColor;
    }

    private void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(
                transform.position +
                mainCamera.transform.rotation *
                    Vector3.forward,
                mainCamera.transform.rotation *
                    Vector3.up);
        }
    }

    public void SetMaxHealth(int health)
    {
        if (slider == null) return;

        slider.maxValue = health;
        slider.value = health;
        UpdateColor();
    }

    public void SetHealth(int health)
    {
        if (slider == null) return;

        slider.value = health;
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (fill == null || slider == null) return;

        float normalized = slider.normalizedValue;

        if (normalized > 0.5f)
            fill.color = Color.Lerp(
                HealthMid, HealthFull,
                (normalized - 0.5f) * 2f);
        else
            fill.color = Color.Lerp(
                HealthLow, HealthMid,
                normalized * 2f);
    }
}
//using UnityEngine;
//using UnityEngine.UI;

//public class HealthBar : MonoBehaviour
//{
//    [SerializeField] private Slider slider;
//    [SerializeField] private Gradient gradient;
//    [SerializeField] private Image fill;

//    private Camera mainCamera;

//    private void Start()
//    {
//        mainCamera = Camera.main;
//    }

//    private void LateUpdate()
//    {
//        // Make healthbar face camera (billboard effect)
//        if (mainCamera != null)
//        {
//            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
//                           mainCamera.transform.rotation * Vector3.up);
//        }
//    }

//    public void SetMaxHealth(int health)
//    {
//        if (slider == null) return;

//        slider.maxValue = health;
//        slider.value = health;

//        if (fill != null)
//            fill.color = gradient.Evaluate(1f);
//    }

//    public void SetHealth(int health)
//    {
//        if (slider == null) return;

//        slider.value = health;

//        if (fill != null)
//            fill.color = gradient.Evaluate(slider.normalizedValue);
//    }
//}
