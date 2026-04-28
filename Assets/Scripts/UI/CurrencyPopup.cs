using UnityEngine;
using TMPro;
using System.Collections;

public class CurrencyPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI totalText;
    [SerializeField] private TextMeshProUGUI incomingText;
    [SerializeField] private CanvasGroup incomingCanvasGroup;
    [SerializeField] private float countDuration = 0.8f;
    [SerializeField] private float displayDuration = 1.5f;
    [SerializeField] private float fadeDuration = 0.5f;

    private int trueTotalAmount = 0;    // actual total in inventory
    private int displayedTotal = 0;     // what the UI is currently showing
    private int pendingIncoming = 0;    // the "+ X" amount still counting

    private Coroutine countRoutine;
    private Coroutine hideRoutine;

    private void OnEnable()
    {
        EnemyDamageable.OnEnemyDeath += HandleCurrencyGained;
    }

    private void OnDisable()
    {
        EnemyDamageable.OnEnemyDeath -= HandleCurrencyGained;
    }

    private void Start()
    {
        incomingCanvasGroup.alpha = 0f;
        totalText.text = $"Total: {trueTotalAmount} gold";
    }

    private void HandleCurrencyGained(int amount)
    {
        trueTotalAmount += amount;
        pendingIncoming += amount;

        incomingCanvasGroup.alpha = 1f;

        if (hideRoutine != null) StopCoroutine(hideRoutine);
        if (countRoutine != null) StopCoroutine(countRoutine);

        countRoutine = StartCoroutine(CountUpRoutine());
    }

    private IEnumerator CountUpRoutine()
    {
        int startTotal = displayedTotal;
        int targetTotal = trueTotalAmount;
        int startIncoming = pendingIncoming;

        float elapsed = 0f;

        while (elapsed < countDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / countDuration);

            int counted = Mathf.RoundToInt(Mathf.Lerp(0, startIncoming, t));

            displayedTotal = startTotal + counted;
            pendingIncoming = startIncoming - counted;

            totalText.text = $"{displayedTotal}";
            incomingText.text = $"+ {pendingIncoming}";

            yield return null;
        }

        // Snap to exact values in case of floating point drift
        displayedTotal = targetTotal;
        pendingIncoming = 0;
        totalText.text = $"{displayedTotal}";
        incomingText.text = "+ 0";

        hideRoutine = StartCoroutine(FadeOutIncoming());
    }

    private IEnumerator FadeOutIncoming()
    {
        yield return new WaitForSeconds(displayDuration);

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            incomingCanvasGroup.alpha = 1f - (elapsed / fadeDuration);
            yield return null;
        }

        incomingCanvasGroup.alpha = 0f;
    }
}