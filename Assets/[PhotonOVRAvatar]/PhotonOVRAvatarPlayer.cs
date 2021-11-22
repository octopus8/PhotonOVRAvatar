using O8C;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


/// <summary>
/// This component is attached to the player prefab used by Photon.
/// </summary>
public class PhotonOVRAvatarPlayer : MonoBehaviour, IPunObservable
{

    #region Editor Variables


    /// <summary>
    /// The local Oculus avatar prefab.
    /// </summary>
    [SerializeField]
    [Tooltip("The local Oculus avatar prefab.")]
    GameObject localAvatar;

    /// <summary>
    /// The remote Oculus avatar prefab.
    /// </summary>
    [SerializeField]
    [Tooltip("The remote Oculus avatar prefab.")]
    GameObject remoteAvatar;


    #endregion



    #region Private Variables


    /// <summary>
    /// The instantiated avatar.
    /// </summary>
    OvrAvatar ovrAvatar;

    /// <summary>
    /// A reference to the PhotonView component on the player.
    /// </summary>
    PhotonView photonView;

    /// <summary>
    /// A reference to the remote OvrAvatarRemoteDriver, if the player is remote.
    /// </summary>
    OvrAvatarRemoteDriver remoteDriver;

    /// <summary>
    /// A reference to the remote loopback OvrAvatarRemoteDriver, if the player is local.
    /// </summary>
    OvrAvatarRemoteDriver remoteLoopbackDriver;

    /// <summary>
    /// Avatar packet data.
    /// </summary>
    List<byte[]> packetData;

    /// <summary>
    /// Packet sequence.
    /// </summary>
    int PacketSequence = 0;

    /// <summary>
    /// The player's Oculus user ID. If the player is a local player, this is either set from the
    /// specified test data, or from the Oculus API. If the player is remote, it is set from PlayerPropertiesUpdated.
    /// </summary>
    string oculusUserID = null;

    /// <summary>
    /// The key for the Oculus user ID in the Photon custom properties.
    /// </summary>
    string photonCustomPropertyKeyOculusUserID = "OculusUserID";


    #endregion



    #region MonoBehaviour Methods


    /// <summary>
    /// MonoBehaviour method; stores references.
    /// </summary>
    void Awake()
    {
        photonView = GetComponent<PhotonView>();
    }




    /// <summary>
    /// MonoBehaviour method; adds a PlayerPropertiesUpdated callback.
    /// </summary>
    void OnEnable()
    {
        // Add player properties update callback.
        NetworkingPhoton.Instance.PlayerPropertiesUpdated += PlayerPropertiesUpdated;
    }





    /// <summary>
    /// MonoBehaviour method; removes the NetworkingPhoton PlayerPropertiesUpdated and the Oculus avatar PacketRecorded callback.
    /// </summary>
    void OnDisable()
    {
        // Remove player properties update callback.
        if (null != NetworkingPhoton.Instance)
        {
            NetworkingPhoton.Instance.PlayerPropertiesUpdated -= PlayerPropertiesUpdated;
        }

        // If this is the local player, then stop recording packets.
        if (photonView.IsMine)
        {
            if (null != ovrAvatar)
            {
                ovrAvatar.RecordPackets = false;
                ovrAvatar.PacketRecorded -= OnLocalAvatarPacketRecorded;
            }
        }
    }




    /// <summary>
    /// MonoBehaviour method; the Oculus user ID is initialized (if available) and the Oculus avatar is created.
    /// </summary>
    void Start()
    {
        // If this is the local player, then first set the Oculus user ID to the test data (may be null or empty).
        if (photonView.IsMine)
        {
            oculusUserID = PhotonOVRAvatarMultiplayerGameSelect.TestOculusUserID;
        }

        // If the Oculus user ID is not set, then set it to the custom property value.
        // Note: It is possible the custom property has not been set yet. This will be handled
        // by CreateAvatar appropriately.
        if ((null == oculusUserID) || (oculusUserID.Length == 0))
        {
            oculusUserID = (string)photonView.Owner.CustomProperties[photonCustomPropertyKeyOculusUserID];
        }

        // Create the avatar.
        StartCoroutine(CreateAvatar());
    }


    #endregion




    #region IPunObservable Methods

    /// <summary>
    /// IPunObservable method; 
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="info"></param>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // If writing, then send the packetData.
        if (stream.IsWriting)
        {
            // If no data, then do nothing.
            if (packetData.Count == 0)
            {
                return;
            }

            // Send the packet data count.
            stream.SendNext(packetData.Count);

            // Send the packet data.
            foreach (byte[] b in packetData)
            {
                stream.SendNext(b);
            }

            // Clear the packet data.
            packetData.Clear();
        }

        // If reading, then receive the packetData.
        if (stream.IsReading)
        {
            // Get the packet data count.
            int num = (int)stream.ReceiveNext();

            // Get the packet data and feed it to the remote avatar driver.
            for (int counter = 0; counter < num; ++counter)
            {
                byte[] data = (byte[])stream.ReceiveNext();
                DeserializeAndQueuePacketData(remoteDriver, data);
            }
        }
    }


    #endregion



    #region Private Methods


    /// <summary>
    /// This method creates the player's avatar. If local, then a local avatar is created and packets
    /// are recorded. If remote, then a remote avatar is created.
    /// Note: When creating the local avatar, a "loopback" avatar is also created. This was done for testing
    /// purposes.
    /// </summary>
    IEnumerator CreateAvatar()
    {
        // Wait for the OculusPlatform singleton component to be ready.
        while (!OculusPlatform.Instance.IsReady)
        {
            yield return null;
        }

        // Set the player's name in Photon.
        PhotonNetwork.NickName = OculusPlatform.Instance.LocalPlayerDisplayName;

        // Create the avatar depending on whether it is the local avatar or not.
        if (photonView.IsMine)
        {
            // Instantiate the local avatar.
            GameObject ovrAvatarGO = Instantiate(localAvatar);

            // Initialize the avatar transform.
            ovrAvatarGO.transform.parent = transform;
            ovrAvatarGO.transform.localPosition = new Vector3(-0.4f, 0, 0.5f);
            ovrAvatarGO.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Initialize the OVRAvatar.
            ovrAvatar = ovrAvatarGO.GetComponent<OvrAvatar>();
            if ((null == oculusUserID) || (oculusUserID.Length == 0))
            {
                oculusUserID = OculusPlatform.Instance.LocalPlayerId.ToString();
            }
            ovrAvatar.oculusUserID = oculusUserID;
            ovrAvatar.RecordPackets = true;
            ovrAvatar.PacketRecorded += OnLocalAvatarPacketRecorded;

            // Set Oculus user ID in Photon custom properties.
            ExitGames.Client.Photon.Hashtable customProperties = new ExitGames.Client.Photon.Hashtable();
            customProperties.Add(photonCustomPropertyKeyOculusUserID, ovrAvatar.oculusUserID);
            PhotonNetwork.SetPlayerCustomProperties(customProperties);

            // If using PhotonOVRAvatarLipSync, then set the local avatar.
            if (null != PhotonOVRAvatarLipSync.Instance)
            {
                PhotonOVRAvatarLipSync.Instance.SetLocalAvatar(ovrAvatar);
            }

            // Initialize the packet data.
            packetData = new List<byte[]>();


            /** Init loopback avatar **/
            // Instantiate the loopback avatar.
            ovrAvatarGO = Instantiate(remoteAvatar);

            // Initialize the loopback avatar transform.
            ovrAvatarGO.transform.parent = transform;
            ovrAvatarGO.transform.localPosition = new Vector3(0.4f, 0, 0.5f);
            ovrAvatarGO.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Initialize the loopback OVRAvatar.
            OvrAvatar ovrLoopbackAvatar = ovrAvatarGO.GetComponent<OvrAvatar>();
            ovrLoopbackAvatar.oculusUserID = oculusUserID;
            ovrLoopbackAvatar.RecordPackets = false;
            remoteLoopbackDriver = ovrAvatarGO.GetComponent<OvrAvatarRemoteDriver>();
        }
        else
        {
            // If the Oculus user ID has not been set yet, then wait to receive the Oculus user ID.
            // Note: If a test user ID was specified in the editor, oculusUserID will already be set
            // at this point.
            while (null == oculusUserID)
            {
                yield return null;
            }

            // Instantiate the avatar.
            GameObject ovrAvatarGO = Instantiate(remoteAvatar);

            // Initialize the avatar transform.
            ovrAvatarGO.transform.parent = transform;
            ovrAvatarGO.transform.localPosition = new Vector3(0, 0, 0.5f);
            ovrAvatarGO.transform.localRotation = Quaternion.Euler(0, 180, 0);

            // Initialize the OVRAvatar.
            ovrAvatar = ovrAvatarGO.GetComponent<OvrAvatar>();            
            ovrAvatar.oculusUserID = oculusUserID;

            // Store a reference to the OVR avatar remote driver.
            remoteDriver = ovrAvatarGO.GetComponent<OvrAvatarRemoteDriver>();
        }
    }



    /// <summary>
    /// Callback called upon avatar packets recorded. The packet data is processed and stored in packetData.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    void OnLocalAvatarPacketRecorded(object sender, OvrAvatar.PacketEventArgs args)
    {
        // If not in a room, then do nothing.
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        using (MemoryStream outputStream = new MemoryStream())
        {
            // Create a BinaryWriter for the outputStream.
            BinaryWriter writer = new BinaryWriter(outputStream);

            // Create and fill a byte array with the packet data.
            var size = Oculus.Avatar.CAPI.ovrAvatarPacket_GetSize(args.Packet.ovrNativePacket);
            byte[] data = new byte[size];
            Oculus.Avatar.CAPI.ovrAvatarPacket_Write(args.Packet.ovrNativePacket, size, data);

            // Write the data metadata and data to the output stream.
            writer.Write(PacketSequence++);
            writer.Write(size);
            writer.Write(data);

            // Add the output stream to the packet data list.
            byte[] outputStreamArray = outputStream.ToArray();
            packetData.Add(outputStreamArray);

            // Feed the output stream array to the remote loopback avatar.
            DeserializeAndQueuePacketData(remoteLoopbackDriver, outputStreamArray);
        }
    }




    /// <summary>
    /// Called by OnPhotonSerializeView when reading data to pass the data to the remote avatar.
    /// </summary>
    /// <param name="driver">The remote driver to pass the data to.</param>
    /// <param name="data">The packet data.</param>
    void DeserializeAndQueuePacketData(OvrAvatarRemoteDriver driver, byte[] data)
    {
        // If no driver was specified, then do nothing.
        if (null == driver)
        {
            return;
        }

        // Process the data.
        using (MemoryStream inputStream = new MemoryStream(data))
        {
            // Create a BinaryReader for the inputStream.
            BinaryReader reader = new BinaryReader(inputStream);

            // Get the sequence number.
            int remoteSequence = reader.ReadInt32();

            // Get the size of the data.
            int size = reader.ReadInt32();

            // Get the data.
            byte[] sdkData = reader.ReadBytes(size);

            // Convert the data into a native packet.
            System.IntPtr packet = Oculus.Avatar.CAPI.ovrAvatarPacket_Read((System.UInt32)data.Length, sdkData);

            // Queue the packet in the remote driver.
            driver.QueuePacket(remoteSequence, new OvrAvatarPacket { ovrNativePacket = packet });
        }
    }





    /// <summary>
    /// Callback called upon player properties update.
    /// 
    /// If this player is the target of the property update and if the property
    /// is the Oculus user ID, then the oculusUserID value is updated.
    /// </summary>
    /// <param name="target">The target of the property update</param>
    /// <param name="key">The property key.</param>
    /// <param name="value">The property value.</param>
    void PlayerPropertiesUpdated(GameObject target, object key, object value)
    {
        // If this game object is the target...
        if (target == gameObject)
        {
            // If the key is the oculusUserIDKey, then set the Oculus user ID.
            string keyStr = (string)key;
            string valueStr = (string)value;
            if (keyStr.Equals(photonCustomPropertyKeyOculusUserID))
            {
                oculusUserID = valueStr;
            }
        }
    }


    #endregion

}
