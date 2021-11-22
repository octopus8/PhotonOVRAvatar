using O8C;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// "Game select" game state controller.
/// 
/// This component is a singleton to provide access to this component to room list items (OnRoomSelected)
/// and PhotonOVRAvatarPlayer (for access to TestOculusUserID).
/// </summary>
public class PhotonOVRAvatarMultiplayerGameSelect : MonoBehaviour
{

    #region Editor Parameters

    /// <summary>
    /// Room select view.
    /// </summary>
    [SerializeField]
    [Tooltip("Room select view")]
    GameObject roomSelectView;

    /// <summary>
    /// Joining game view.
    /// </summary>
    [SerializeField]
    [Tooltip("Joining game view")]
    GameObject joiningGameView;

    /// <summary>
    /// Room list vertical layout controller.
    /// </summary>
    [SerializeField]
    [Tooltip("Room list vertical layout controller.")]
    VerticalLayoutController roomListController;

    /// <summary>
    /// The region dropdown menu.
    /// </summary>
    [SerializeField]
    [Tooltip("The region dropdown menu.")]
    TMP_Dropdown regionDropdown;

    /// <summary>
    /// "Obtaining Available Games" text.
    /// </summary>
    [SerializeField]
    [Tooltip("\"Obtaining Available Games\" text.")]
    GameObject obtainingAvailableGamesText;

    /// <summary>
    /// "No Games Available" text.
    /// </summary>
    [SerializeField]
    [Tooltip("\"No Games Available\" text.")]
    GameObject noGamesAvailableText;

    /// <summary>
    /// "Joining Game" text.
    /// </summary>
    [SerializeField]
    [Tooltip("\"Joining Game\" text.")]
    TextMeshProUGUI joiningText;

    /// <summary>
    /// "Play" button.
    /// </summary>
    [SerializeField]
    [Tooltip("\"Play\" button.")]
    Button playButton;

    /// <summary>
    /// Player resource object.
    /// 
    /// This must be a prefab in a Resources directory.
    /// </summary>
    [SerializeField]
    [Tooltip("Player resource object. This must be a prefab in a Resources directory.")]
    GameObject playerResourceObject;

    /// <summary>
    /// Test Oculus user ID, if any.
    /// </summary>
    [SerializeField]
    [Tooltip("Test Oculus user ID, if any.")]
    string testOculusUserID;

    #endregion



    #region Static Public Variables

    /// <summary>
    /// Provides access to the testOculusUserID editor variable.
    /// </summary>
    static public string TestOculusUserID;

    #endregion



    #region Private Variables

    /// <summary>
    /// Max number of players per room.
    /// </summary>
    byte maxNumPlayersPerRoom = 4;

    /// <summary>
    /// Network timeout.
    /// </summary>
    int networkConnectTimeoutSeconds = 120;

    /// <summary>
    /// The selected room.
    /// </summary>
    string selectedRoom = null;

    /// <summary>
    /// Flag indicating the room list has been obtained.
    // This is used to prevent this method executing upon initial populuation
    // of the room list.
    /// </summary>
    bool hasObtainedRoomList = false;


    #endregion



    #region Singleton Instance


    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static PhotonOVRAvatarMultiplayerGameSelect Instance;


    #endregion



    #region MonoBehaviour Methods


    /// <summary>
    /// MonoBehavour method; the singleton instance is set.
    /// </summary>
    private void Awake()
    {
        // Store a reference to the singleton.
        Instance = this;

        // Set the public static test Oculus User ID to the editor variable.
        TestOculusUserID = testOculusUserID;
    }





    /// <summary>
    /// MonoBehaviour method; the UI is initialized and network connection is initialized.
    /// </summary>
    private void OnEnable()
    {
        // Initialize variables.
        hasObtainedRoomList = false;
        selectedRoom = null;

        // Initialize active states.
        roomSelectView.SetActive(true);
        joiningGameView.SetActive(false);
        obtainingAvailableGamesText.SetActive(true);
        noGamesAvailableText.SetActive(false);

        // Set the "room list update" callback.
        NetworkingPhoton.Instance.OnRoomListCallback += OnRoomList;

        // Disable the region dropdown and play buttons.
        regionDropdown.interactable = false;
        playButton.interactable = false;
        playButton.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";

        // Connect to the network when ready.
        StartCoroutine(ConnectToNetworkWhenReady());
    }





    /// <summary>
    /// MonoBehaviour method; callbacks are removed.
    /// </summary>
    private void OnDisable()
    {
        // Remove OnRoomList callback.
        // Check for null in case app is shutting down.
        if (null != NetworkingPhoton.Instance)
        {
            NetworkingPhoton.Instance.OnRoomListCallback -= OnRoomList;
        }
    }





    /// <summary>
    /// MonoBehaviour method; this code is completely for test purposes to provide
    /// input for menus when in VR.
    /// </summary>
    private void Update()
    {
        // If the primary button is pressed, then call "OnPlayButton".
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            OnPlayButton();
        }

        // If the secondary button is pressed, then connect to a specified region.
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            NetworkingPhoton.Instance.Disconnect();
            NetworkingPhoton.Instance.ConnectToRegion("usw");
        }
    }


    #endregion



    #region Public Methods


    /// <summary>
    /// Called by OnClick of the "Play" button.
    /// 
    /// An appropriate message is displayed and a room is joined/created.
    /// </summary>
    public void OnPlayButton()
    {
        // Set Multiplayer View as inactive.
        roomSelectView.SetActive(false);
        joiningGameView.SetActive(true);

        // Display an apprpriate message and create/join a room.
        if (roomListController.ItemCount() == 0)
        {
            joiningText.text = "Creating new game...";
            NetworkingPhoton.Instance.CreateRoom(OnRoomJoinSuccessful, OnRoomJoinFailed, playerResourceObject.name, OculusPlatform.Instance.LocalPlayerDisplayName, OculusPlatform.Instance.LocalPlayerDisplayName);
        }
        else
        {
            if (selectedRoom == null)
            {
                joiningText.text = "Joining random game...";
            }
            else
            {
                joiningText.text = $"Joining {selectedRoom}...";
            }

            // Attempt to join a random room or create a new one.
            NetworkingPhoton.Instance.JoinRoom(OnRoomJoinSuccessful, OnRoomJoinFailed, playerResourceObject.name, selectedRoom, OculusPlatform.Instance.LocalPlayerDisplayName);
        }
    }





    /// <summary>
    /// Called by OnValueChanged of the region dropdown, the selected region is connected to.
    /// </summary>
    /// <param name="idx">List index of the selected region.</param>
    public void OnRegionSelected(int idx)
    {
        // If the "hasObtainedRoomList" flag hasn't been set, then do nothing.
        // This is used to prevent this method executing upon initial populuation
        // of the room list.
        if (!hasObtainedRoomList)
        {
            return;
        }

        //  Set the play button as not interactable and clear its text.
        playButton.interactable = false;
        playButton.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "";

        // Display "Obtaining Available Games" message.
        noGamesAvailableText.SetActive(false);
        obtainingAvailableGamesText.SetActive(true);

        // Clear the displayed room list.
        roomListController.Clear();

        // Disconnect, then reconnect to the specified region.
        NetworkingPhoton.Instance.Disconnect();
        NetworkingPhoton.Instance.ConnectToRegion(NetworkingPhoton.Instance.RegionList[idx]);
    }





    /// <summary>
    /// Called by the OnClick of a room list item.
    /// 
    /// The selected room is stored, and the text for the "Play" button is updated.
    /// </summary>
    /// <param name="text"></param>
    public void OnRoomSelected(string text)
    {
        selectedRoom = text;
        playButton.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = $"Join {text}";
    }


    #endregion



    #region Private Methods


    /// <summary>
    /// Called by OnEnable to connect to the Photon network. The region list is also populuated (upon the network being ready).
    /// 
    /// This method waits for OculusPlatform to be ready before connecting to the network
    /// to ensure we have the Oculus user ID when needed.
    /// </summary>
    IEnumerator ConnectToNetworkWhenReady()
    {
        while (!OculusPlatform.Instance.IsReady)
        {
            yield return null;
        }
        NetworkingPhoton.Instance.Connect(OnNetworkingConnectSuccess, OnNetworkingConnectFailed, PhotonOVRAvatar.NetworkVersion, maxNumPlayersPerRoom, networkConnectTimeoutSeconds);
        InitRegionList();
    }






    /// <summary>
    /// Callback called upon failing to join a room. Currently does nothing.
    /// </summary>
    void OnRoomJoinFailed()
    {
    }





    /// <summary>
    /// Callback called upon obtaining a room list. This callback is called
    /// any time the room list changes.
    /// </summary>
    void OnRoomList(List<NetworkingPhoton.RoomInfo> roomList)
    {
        // Set the "Obtaining Available Games" text inactive.
        obtainingAvailableGamesText.SetActive(false);

        // Clear the displayed room list.
        roomListController.Clear();

        // Go through the room list data.
        bool hasRooms = false;
        foreach (NetworkingPhoton.RoomInfo roomInfo in roomList)
        {
            // If the room is not open or was removed from the list, ignore.
            if (!roomInfo.IsOpen || roomInfo.RemovedFromList)
            {
                continue;
            }

            // Set the "has rooms" flag.
            hasRooms = true;

            // Get a new list item and initialize.
            PhotonOVRAvatarMultiplayerGameSelectListItem newItem = roomListController.AddItem().GetComponent<PhotonOVRAvatarMultiplayerGameSelectListItem>();
            newItem.gameObject.name = roomInfo.Name;
            newItem.SetName(roomInfo.Name);
            newItem.gameObject.SetActive(true);
            newItem.transform.localScale = Vector3.one;
            newItem.transform.localRotation = Quaternion.identity;
            Vector3 pos = newItem.transform.localPosition;
            pos.z = 0;
            newItem.transform.localPosition = pos;
        }

        // Set the "has obtained room list" flag.
        hasObtainedRoomList = true;

        // Display the appropriate message and set the join button text according to games available.
        TextMeshProUGUI joinButtonText = playButton.gameObject.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        if (!hasRooms)
        {
            noGamesAvailableText.SetActive(true);
            joinButtonText.text = "Start New Game";
        }
        else
        {
            noGamesAvailableText.SetActive(false);
            joinButtonText.text = "Join Random Game";
        }

        // Set the play button as interactable.
        playButton.interactable = true;
    }





    /// <summary>
    /// Callback called upon successful connection to the network. Nothing needs to be done currently.
    /// </summary>
    void OnNetworkingConnectSuccess()
    {
    }

    



    /// <summary>
    /// Callback called upon failing to connect to the network.
    /// 
    /// Currently, only an error message is logged.
    /// </summary>
    /// <param name="error"></param>
    void OnNetworkingConnectFailed(NetworkingPhoton.Error error)
    {
        Debug.LogError($"Network connection failed. Reason: {error.ToString()}");
    }





    /// <summary>
    /// Initializes the region list. The dropdown is set as not interactable
    /// and the list data is updated upon being available.
    /// </summary>
    void InitRegionList()
    {
        regionDropdown.interactable = false;
        StartCoroutine(PopulateRegionListAsync());
    }





    /// <summary>
    /// Waits for network connection, then populuates the region list.
    /// </summary>
    IEnumerator PopulateRegionListAsync()
    {
        // Wait for network connection.
        // BLEE Note: This also should check for a connection error.
        while (!NetworkingPhoton.Instance.IsConnected())
        {
            yield return null;
        }

        // Get the region list.
        string[] regions = NetworkingPhoton.Instance.RegionList;

        // Get the current region name. Trim any path in the name.
        int currentRegionIndex = 0;
        string currentRegionName = NetworkingPhoton.Instance.GetRegionName();
        if (currentRegionName.Contains("/"))
        {
            currentRegionName = currentRegionName.Substring(0, currentRegionName.IndexOf("/"));
        }

        // Go through the region list.
        foreach (string region in regions)
        {
            // Convert the Photon region name to text and add it to the dropdown.
            TMP_Dropdown.OptionData regionData = new TMP_Dropdown.OptionData();
            regionData.text = NetworkingPhoton.Instance.RegionTokenToName(region);
            regionDropdown.options.Add(regionData);

            // If this is the currently selected region, then store the list index.
            if (region.Equals(currentRegionName))
            {
                currentRegionIndex = regionDropdown.options.Count - 1;
            }
        }

        // Set the currently selected region and enable interaction on the dropdown.
        regionDropdown.value = currentRegionIndex;
        regionDropdown.interactable = true;
    }





    /// <summary>
    /// Callback called upon successfully joining a room.
    /// </summary>
    /// <param name="playerGameObject">The player game object</param>
    /// <param name="numPlayers">The current number of players in the room.</param>
    void OnRoomJoinSuccessful(GameObject playerGameObject, int numPlayers)
    {
        // Remove the "Joining Game" view.
        joiningGameView.SetActive(false);
    }


    #endregion


}
