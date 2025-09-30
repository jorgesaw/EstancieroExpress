using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyController : MonoBehaviourPunCallbacks
{
    [Header("Config")]
    [SerializeField] string versionJuego = "1.0.0";
    [SerializeField] string fixedRegion = "sa";           // MISMA región en todos
    [SerializeField] string nombreEscenaJuego = "MainScene";

    [Header("UI")]
    [SerializeField] TMP_Text txtStatus;
    [SerializeField] Transform contentRooms;              // contenedor vacío (sin layout)
    [SerializeField] Button btnCreate;                    // "Crear Mesa"
    [SerializeField] Button btnReady;                     // "READY"
    [SerializeField] GameObject countdownPanel;           // panel 3-2-1
    [SerializeField] TMP_Text txtCountdown;

    // ---- almacenamiento de botones por mesa (sin layout groups) ----
    readonly Dictionary<string, Button> roomButtons = new();
    readonly List<string> roomOrder = new();   // para posicionar en columna
    const float itemHeight = 90f;
    const float itemSpacing = 12f;

    bool isInRoom = false;
    bool iAmReady = false;

    // Player custom prop key
    const string kReady = "ready";

    // Room props
    const string kStarted = "started";
    const string kStartTime = "startTime";  // double (PhotonNetwork.Time)
    const string kSeed = "seed";       // int (opcional)

    // ====== LOG helpers ======
    void L(string tag, string msg) => Debug.Log($"{tag} {msg}");
    void LWarning(string tag, string msg) => Debug.LogWarning($"{tag} {msg}");
    void SetStatus(string s) { if (txtStatus) txtStatus.text = s; L("[Lobby/UI]", s); }

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.GameVersion = versionJuego;

        if (!string.IsNullOrEmpty(fixedRegion))
        {
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = fixedRegion;
            L("[Lobby/Setup]", $"FixedRegion='{fixedRegion}'");
        }

        if (countdownPanel) countdownPanel.SetActive(false);

        if (btnReady)
        {
            btnReady.interactable = false;
            btnReady.onClick.AddListener(ToggleReady);
        }
        if (btnCreate) btnCreate.onClick.AddListener(CreateRandomRoom);

        // Log inicial de estado de red
        L("[Lobby/Net]",
          $"connected={PhotonNetwork.IsConnected} inRoom={PhotonNetwork.InRoom} region={PhotonNetwork.CloudRegion}");
    }

    void Start()
    {
        SetStatus("Conectando a Photon…");
        if (!PhotonNetwork.IsConnected)
            PhotonNetwork.ConnectUsingSettings();
        else
            PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    // ================== UI: creación manual de botones ==================
    Button MakeRoomButton(string roomName)
    {
        if (roomButtons.TryGetValue(roomName, out var exists))
            return exists;

        var go = new GameObject(roomName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(contentRooms, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(760, itemHeight);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); // top center
        rt.pivot = new Vector2(0.5f, 1f);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(go.transform, false);
        var rtt = textGO.GetComponent<RectTransform>();
        rtt.anchorMin = rtt.anchorMax = new Vector2(0.5f, 0.5f);
        rtt.pivot = new Vector2(0.5f, 0.5f);
        rtt.sizeDelta = new Vector2(740, itemHeight - 20);

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = roomName;
        tmp.alignment = TextAlignmentOptions.Midline;
        tmp.fontSize = 44;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.85f, 0.85f, 0.9f, 1f);

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => JoinRoom(roomName));

        roomButtons[roomName] = btn;
        roomOrder.Add(roomName);
        ReflowButtons();

        L("[Room/UI]", $"Creado botón '{roomName}'");
        return btn;
    }

    void UpdateRoomButton(RoomInfo info)
    {
        var btn = MakeRoomButton(info.Name);
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = $"{info.Name}    {info.PlayerCount}/{info.MaxPlayers}";
        btn.interactable = info.IsOpen && info.PlayerCount < info.MaxPlayers;

        L("[Room/List]",
          $"Mesa='{info.Name}' players={info.PlayerCount}/{info.MaxPlayers} isOpen={info.IsOpen} isVisible={info.IsVisible}");
    }

    void RemoveRoomButton(string roomName)
    {
        if (roomButtons.TryGetValue(roomName, out var btn))
        {
            if (btn) Destroy(btn.gameObject);
            roomButtons.Remove(roomName);
            roomOrder.Remove(roomName);
            ReflowButtons();
            L("[Room/UI]", $"Eliminado botón '{roomName}'");
        }
    }

    void ReflowButtons()
    {
        float topY = contentRooms.GetComponent<RectTransform>().rect.height * 0.5f - 20f;
        for (int i = 0; i < roomOrder.Count; i++)
        {
            string name = roomOrder[i];
            if (!roomButtons.TryGetValue(name, out var btn) || btn == null) continue;
            var rt = btn.GetComponent<RectTransform>();
            float y = topY - i * (itemHeight + itemSpacing);
            rt.anchoredPosition = new Vector2(0f, y);
        }
    }

    // ================== Lobby Flow + LOGS ==================
    public override void OnConnected()
    {
        L("[Lobby/Net]", "OnConnected()");
    }

    public override void OnConnectedToMaster()
    {
        SetStatus($"Conectado a Master (region={PhotonNetwork.CloudRegion}). Entrando al lobby…");
        L("[Lobby/Net]", $"IsMasterClient={PhotonNetwork.IsMasterClient}");
        PhotonNetwork.JoinLobby(TypedLobby.Default);
    }

    public override void OnJoinedLobby()
    {
        SetStatus("En Lobby. Si no ves mesas, tocá 'Crear Mesa'.");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        L("[Room/List]", $"Actualización ({roomList.Count} items)");
        foreach (var info in roomList)
        {
            if (info.RemovedFromList)
            {
                L("[Room/List]", $"REMOVED '{info.Name}'");
                RemoveRoomButton(info.Name);
                continue;
            }
            UpdateRoomButton(info);
        }
        if (roomButtons.Count == 0) SetStatus("No hay mesas. Tocá 'Crear Mesa'.");
    }

    void CreateRandomRoom()
    {
        string roomName = "Mesa " + Random.Range(1, 99).ToString("00");

        var opts = new RoomOptions
        {
            MaxPlayers = 2,
            IsOpen = true,
            IsVisible = true,
            CleanupCacheOnLeave = true,
            PublishUserId = true
        };
        var props = new ExitGames.Client.Photon.Hashtable
        {
            { kStarted,   false },
            { kSeed,      Random.Range(int.MinValue, int.MaxValue) }
        };
        opts.CustomRoomProperties = props;
        opts.CustomRoomPropertiesForLobby = new[] { kStarted };

        SetStatus($"Creando {roomName}…");
        PhotonNetwork.CreateRoom(roomName, opts, TypedLobby.Default);
    }

    void JoinRoom(string roomName)
    {
        if (isInRoom) return;
        SetStatus($"Uniéndome a {roomName}…");
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        LWarning("[Room/Create]", $"FAILED rc={returnCode} msg={message}");
        SetStatus("Error al crear mesa.");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        LWarning("[Room/Join]", $"FAILED rc={returnCode} msg={message}");
        SetStatus("Error al unirse a la mesa.");
    }

    public override void OnJoinedRoom()
    {
        isInRoom = true;
        var r = PhotonNetwork.CurrentRoom;
        SetStatus($"En sala: {r.Name}  {r.PlayerCount}/2  | isMaster={PhotonNetwork.IsMasterClient}");
        L("[Room/Props]",
          $"started={GetRoomBool(kStarted)} seed={GetRoomInt(kSeed)}");

        SetMyReady(false);
        if (btnReady) btnReady.interactable = true;

        // Log de jugadores actuales
        foreach (var p in PhotonNetwork.PlayerList)
            L("[Room/Player]", $"Actualmente en sala: actor={p.ActorNumber} userId={p.UserId}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        L("[Room/PlayerEntered]",
          $"actor={newPlayer.ActorNumber} userId={newPlayer.UserId} count={PhotonNetwork.CurrentRoom.PlayerCount}/2");
        TryStartIfBothReadyAndFull();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        L("[Room/PlayerLeft]",
          $"actor={otherPlayer.ActorNumber} userId={otherPlayer.UserId} count={PhotonNetwork.CurrentRoom.PlayerCount}/2");

        if (PhotonNetwork.IsMasterClient)
        {
            var room = PhotonNetwork.CurrentRoom;
            if (room != null)
            {
                room.IsOpen = true;
                room.IsVisible = true;
                room.SetCustomProperties(new ExitGames.Client.Photon.Hashtable { { kStarted, false } });
                L("[Room/Props]", "started=false (alguien se fue; abort countdown)");
            }
        }
        if (countdownPanel) countdownPanel.SetActive(false);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        LWarning("[Lobby/Net]", $"Disconnected: {cause}");
        SetStatus("Desconectado.");
    }

    // ================== READY ==================
    void ToggleReady()
    {
        SetMyReady(!iAmReady);
        TryStartIfBothReadyAndFull();
    }

    void SetMyReady(bool ready)
    {
        iAmReady = ready;
        if (btnReady) btnReady.GetComponentInChildren<TextMeshProUGUI>().text = ready ? "UNREADY" : "READY";

        var props = new ExitGames.Client.Photon.Hashtable { { kReady, ready } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        L("[Ready/Me]",
          $"actor={PhotonNetwork.LocalPlayer.ActorNumber} ready={ready}");
    }

    bool AllReadyAndFull()
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return false;
        if (room.PlayerCount < 2) return false;

        foreach (var p in PhotonNetwork.PlayerList)
        {
            bool ok = p.CustomProperties.TryGetValue(kReady, out var v) && v is bool b && b;
            L("[Ready/Check]", $"actor={p.ActorNumber} ready={ok}");
            if (!ok) return false;
        }
        return true;
    }

    void TryStartIfBothReadyAndFull()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!AllReadyAndFull()) return;

        if (GetRoomBool(kStarted))
        {
            L("[CD]", "Ya estaba started=true; ignorado.");
            return;
        }

        double startAt = PhotonNetwork.Time + 3.0;
        var room = PhotonNetwork.CurrentRoom;

        room.IsOpen = false;
        room.IsVisible = false;
        room.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { kStarted, true },
            { kStartTime, startAt }
        });

        L("[CD]", $"Comienza countdown. startTime={startAt} (netTime={PhotonNetwork.Time})");
        SetStatus("Partida lista. Iniciando cuenta atrás…");
    }

    // ================== Countdown + cargar escena ==================
    void Update()
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return;

        if (GetRoomBool(kStarted) && room.CustomProperties.TryGetValue(kStartTime, out var t) && t is double startAt)
        {
            double remain = startAt - PhotonNetwork.Time;
            int sec = Mathf.Max(0, Mathf.CeilToInt((float)remain));

            if (countdownPanel) countdownPanel.SetActive(true);
            if (txtCountdown) txtCountdown.text = sec > 0 ? sec.ToString() : "¡Vamos!";

            if (sec > 0)
                SetStatus($"Comienza en {sec}…");

            if (remain <= 0.0)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    // 👇 ESTA ES LA LÍNEA QUE DEBES AGREGAR
                    PlayerPrefs.SetInt("MODE", 3); // 3 = Online 1 vs 1

                    L("[CD]", "Countdown terminado → LoadLevel(MainScene)");
                    PhotonNetwork.LoadLevel(nombreEscenaJuego);
                }
            }
        }
    }

    // ================== Helpers ==================
    bool GetRoomBool(string key)
    {
        var r = PhotonNetwork.CurrentRoom;
        if (r == null) return false;
        return r.CustomProperties.TryGetValue(key, out var v) && v is bool b && b;
    }

    int GetRoomInt(string key, int def = 0)
    {
        var r = PhotonNetwork.CurrentRoom;
        if (r == null) return def;
        return r.CustomProperties.TryGetValue(key, out var v) && v is int i ? i : def;
    }
}
