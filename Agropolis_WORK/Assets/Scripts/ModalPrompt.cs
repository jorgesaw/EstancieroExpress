using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModalPrompt : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup canvasGroup;
    public RectTransform window;
    public TMP_Text txtTitle;
    public TMP_Text txtBody;
    public Button btnYes;
    public Button btnNo;
    public TMP_Text lblYes;
    public TMP_Text lblNo;

    [Header("Animación")]
    public float fadeTime = 0.15f;
    public float scaleIn = 1.03f;

    Action onYes, onNo;
    Coroutine anim;

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!window) window = transform as RectTransform;

        SetVisible(false, true);

        if (btnYes)
        {
            btnYes.onClick.RemoveAllListeners();
            btnYes.onClick.AddListener(() => { Hide(); onYes?.Invoke(); });
        }
        if (btnNo)
        {
            btnNo.onClick.RemoveAllListeners();
            btnNo.onClick.AddListener(() => { Hide(); onNo?.Invoke(); });
        }
    }

    public void Show(string title, string body, string yes = "Sí", string no = "No",
                     Action onYes = null, Action onNo = null)
    {
        this.onYes = onYes;
        this.onNo = onNo;
        if (txtTitle) txtTitle.text = title;
        if (txtBody) txtBody.text = body;
        if (lblYes) lblYes.text = yes;
        if (lblNo) lblNo.text = no;
        SetVisible(true, false);
    }

    public void Hide() => SetVisible(false, false);

    void SetVisible(bool visible, bool instant)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(CoFade(visible, instant));
    }

    IEnumerator CoFade(bool show, bool instant)
    {
        if (!canvasGroup) yield break;

        canvasGroup.blocksRaycasts = show;
        canvasGroup.interactable = show;

        float t = instant ? fadeTime : 0f;
        float from = canvasGroup.alpha;
        float to = show ? 1f : 0f;

        if (window) window.localScale = show ? Vector3.one * scaleIn : Vector3.one;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = fadeTime <= 0f ? 1f : Mathf.Clamp01(t / fadeTime);
            if (window)
            {
                float s = show ? Mathf.Lerp(scaleIn, 1f, k) : 1f;
                window.localScale = Vector3.one * s;
            }
            canvasGroup.alpha = Mathf.Lerp(from, to, k);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}
