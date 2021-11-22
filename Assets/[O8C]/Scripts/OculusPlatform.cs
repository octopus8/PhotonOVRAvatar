using Oculus.Platform;
using Oculus.Platform.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace O8C
{
    public class OculusPlatform : SingletonAsComponent<OculusPlatform>
    {
        /// <summary>
        /// Flag set upon this component ready. Before this, requested data
        /// is likely wrong.
        /// </summary>
        public bool IsReady { get; private set; } = false;

        /// <summary>
        /// Flag indicating Oculus app entitlement.
        /// </summary>
        public bool IsEntitledToApp { get; private set; } = false;

        /// <summary>
        /// The local player Oculus user ID.
        /// </summary>
        public ulong LocalPlayerId { get; private set; } = 0;

        public string LocalPlayerDisplayName { get; private set; }


        /// <summary>
        /// Singleton Instance accessor.
        /// </summary>
        public static OculusPlatform Instance
        {
            get
            {
                OculusPlatform retval = ((OculusPlatform)_Instance);
                return retval;
            }

            set
            {
                _Instance = value;
            }
        }


        /// <summary>
        /// Initializes the Oculus core and checks user app entitlement.
        /// If the core cannot be initialized, then the app quits.
        /// </summary>
        void Awake()
        {
            DontDestroyOnLoad(gameObject);

            try
            {
                Core.AsyncInitialize();
                Entitlements.IsUserEntitledToApplication().OnComplete(EntitlementCallback);
            }
            catch (UnityException e)
            {
                Debug.LogError("Platform failed to initialize due to exception.");
                Debug.LogException(e);
                UnityEngine.Application.Quit();
            }
        }


        /// <summary>
        /// Callback called upon checking user app entitlement.
        /// If the user is not entitled, then the app quits. Otherwise the logged in user's
        /// data is obtained.
        /// </summary>
        /// <param name="msg"></param>
        void EntitlementCallback(Message msg)
        {
            if (msg.IsError) // User failed entitlement check
            {
                // Implements a default behavior for an entitlement check failure -- log the failure and exit the app.
                Debug.LogError("You are NOT entitled to use this app.");
                UnityEngine.Application.Quit();
            }
            else // User passed entitlement check
            {
                // Log the succeeded entitlement check for debugging.
                Oculus.Platform.Users.GetLoggedInUser().OnComplete(GetLoggedInUserCallback);
            }
        }

        /// <summary>
        /// Callback called upon obtaining the logged in user data.
        /// </summary>
        /// <param name="message"></param>
        private void GetLoggedInUserCallback(Message<User> message)
        {
            if (!message.IsError)
            {
                LocalPlayerId = message.Data.ID;
                LocalPlayerDisplayName = message.Data.DisplayName;
#if UNITY_EDITOR
                LocalPlayerDisplayName = "Editor Test";
#endif
            }
            else
            {
                Debug.Log($"Oculus Avatar Error: {message.GetError()}");
            }

            // Set the "IsReady" flag.
            IsReady = true;
        }

    }
}

