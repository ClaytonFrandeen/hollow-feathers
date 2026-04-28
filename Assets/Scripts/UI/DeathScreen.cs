using System.Collections;
using UnityEngine;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup blackOverlay;
    [SerializeField] private CanvasGroup deathText;

    [SerializeField] private float fadeSpeed = 1.5f;
    [SerializeField] private float delayBeforeText = 0.5f;

    public void ShowDeathScreen()
    {
        gameObject.SetActive(true);
        StartCoroutine(FadeSequence());
    }

    private IEnumerator FadeSequence()
    {
        yield return StartCoroutine(FadeCanvasGroup(blackOverlay, 1f));

        yield return new WaitForSeconds(delayBeforeText);

        yield return StartCoroutine(FadeCanvasGroup(deathText, 1f));

        yield return new WaitForSeconds(3f);

        yield return StartCoroutine(FadeCanvasGroup(deathText, 0f));
        
        yield return new WaitForSeconds(0.25f);

        yield return StartCoroutine(FadeCanvasGroup(blackOverlay, 0f));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha)
    {
        while (!Mathf.Approximately(cg.alpha, targetAlpha))
        {
            cg.alpha = Mathf.MoveTowards(
                cg.alpha,
                targetAlpha,
                fadeSpeed * Time.unscaledDeltaTime
            );
            yield return null;
        }
    }
}
