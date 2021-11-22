using TMPro;
using UnityEngine;


/// <summary>
/// This component is attached to the prototype list item. It provides functionality to handle
/// item click and setting the item's text.
/// </summary>
public class PhotonOVRAvatarMultiplayerGameSelectListItem : MonoBehaviour
{
    #region Private Variables


    /// <summary>
    /// The room name text.
    /// </summary>
    TextMeshProUGUI roomNameText;


    #endregion



    #region MonoBehaviour Methods


    /// <summary>
    /// MonoBehaviour method; stores a reference to the room name text.
    /// </summary>
    void Awake()
    {
        roomNameText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
    }


    #endregion



    #region Public Methods


    /// <summary>
    /// Called by PhotonOVRAvatarMultiplayerGameSelect when creating list items.
    /// </summary>
    /// <param name="name">The text to display for the list item.</param>
    public void SetName(string name)
    {
        // It is possible this method is called before Awake is called.
        // Ensure roomNameText is not null.
        // BLEE Note: It may be worth it to check if this is true.
        if (null == roomNameText)
        {
            roomNameText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        }

        // Set the text.
        roomNameText.text = name;
    }





    /// <summary>
    /// Called by the region list item OnClick.
    /// 
    /// This method passes the event to PhotonOVRAvatarMultiplayerGameSelect.
    /// </summary>
    public void OnClicked()
    {
        PhotonOVRAvatarMultiplayerGameSelect.Instance.OnRoomSelected(roomNameText.text);
    }


    #endregion

}
