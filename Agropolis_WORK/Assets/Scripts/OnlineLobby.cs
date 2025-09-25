using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class OnlineLobby : MonoBehaviourPunCallbacks
{
    [Header("Config")]
    [SerializeField] string nombreEscenaTablero = "MainScene";
    [SerializeField] string versionJuego = "1.0.0";
    [SerializeField] string nombreSala = "agropolis_default";

    bool isJoining = false; // evita doble clic / llamadas duplicadas

    void Awake()
    {
        // MUY IMPORTANTE: el master carga la escena y el resto la siguen
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = versionJuego;
    }

    // =======================
    // BOTONES DEL MENÚ
    // =======================

    // Online vs CPU
    public void JugarOnlineVsCPU()
    {
        if (isJoining) return;
        isJoining = true;

        PlayerPrefs.SetInt("MODE", 4);  // 4 = online vs CPU (tu GameManager ya lo usa)
        ConectarYEntrarSala();
    }

    // Online 1 vs 1 (dos humanos)
    public void JugarOnlinePvP()
    {
        if (isJoining) return;
        isJoining = true;

        PlayerPrefs.SetInt("MODE", 3);  // 3 = online 1 vs 1
        ConectarYEntrarSala();
    }

    // =======================
    // CONEXIÓN / SALA
    // =======================

    void ConectarYEntrarSala()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("Conectando a Photon…");
            PhotonNetwork.ConnectUsingSettings(); // usa tu PhotonServerSettings (AppId + región)
        }
        else
        {
            EntrarSala();
        }
    }

    void EntrarSala()
    {
        var opts = new RoomOptions
        {
            MaxPlayers = 2,
            IsOpen = true,
            IsVisible = true
        };

        // meter el modo como propiedad de la sala (opcional pero útil)
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["mode"] = PlayerPrefs.GetInt("MODE", -1);
        opts.CustomRoomProperties = props;
        opts.CustomRoomPropertiesForLobby = new[] { "mode" };

        Debug.Log($"Entrando/Creando sala: {nombreSala} (mode={props["mode"]})");
        PhotonNetwork.JoinOrCreateRoom(nombreSala, opts, TypedLobby.Default);
    }

    // =======================
    // CALLBACKS PUN
    // =======================

    public override void OnConnectedToMaster()
    {
        Debug.Log("✅ Conectado a MasterServer. Uniéndose/creando sala…");
        EntrarSala();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"✅ OnJoinedRoom. Jugadores en sala: {PhotonNetwork.CurrentRoom.PlayerCount}");

        // El master carga la escena (se sincroniza a todos por AutomaticallySyncScene)
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Cargando escena de juego (master)...");
            PhotonNetwork.LoadLevel(nombreEscenaTablero);
        }
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        Debug.LogWarning($"❌ OnJoinRoomFailed ({returnCode}): {message}");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        Debug.LogWarning($"❌ OnCreateRoomFailed ({returnCode}): {message}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isJoining = false;
        Debug.LogWarning("❌ Desconectado de Photon: " + cause);
    }
}
