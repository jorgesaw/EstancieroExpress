using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RobberyUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Button[] cards;           // 9 botones: CardA..CardI
    [SerializeField] Button btnClose;
    [SerializeField] TMP_Text title;
    [SerializeField] TMP_Text subtitle;

    [Header("Anim")]
    [SerializeField] float fadeTime = 0.25f;

    public enum RobberyCardType
    {
        Steal15, Steal20, Steal25,    // robas % del dinero del rival
        StealProperty,                // robas 1 propiedad; si no tiene → 25% dinero
        Double15,                     // robas 15% dos veces (en 2 jugadores = 30% total)
        Null,                         // sin efecto
        Jail, JailLose5, JailLose10   // vas a cárcel; Lose5/10 además pierdes % al rival
    }

    public struct RobberyResult
    {
        public RobberyCardType type;
        public int index; // índice de carta elegida (0..8) o -1 si fue "Cerrar"
    }

    Action<RobberyResult> onResult;
    readonly RobberyCardType[] deckAssigned = new RobberyCardType[9];
    bool isOpen;

    void Awake()
    {
        if (btnClose) btnClose.onClick.AddListener(OnClose);

        // Enlazar los botones una sola vez
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                int ix = i;
                if (cards[i]) cards[i].onClick.AddListener(() => OnCard(ix));
            }
        }

        gameObject.SetActive(false);
        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    // Mostrar el pop-up y barajar las 9 cartas
    public void Show(string t, string s, Action<RobberyResult> onPick)
    {
        onResult = onPick;
        if (title) title.text = t;
        if (subtitle) subtitle.text = s;

        BuildDeck();

        // Reset de cartas
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (!cards[i]) continue;
                cards[i].interactable = true;

                var img = cards[i].GetComponent<Image>();
                if (img) img.color = Color.white; // “dorso”/reset visual
            }
        }

        StopAllCoroutines();
        gameObject.SetActive(true);

        if (canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;   // ⬅ habilita interacción
            canvasGroup.blocksRaycasts = true; // ⬅ recibe clics
        }

        StartCoroutine(Fade(0f, 1f));
        isOpen = true;
    }

    void BuildDeck()
    {
        var deck = new List<RobberyCardType>
        {
            RobberyCardType.Steal15,
            RobberyCardType.Steal20,
            RobberyCardType.Steal25,
            RobberyCardType.StealProperty,
            RobberyCardType.Double15,
            RobberyCardType.Null,
            RobberyCardType.Jail,
            RobberyCardType.JailLose5,
            RobberyCardType.JailLose10
        };

        // Mezcla Fisher–Yates
        for (int i = 0; i < deck.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, deck.Count);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }

        for (int i = 0; i < deckAssigned.Length; i++)
            deckAssigned[i] = deck[i];
    }

    void OnCard(int index)
    {
        if (!isOpen) return;

        // Bloquea todo y “revela” la elegida
        if (cards != null)
        {
            for (int i = 0; i < cards.Length; i++)
                if (cards[i]) cards[i].interactable = false;
        }

        var img = (index >= 0 && index < cards.Length) ? cards[index]?.GetComponent<Image>() : null;
        if (img) img.color = new Color(0.95f, 0.9f, 0.6f, 1f); // mini-highlight

        var res = new RobberyResult { type = deckAssigned[index], index = index };
        StartCoroutine(Hide(true, res));
    }

    void OnClose()
    {
        if (!isOpen) return;

        // Cerrar = tratamos como “Null” para que el flujo del GameManager no se quede colgado
        var res = new RobberyResult { type = RobberyCardType.Null, index = -1 };
        StartCoroutine(Hide(true, res));
    }

    IEnumerator Hide(bool fire, RobberyResult res = default)
    {
        if (!isOpen) yield break;
        isOpen = false;

        yield return Fade(1f, 0f);

        if (canvasGroup)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        gameObject.SetActive(false);

        if (fire) onResult?.Invoke(res);
        onResult = null;
    }

    IEnumerator Fade(float from, float to)
    {
        if (!canvasGroup) yield break;

        float t = 0f;
        canvasGroup.alpha = from;

        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        canvasGroup.alpha = to;

        bool enable = to > 0.5f;
        canvasGroup.interactable = enable;
        canvasGroup.blocksRaycasts = enable;
    }
}
