using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;

public class OnlineLobby : MonoBehaviourPunCallbacks
{
    [Header("Config")]
    [SerializeField] string nombreEscenaTablero = "MainScene";
    [SerializeField] string versionJuego = "1.0.0";

    // IMPORTANTE: usar EXACTAMENTE la misma sala y región en ambos jugadores
    [SerializeField] string nombreSala = "agropolis_default";

    // Región fija (mismo valor en ambos dispositivos). Ejemplos: "sa", "usw", "use", "eu"
    [SerializeField] string fixedRegion = "sa";   // ← cambia si lo necesitas

    bool isJoining = false; // evita doble clic / llamadas duplicadas

    void Awake()
    {
        // El master carga la escena y el resto la siguen
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

        PlayerPrefs.SetInt("MODE", 4);  // 4 = online vs CPU
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
        // Aseguramos que ambos usen la MISMA región
        if (!string.IsNullOrEmpty(fixedRegion))
        {
            // Forzamos región antes de conectar
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = fixedRegion;
            Debug.Log($"[OnlineLobby] Región fija configurada: {fixedRegion}");
        }

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[OnlineLobby] Conectando a Photon…");
            PhotonNetwork.ConnectUsingSettings(); // usa PhotonServerSettings + FixedRegion
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
            IsVisible = true,
            CleanupCacheOnLeave = true,
            PublishUserId = true
        };

        // Meter el modo como propiedad de la sala (útil para depurar)
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["mode"] = PlayerPrefs.GetInt("MODE", -1);
        opts.CustomRoomProperties = props;
        opts.CustomRoomPropertiesForLobby = new[] { "mode" };

        Debug.Log($"[OnlineLobby] Entrando/Creando sala: {nombreSala} (mode={props["mode"]}, region={PhotonNetwork.CloudRegion})");
        PhotonNetwork.JoinOrCreateRoom(nombreSala, opts, TypedLobby.Default);
    }

    // =======================
    // CALLBACKS PUN
    // =======================

    public override void OnConnectedToMaster()
    {
        Debug.Log($"✅ Conectado a MasterServer (region={PhotonNetwork.CloudRegion}). Uniéndose/creando sala…");
        EntrarSala();
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"✅ OnJoinedRoom. Jugadores en sala: {PhotonNetwork.CurrentRoom.PlayerCount} (region={PhotonNetwork.CloudRegion})");

        // El master carga la escena (se sincroniza a todos por AutomaticallySyncScene)
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("[OnlineLobby] Cargando escena de juego (master)...");
            PhotonNetwork.LoadLevel(nombreEscenaTablero);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[OnlineLobby] Entró un jugador: {newPlayer.UserId} | Count={PhotonNetwork.CurrentRoom.PlayerCount}");
        // Si ya estamos en menú, el master podría cargar la escena aquí también (fallback)
        if (PhotonNetwork.IsMasterClient && SceneManager.GetActiveScene().name != nombreEscenaTablero)
        {
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
