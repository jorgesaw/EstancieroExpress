using System.Collections;
using TMPro;
using UnityEngine;

public class ToastUI : MonoBehaviour
{
    public CanvasGroup canvasGroup;   // arrastra el CanvasGroup del objeto Toast
    public TMP_Text msg;              // arrastra el TMP_Text hijo "Msg"
    public float showTime = 3f;       // duración visible
    public float fadeTime = 0.25f;    // fundidos

    Coroutine co;

    void Reset()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!msg) msg = GetComponentInChildren<TMP_Text>();
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void Show(string text, float? seconds = null)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(CoShow(text, seconds ?? showTime));
    }

    IEnumerator CoShow(string text, float seconds)
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (msg) msg.text = text;

        // fade in
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup) canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeTime);
            yield return null;
        }
        if (canvasGroup) canvasGroup.alpha = 1f;

        // mantener visible
        float wait = Mathf.Max(0f, seconds - 2f * fadeTime);
        yield return new WaitForSecondsRealtime(wait);

        // fade out
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            if (canvasGroup) canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
            yield return null;
        }
        if (canvasGroup) canvasGroup.alpha = 0f;
        co = null;
    }
}
