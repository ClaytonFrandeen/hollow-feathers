using UnityEngine;

public class StaminaBar : MonoBehaviour
{
    [SerializeField] private DepletingBar staminaBar;
    [SerializeField] private PlayerVitals playerVitals;

    private float maxStamina;
    private float currentStamina;
    private float displayedStamina;

    private void Start()
    {
        maxStamina = playerVitals.maxStamina;
        currentStamina = playerVitals.currentStamina;
        // Initialize UI
        staminaBar.SetFillA(currentStamina / maxStamina);
        // Subscribe to updates
    }
    private void OnEnable()
    {
        playerVitals.OnStaminaChanged += HandleStaminaChanged;
    }

    private void OnDisable()
    {
        playerVitals.OnStaminaChanged -= HandleStaminaChanged;
    }
    private void Update()
    {
        float targetFill = currentStamina / maxStamina;

        displayedStamina = Mathf.Lerp(displayedStamina, targetFill, Time.deltaTime * 10f);

        staminaBar.SetFillA(displayedStamina);
    }

    private void HandleStaminaChanged(float current, float max)
    {
        currentStamina = current;
        maxStamina = max;

    }


}
