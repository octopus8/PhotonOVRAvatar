using Photon.Voice.Unity;
using UnityEngine;


/// <summary>
/// Requests microphone permission on Android.
/// </summary>
public class AndroidPhotonMicrophonePermission : MonoBehaviour
{

    #region Private Variables

    /// <summary>
    /// The PUN2 recorder component. AutoStart is set to true upon permissions being granted.
    /// </summary>
    Recorder recorder;

    /// <summary>
    /// Flag indicating permissions have been granted.
    /// </summary>
    bool permissionGranted = false;

    #endregion



    #region MonoBehaviour Methods

    /// <summary>
    /// MonoBehaviour method; stores references (and requests permissions if not yet granted).
    /// </summary>
    private void Start()
    {
        recorder = GetComponent<Recorder>();

        // Normally, a button will be used to turn on voice. In this case, voice is automatically turned on.
        OnRecord();
    }

    #endregion



    #region Private Methods

    /// <summary>
    /// Requests permissions if not yet granted.
    /// 
    /// Normally, called upon starting record. This is currently called in Start for simplicity sake.
    /// </summary>
    void OnRecord()
    {
        // If the permissionGranted flag has been set, then do nothing.
        if (permissionGranted)
        {
            return;
        }

        // If permissions have not been granted, then request permissions.
        if (!MicrophonePermission.HasPermission)
        {
            MicrophonePermission.RequestPermissions(OnPermissionGranted, OnPermissionDenied, OnPermissionDeniedDontAskAgain);
        }

        // Otherwise, permission has been previously granted. Call the OnPermissionGranted callback.
        else
        {
            OnPermissionGranted(null);
        }
    }





    /// <summary>
    /// Callback called upon permissions being granted. permissionGranted flag is set
    /// and AutoStart is set to true for the Recorder.
    /// </summary>
    void OnPermissionGranted(string permissionName)
    {
        permissionGranted = true;
        recorder.AutoStart = true;
    }





    /// <summary>
    /// Callback called upon permissions being denied. Currently does nothing.
    /// </summary>
    void OnPermissionDenied(string permissionName)
    {
    }





    /// <summary>
    /// Callback called upon a hard permission denial. Currently does nothing.
    /// </summary>
    void OnPermissionDeniedDontAskAgain(string permissionName)
    {
    }

    #endregion

}
