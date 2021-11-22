using System;
using UnityEngine.Android;


/// <summary>
/// This class provides functionality for getting granted microphone permission and for requesting
/// microphone permission.
/// </summary>
public class MicrophonePermission
{
    // Flag indicating permission has been granted.
    static bool hasPermission = false;


    /// <summary>
    /// Accessor for hasPermission value.
    /// </summary>
    static public bool HasPermission
    {
        get
        {
            // If it already has permission, then return true.
            if (hasPermission)
            {
                return true;
            }

            // If Android permission has already been granted, then set true and return true.
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                HasPermission = true;
                return true;
            }
#elif UNITY_EDITOR
            HasPermission = true;
            return true;
#endif
            return false;
        }
        set
        {
            hasPermission = value;
        }
    }





    public static void RequestPermissions(Action<string> onPermissionGranted, Action<string> onPermissionDenied, Action<string> onPermissionDeniedDontAskAgain)
    {
#if UNITY_2020_2_OR_NEWER
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionDenied += onPermissionDenied;
        callbacks.PermissionGranted += onPermissionGranted;
        callbacks.PermissionDeniedAndDontAskAgain += onPermissionDeniedDontAskAgain;
        Permission.RequestUserPermission(Permission.Microphone, callbacks);
#else
                Permission.RequestUserPermission(Permission.Microphone);
#endif
    }

}
