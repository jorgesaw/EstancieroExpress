using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RobberyUI : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup canvasGroup;
    public Button cardA;
    public Button cardB;
    public Button cardC;
    public Button btnClose;
    public TMP_Text title;
    public TMP_Text subtitle;

    [Header("Anim")]
    public float fadeTime = 0.25f;

    Action<int> onPick; // 0=A, 1=B, 2=C

    void Awake()
    {
        // Estado inicial oculto
        HideImmediate();

        // Wire de botones
        if (cardA) cardA.onClick.AddListener(() => Pick(0));
        if (cardB) cardB.onClick.AddListener(() => Pick(1));
        if (cardC) cardC.onClick.AddListener(() => Pick(2));
        if (btnClose) btnClose.onClick.AddListener(() =>
        {
            // Si cierra sin elegir, tomamos "Nada" (1)
            Pick(1);
        });
    }

    public void Show(string titleText, string subtitleText, string a, string b, string c, Action<int> pickCb)
    {
        onPick = pickCb;

        if (title) title.text = titleText;
        if (subtitle) subtitle.text = subtitleText;

        // Poner el texto en los TMP de los botones
        SetBtnText(cardA, a);
        SetBtnText(cardB, b);
        SetBtnText(cardC, c);

        StopAllCoroutines();
        StartCoroutine(FadeTo(1f));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(FadeTo(0f));
    }

    void HideImmediate()
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    IEnumerator FadeTo(float target)
    {
        if (!canvasGroup) yield break;

        float start = canvasGroup.alpha;
        float t = 0f;

        // habilitar solo cuando se muestre
        if (target > 0f)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / fadeTime);
            canvasGroup.alpha = Mathf.Lerp(start, target, k);
            yield return null;
        }

        canvasGroup.alpha = target;

        // deshabilitar al ocultar
        if (Mathf.Approximately(target, 0f))
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    void Pick(int i)
    {
        onPick?.Invoke(i);
        Hide();
    }

    static void SetBtnText(Button b, string txt)
    {
        if (!b) return;
        var t = b.GetComponentInChildren<TMP_Text>();
        if (t) t.text = txt;
    }
}
