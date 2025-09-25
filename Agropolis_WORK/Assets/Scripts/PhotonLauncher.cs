using UnityEngine;
using Photon.Pun; // <- Importante para usar Photon

public class PhotonLauncher : MonoBehaviourPunCallbacks
{
    void Start()
    {
        Debug.Log("Conectando a Photon...");
        PhotonNetwork.ConnectUsingSettings(); // Usa la configuración de PhotonServerSettings
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Conectado a Photon Master Server!");
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.LogWarning("❌ Desconectado de Photon: " + cause);
    }
}
