using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    // Llamado por el bot�n
    public void GoToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
