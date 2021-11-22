using O8C;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkingPhotonPlayer : MonoBehaviour
{
    void Start()
    {
        NetworkingPhoton.Instance.OnNewNetworkedUser(gameObject);
    }

    private void OnDestroy()
    {
        if (null != NetworkingPhoton.Instance)
        {
            NetworkingPhoton.Instance.OnDestroyNetworkedUser(gameObject);
        }
    }

}
