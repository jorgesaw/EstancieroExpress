using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyEntry : MonoBehaviour
{
    // Abrir el lobby para modo 1 vs 1
    public void IrAlLobbyPvP()
    {
        // Guardamos el modo para que GameManager lo lea al entrar a MainScene
        PlayerPrefs.SetInt("MODE", 3); // 3 = Online 1v1 (tus valores)
        SceneManager.LoadScene("LobbyScene");
    }

    // (opcional) si más adelante querés un botón "Online vs CPU":
    public void IrAlLobbyVsCPU()
    {
        PlayerPrefs.SetInt("MODE", 4); // 4 = Online vs CPU
        SceneManager.LoadScene("LobbyScene");
    }
}
