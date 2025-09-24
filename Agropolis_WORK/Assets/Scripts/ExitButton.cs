using UnityEngine;

public class ExitButton : MonoBehaviour
{
    public ModalPrompt modal; // arrastra aqu� tu ModalPrompt de la escena

    public void OnClickExit()
    {
        if (!modal) return;

        modal.Show(
            "�Cerrar el juego?",
            "Se cerrar� la aplicaci�n.",
            "Salir",
            "Cancelar",
            onYes: () =>
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false; // Detiene el play en Unity
#else
                Application.Quit(); // Cierra la app en el m�vil
#endif
            },
            onNo: () => { }
        );
    }
}
