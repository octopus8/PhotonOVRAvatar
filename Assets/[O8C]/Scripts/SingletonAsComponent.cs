using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace O8C
{



    /// <summary>
    /// Parent class for Singleton classes that need MonoBehaviour support.
    /// </summary>
    /// <remarks>
    /// To use this class, the specific singleton class needs to inherit from this class
    /// like `class XXX : SingletonAsComponent<XXX>`. The `Instance` accessor needs to be
    /// defined like the following:
    /// `   public static XXX Instance
    ///     {
    ///         get
    ///         {
    ///             XXX retval = ((XXX)_Instance);
    ///             return retval;
    ///         }
    /// 
    ///         set
    ///         {
    ///             _Instance = value;
    ///         }
    ///     }
    /// `
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class SingletonAsComponent<T> : MonoBehaviour where T : SingletonAsComponent<T>
    {

        #region PRIVATE MEMBERS


        /// <summary>
        /// The instance of the Singleton object.
        /// </summary>
        protected static T __Instance;

        /// <summary>
        /// Flag indicating whether or not the script is alive.
        /// </summary>
        private bool _alive = true;


        #endregion





        /// <summary>
        /// Access to the instance. 
        /// </summary>
        protected static SingletonAsComponent<T> _Instance
        {
            get
            {
                if (!__Instance)
                {
                    T[] managers = GameObject.FindObjectsOfType(typeof(T)) as T[];
                    if (managers != null)
                    {
                        if (managers.Length == 1)
                        {
                            __Instance = managers[0];
                            return __Instance;
                        }
                        else if (managers.Length > 1)
                        {
                            Debug.LogError("You have more than one " + typeof(T).Name + " in the scene. You only need 1, it's a singleton!");
                            for (int i = 0; i < managers.Length; ++i)
                            {
                                T manager = managers[i];
                                Destroy(manager.gameObject);
                            }
                        }
                    }

                    GameObject go;

                    // This is taken out. It can be used for obtaining the Singleton from the Resources.
                    if (false)
                    {
                        Object o = Resources.Load(typeof(T).Name, typeof(GameObject));
                        if (null != o)
                        {
                            go = Instantiate(o) as GameObject;
                        }
                        else
                        {
                            go = new GameObject(typeof(T).Name, typeof(T));
                        }
                    }
                    else
                    {
                        go = new GameObject(typeof(T).Name, typeof(T));
                    }
                    __Instance = go.GetComponent<T>();
                    //                DontDestroyOnLoad(__Instance.gameObject);
                }
                return __Instance;
            }

            set
            {
                __Instance = value as T;
            }
        }





        /// <summary>
        /// Returns the alive status.
        /// </summary>
        public static bool IsAlive
        {
            get
            {
                if (__Instance == null)
                    return false;
                return __Instance._alive;
            }
        }





        /// <summary>
        /// Destroys the game object along with the script.
        /// </summary>
        public void Destroy()
        {
            Destroy(gameObject);
        }





        /// <summary>
        /// Sets the alive flag as false.
        /// </summary>
        void OnDestroy()
        {
            _alive = false;
        }





        /// <summary>
        /// Sets the alive flag a false.
        /// </summary>
        void OnApplicationQuit()
        {
            _alive = false;
        }


    }

}