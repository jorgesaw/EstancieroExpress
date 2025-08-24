using TMPro;
using UnityEngine;

#if UNITY_EDITOR
// Quitar ExecuteAlways para no tocar nada salvo que lo pidas
#endif
public class TileIndexLabel : MonoBehaviour
{
    [Tooltip("Si est� activado, muestra 00..39. Si est� apagado, no toca el texto.")]
    public bool debugShowIndices = false;

    TMP_Text txt;
    Tile tile;

    void OnEnable() { Cache(); UpdateNow(); }

#if UNITY_EDITOR
    void OnValidate() { Cache(); UpdateNow(); } // s�lo en editor
#endif

    void Cache()
    {
        if (!txt) txt = GetComponent<TMP_Text>();
        if (!tile) tile = GetComponentInParent<Tile>();
    }

    void UpdateNow()
    {
        if (!txt || !tile) return;

        if (debugShowIndices)
        {
            // S�lo si activ�s el modo debug:
            txt.text = tile.index.ToString("00");
        }
        // Si est� apagado, NO tocamos el texto (se queda el nombre que puso GameManager)
    }
}
