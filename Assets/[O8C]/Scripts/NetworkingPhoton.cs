using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace O8C
{
    /// <summary>
    /// Interface to Photon Networking.
    /// </summary>
    public class NetworkingPhoton : MonoBehaviourPunCallbacks
    {

        #region Public Variables

        /// <summary>
        /// Callback called upon a networked player connecting.
        /// </summary>
        public Action OnNetworkedPlayerConnected;

        /// <summary>
        /// Callback called upon connecting to a room.
        /// </summary>
        public Action<GameObject, int> ConnectedToRoomCallback;

        /// <summary>
        /// Callback called upon connecting to a room.
        /// </summary>
        public Action ConnectToRoomFailedCallback;

        /// <summary>
        /// Callback called upon timing out connecting to the master server.
        /// </summary>
        public Action ConnectToMasterTimeoutCallback;

        /// <summary>
        /// Callback called upon leaving the room.
        /// </summary>
        public Action OnLeftRoomCallback;

        /// <summary>
        /// Callback called upon a player's properties being updated.
        /// </summary>
        public Action<GameObject, object, object> PlayerPropertiesUpdated;

        /// <summary>
        /// Flag indicating the network connection timed out.
        /// </summary>
        public bool ConnectionTimedOut { get; private set; } = false;

        /// <summary>
        /// Callback called upon the room list being updated.
        /// </summary>
        public Action<List<RoomInfo>> OnRoomListCallback;

        /// <summary>
        /// Callback called upon the region list being updated.
        /// </summary>
        public Action OnRegionListCallback;

        /// <summary>
        /// Callback called upon failing to create/join a room.
        /// </summary>
        public Action OnCreateJoinFailed;

        public string[] RegionList { get; private set; }


        #endregion



        #region Static Public Variables

        /// <summary>
        /// Flag indicating the instance is still alive.
        /// </summary>   
        static public bool IsAlive = true;

        /// <summary>
        /// Singleton instance.
        /// </summary>
        static public NetworkingPhoton Instance
        {
            get
            {
                if (!IsAlive)
                {
                    return null;
                }
                if (null == _Instance)
                {
                    GameObject go = new GameObject("NetworkingPhoton");
                    _Instance = go.AddComponent<NetworkingPhoton>();
                    DontDestroyOnLoad(go);
                }
                return _Instance;
            }

            private set
            {
                _Instance = value;
            }
        }

        #endregion



        #region Private Variables
        /// @name Private Variables
        ///
        ///@{
        ///

        /// <summary>
        /// Configuration variable; used to lock the region.
        /// </summary>
        string fixedRegion = "";// "us";

        /// <summary>
        /// The singleton instance.
        /// </summary>
        static NetworkingPhoton _Instance;

        /// <summary>
        /// The maximum number of players per room.
        /// </summary>
        byte maxNumPlayersPerRoom;

        /// <summary>
        /// Coroutine used to detect connection timeout.
        /// </summary>
        Coroutine connectTimeoutCoroutine;

        /// <summary>
        /// Flag indicating in the process of connecting to the master server.
        /// </summary>
        bool isConnectingToMaster = false;

        /// <summary>
        /// The current room list.
        /// </summary>
        List<RoomInfo> roomList = new List<RoomInfo>();

        /// <summary>
        /// Callback called upon successful connection to the master server.
        /// This callback is specified in the Connect method parameters.
        /// </summary>
        Action onConnectToMasterSuccessCallback;

        /// <summary>
        /// Callback called upon unsuccessful connection to the master server.
        /// This callback is specified in the Connect method parameters.
        /// </summary>
        Action<Error> onConnectFailedCallback;

        /// <summary>
        /// Callback called upon successful joining a room.
        /// This callback is specified in the JoinRoom method parameters.
        /// </summary>
        Action<GameObject, int> onJoinRoomSuccessfulCallback;

        /// <summary>
        /// The name of the player resource object.
        /// This is specified in the parameters when joining or creating a game.
        /// </summary>
        string playerResourceObjectName;

        /// <summary>
        /// A list of player GameObjects.
        /// 
        /// This list is maintained to send and receive player property values.
        /// </summary>
        List<GameObject> playerGameObjects = new List<GameObject>();

        ///@}
        #endregion



        #region MonoBehaviour Callbacks

        /// <summary>
        /// Lifecycle method; Clears the "isAlive" flag.
        /// </summary>
        private void OnDestroy()
        {
            IsAlive = false;
        }

        #endregion



        #region Public Methods
        /// @name Public Methods
        ///
        ///@{

        /// <summary>
        /// Called to connect to the master server.
        /// </summary>
        public void Connect(Action onConnectSuccessCallback, Action<Error> onConnectFailedCallback, string photonGameVersion, byte maxNumPlayersPerRoom, float connectTimeoutSeconds)
        {
            // If already attempting to connect, do nothing.
            if (isConnectingToMaster)
            {
                return;
            }

            // If already connected, do nothing.
            if (IsConnected())
            {
                return;
            }

            // Store parameters for use later.
            Debug.Log("Photon connecting.");
            this.onConnectToMasterSuccessCallback = onConnectSuccessCallback;
            this.onConnectFailedCallback = onConnectFailedCallback;
            this.maxNumPlayersPerRoom = maxNumPlayersPerRoom;

            // Set photon game version.
            PhotonNetwork.GameVersion = photonGameVersion;

            // If a fixed region has been specifed, then use the specified region.
            if ((null != fixedRegion) && (fixedRegion.Length != 0))
            {
                PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = fixedRegion;
            }

            // Connect to a master server and start the timeout coroutine.
            ConnectionTimedOut = false;
            connectTimeoutCoroutine = StartCoroutine(ConnectTimeout(connectTimeoutSeconds));
            isConnectingToMaster = true;
            PhotonNetwork.ConnectUsingSettings();
        }





        /// <summary>
        /// Returns current master server connection status.
        /// </summary>
        public bool IsConnected()
        {
            return (PhotonNetwork.IsConnectedAndReady && (PhotonNetwork.NetworkingClient.CloudRegion != null));
        }





        public void OnNewNetworkedUser(GameObject gameObject)
        {
            playerGameObjects.Add(gameObject);
        }





        public void OnDestroyNetworkedUser(GameObject gameObject)
        {
            playerGameObjects.Remove(gameObject);
        }





        public void ConnectToRegion(string regionName)
        {
            PhotonNetwork.ConnectToRegion(regionName);
        }





        public void Disconnect()
        {
            PhotonNetwork.Disconnect();
        }





        public string GetRegionName()
        {
            return PhotonNetwork.NetworkingClient.CloudRegion;
        }





        /// <summary>
        /// Called to join a room.
        /// </summary>
        /// <param name="roomName">The name of the room to join. If null, then a random room is joined.</param>
        public void JoinRoom(Action<GameObject, int> joinRoomSuccessfulCallback, Action joinRoomFailedCallback, string playerPrefabName, string roomName, string userName, bool isPrivate = false, string password = null)
        {
            PhotonNetwork.NickName = userName;

            this.onJoinRoomSuccessfulCallback = joinRoomSuccessfulCallback;
            this.playerResourceObjectName = playerPrefabName;

            if ((null == roomName) || (roomName.Length == 0))
            {
                Debug.Log("Photon joining random room.");
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                Debug.Log("Photon Joining room: " + roomName);
                PhotonNetwork.JoinRoom(roomName);
            }
        }





        public void CreateRoom(Action<GameObject, int> joinedRoomCallback, Action joinRoomFailedCallback, string playerPrefabName, string roomName, string userName, bool isPrivate = false, string password = null)
        {
            PhotonNetwork.NickName = userName;

            this.onJoinRoomSuccessfulCallback = joinedRoomCallback;
            this.playerResourceObjectName = playerPrefabName;

            if ((null == roomName) || (roomName.Length == 0))
            {
                Debug.Log("Photon joining random room.");
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                // Set the custom room properties.
                Debug.Log("Photon Join/Create room: " + roomName);
                RoomOptions roomOptions = new RoomOptions();
                ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
                customProperties.Add("IsPrivate", isPrivate);
                if (isPrivate)
                {
                    customProperties.Add("Password", password);
                }


                roomOptions.CustomRoomProperties = customProperties;

                // Create the room.
                PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
            }
        }





        /// <summary>
        /// Called to leave the current room.
        /// </summary>
        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom();
        }





        public string GetCurrentRoomName()
        {
            if (null == PhotonNetwork.CurrentRoom)
            {
                return null;
            }
            return PhotonNetwork.CurrentRoom.Name;
        }





        public Error DisconnectCauseToError(DisconnectCause cause)
        {
            switch (cause)
            {
                case DisconnectCause.DnsExceptionOnConnect:
                    return Error.DnsExceptionOnConnect;
                default:
                    return Error.Unknown;
            }
        }


        ///@}
        #endregion



        #region Photon Callbacks

        /// <summary>
        /// Note: I think a room list update is done automatically upon connecting to a lobby and whenever it changes.
        /// </summary>
        /// <param name="roomListPhoton"></param>
        public override void OnRoomListUpdate(List<Photon.Realtime.RoomInfo> roomListPhoton)
        {
            roomList.Clear();
            foreach (Photon.Realtime.RoomInfo roomInfoPhoton in roomListPhoton)
            {
                RoomInfo roomInfo = new RoomInfo();
                roomInfo.IsOpen = roomInfoPhoton.IsOpen;
                roomInfo.RemovedFromList = roomInfoPhoton.RemovedFromList;
                roomInfo.Name = roomInfoPhoton.Name;
                roomList.Add(roomInfo);
            }

            OnRoomListCallback?.Invoke(roomList);
        }





        public override void OnRegionListReceived(RegionHandler regionHandler)
        {
            base.OnRegionListReceived(regionHandler);
            RegionList = regionHandler.SummaryToCache.Substring(regionHandler.SummaryToCache.LastIndexOf(';') + 1).Split(',');
            OnRegionListCallback?.Invoke();
        }





        /// <summary>
        /// MonoBehaviourPunCallbacks method called when custom player properties are changed.
        /// 
        /// 
        /// </summary>
        public override void OnPlayerPropertiesUpdate(Player target, ExitGames.Client.Photon.Hashtable changedProps)
        {
            // Call the required base.
            base.OnPlayerPropertiesUpdate(target, changedProps);

            // If the local target property changed, then do nothing; it should already have been acted upon.
            if (target.IsLocal)
            {
                return;
            }

            // Find the target in the set of player GameObjects.
            GameObject targetGO = null;
            foreach (GameObject go in playerGameObjects)
            {
                if (go.GetComponent<PhotonView>().Owner.Equals(target))
                {
                    targetGO = go;
                    break;
                }
            }

            // If the player could not be found, then do nothing.
            if (null == targetGO)
            {
                return;
            }

            // Send each changed property to the target player GameObject.
            foreach (DictionaryEntry value in changedProps)
            {
                PlayerPropertiesUpdated?.Invoke(targetGO, value.Key, value.Value);
            }
        }





        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            OnNetworkedPlayerConnected?.Invoke();
        }





        /// <summary>
        /// Called by Photon upon successfully connecting to
        /// a master server.        
        /// </summary>
        public override void OnConnectedToMaster()
        {
            // Call the required base method.
            base.OnConnectedToMaster();

            if (ConnectionTimedOut == true)
            {
                Debug.Log("Photon connected to master, BUT TIMEOUT EXPIRED! Increase the timeout to avoid this situation.");
                PhotonNetwork.Disconnect();
                return;
            }

            // Stop the timeout coroutine.
            Debug.Log("Photon connected to master in region " + PhotonNetwork.CloudRegion + ".");
            StopCoroutine(connectTimeoutCoroutine);

            //        IsConnected = true;
            //        onConnectedCallback();

            PhotonNetwork.JoinLobby(TypedLobby.Default);
        }





        public override void OnJoinedLobby()
        {
            base.OnJoinedLobby();

            isConnectingToMaster = false;
            Debug.Log("Photon joined lobby.");
            onConnectToMasterSuccessCallback?.Invoke();
            onConnectToMasterSuccessCallback = null;
        }





        /// <summary>
        /// Called by Photon upon failing to join a speccified room.
        ///
        /// Upon failing to join a specified room, the registered callbacks are called.
        /// </summary>
        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            base.OnJoinRoomFailed(returnCode, message);

            ConnectToRoomFailedCallback?.Invoke();
        }





        /// <summary>
        /// Called by Photon upon failing to join a random room.
        ///
        /// Upon failing to join a random room, it is assumed
        /// there are no rooms, and therefore a new room is created.
        /// </summary>
        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            // Call the required base method.
            base.OnJoinRandomFailed(returnCode, message);

            // Create a room.
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxNumPlayersPerRoom });
        }




        /// <summary>
        /// Called by Photon upon successfully joining a room,
        /// this method instantiates the networked player GameObject,
        /// then calls the associated delegate.
        /// </summary>
        public override void OnJoinedRoom()
        {
            // Call the required base method.
            base.OnJoinedRoom();

            // Instantiate the networked player GameObject.
            Debug.Log("Photon joined room: #players: " + PhotonNetwork.PlayerList.Length);
            Debug.Log("Instantiating networked player.");
            GameObject player = PhotonNetwork.Instantiate(playerResourceObjectName, new Vector3(0, 0, 0), Quaternion.identity, 0);

            // Call the delegate.
            onJoinRoomSuccessfulCallback(player, PhotonNetwork.PlayerList.Length);
        }





        public override void OnLeftRoom()
        {
            OnLeftRoomCallback?.Invoke();
        }





        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);

            if (isConnectingToMaster)
            {
                onConnectFailedCallback?.Invoke(DisconnectCauseToError(cause));
                isConnectingToMaster = false;
            }
            else
            {
                Debug.Log($"Photon disconnected: {cause.ToString()}");
            }
        }


        ///@}
        #endregion



        #region Private Methods

        /// <summary>
        /// This coroutine is started upon attempting to connect
        /// to a master server, and canceled upon successfully
        /// connecting.
        ///
        /// If this coroutine completes waiting before it is cancelled,
        /// the timeout delegate is called.
        /// </summary>        
        IEnumerator ConnectTimeout(float connectTimeoutSeconds)
        {
            yield return new WaitForSeconds(connectTimeoutSeconds);
            ConnectionTimedOut = true;
            ConnectToMasterTimeoutCallback?.Invoke();
        }





        public string RegionTokenToName(string token)
        {
            switch (token)
            {
                case "asia":
                    return "Asia";
                case "au":
                    return "Austraila";
                case "cae":
                    return "Canada";
                case "cn":
                    return "China";
                case "eu":
                    return "Europe";
                case "in":
                    return "India";
                case "jp":
                    return "Japan";
                case "ru":
                    return "Russia";
                case "rue":
                    return "Russia-East";
                case "za":
                    return "South Africa";
                case "sa":
                    return "South America";
                case "kr":
                    return "Korea";
                case "us":
                    return "USA-East";
                case "usw":
                    return "USA-West";
                case "tr":
                    return "Turkey";
                default:
                    return $"\"{token}\"";
            }
        }

        #endregion



        #region Data Types

        /// <summary>
        /// Error codes. A facade for Photon error codes.
        /// </summary>
        public enum Error
        {
            DnsExceptionOnConnect,
            Unknown
        }





        /// <summary>
        /// Room info. A facade for Photon room info.
        /// </summary>
        public class RoomInfo
        {
            public bool IsOpen;
            public bool RemovedFromList;
            public string Name;

        }


        ///@}
        #endregion

    }
}