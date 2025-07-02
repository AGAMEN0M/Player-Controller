using UnityEngine.UI;
using UnityEngine;

public class StaminaMonitor : MonoBehaviour
{
    [SerializeField] private BasePlayer3D playerController;
    [SerializeField] private Slider slider;

    private void Start()
    {
        if (slider == null) return;

        slider.minValue = 0;
        slider.maxValue = 100;
    }

    private void FixedUpdate()
    {
        if (slider == null) return;

        slider.value = playerController.StaminaPercent;
    }
}