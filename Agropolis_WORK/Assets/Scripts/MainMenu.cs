using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string gameSceneName = "MainScene"; // ⚠️ pon aquí el nombre exacto de tu escena del tablero

    public void StartVsCPU()
    {
        PlayerPrefs.SetInt("MODE", 0); // 0 = vs CPU
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartVsLocal()
    {
        PlayerPrefs.SetInt("MODE", 1); // 1 = local 2 jugadores
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Detiene el Play en Unity
#else
        Application.Quit(); // Cierra la app en el móvil
#endif
    }
}
