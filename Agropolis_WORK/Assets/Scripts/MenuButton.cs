using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButton : MonoBehaviour
{
    // Llamado por el botón
    public void GoToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
