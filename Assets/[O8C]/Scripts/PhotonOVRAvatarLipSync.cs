using Photon.Voice;
using Photon.Voice.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Connects Voice for PUN2 data to the Oculus Avatar lip sync.
/// 
/// This component is a singleton so the PhotonOVRAvatarPlayer can set the local avatar.
/// </summary>
public class PhotonOVRAvatarLipSync : MonoBehaviour, IProcessor<short>
{

    #region Private Variables

    /// <summary>
    /// The local avatar. This is set via a call to SetLocalAvatar.
    /// </summary>
    OvrAvatar ovrAvatar;

    /// <summary>
    /// Reference to the voice data.
    /// </summary>
    LocalVoiceAudioShort localVoiceAudioShort;

    #endregion



    #region Singleton Instance

    /// <summary>
    /// the singleton instance.
    /// </summary>
    static public PhotonOVRAvatarLipSync Instance { get; private set; } = null;

    #endregion



    #region MonoBehaviour Methods


    /// <summary>
    /// MonoBehaviour method; stores a reference to the singleton instance.
    /// </summary>
    void Awake()
    {
        Instance = this;
    }


    #endregion



    #region IProcessor methods


    /// <summary>
    /// IProcessor method; voice data preprocessor.
    /// 
    /// If the avatar has been specified, then update the avatar's voice data.
    /// </summary>
    /// <returns>The buffer passed in.</returns>
    public short[] Process(short[] buf)
    {
        if (null != ovrAvatar)
        {
            ovrAvatar.UpdateVoiceData(buf, 1);
        }
        return buf;
    }





    /// <summary>
    /// IProcessor method.
    /// 
    /// Does nothing.
    /// </summary>
    public void Dispose()
    {
    }

    #endregion



    #region Public Methods


    /// <summary>
    /// Called by PhotonOVRAvatarPlayer to specify the local avatar to apply the voice data to.
    /// 
    /// Stores a reference to the avatar.
    /// </summary>
    public void SetLocalAvatar(OvrAvatar avatar)
    {
        ovrAvatar = avatar;
    }


    #endregion



    #region Photon Recorder Message Receivers


    /// <summary>
    /// Photon Recorder message receiver.
    /// 
    /// Stores a reference to the voice data and adds a voice data preprocessor callback.
    /// </summary>
    void PhotonVoiceCreated(PhotonVoiceCreatedParams photonVoiceCreatedParams)
    {
        // If the voice data is of short type, store a reference to the voice data
        // and add a voice data preprocessor callback.
        if (photonVoiceCreatedParams.Voice is LocalVoiceAudioShort)
        {
            localVoiceAudioShort = photonVoiceCreatedParams.Voice as LocalVoiceAudioShort;
            localVoiceAudioShort.AddPreProcessor(this);
        }
    }





    /// <summary>
    /// Photon Recorder message receiver.
    /// 
    /// Clears the voice data preprocessor callback.
    /// </summary>
    void PhotonVoiceRemoved()
    {
        localVoiceAudioShort.ClearProcessors();
    }


    #endregion

}
