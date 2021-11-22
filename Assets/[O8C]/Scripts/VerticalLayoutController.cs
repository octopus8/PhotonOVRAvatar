using UnityEngine;


/// <summary>
/// Vertical scroll view helper component.
/// This component assumes the content contains a single item. This single
/// item is used as the prototype for all items added to the list.
/// </summary>
public class VerticalLayoutController : MonoBehaviour
{

    #region Private Variables

    /// <summary>
    /// Vertical scroll view content.
    /// </summary>
    RectTransform contentRectTransform;

    /// <summary>
    /// The prototype list item.
    /// </summary>
    GameObject prototypeItem;

    #endregion





    #region MonoBehaviour Callbacks

    private void Awake()
    {
        prototypeItem = transform.GetChild(0).gameObject;
        prototypeItem.SetActive(false);
        contentRectTransform = GetComponent<RectTransform>();
    }

    #endregion





    #region Public Methods

    /// <summary>
    /// Called to add an item to the list.
    /// </summary>
    /// <returns>The new item added to the list.</returns>
    public GameObject AddItem()
    {
        // Create the item.
        GameObject retval = Instantiate(prototypeItem, transform);

        // Recompute the container size.
        RectTransform itemRectTransform = retval.GetComponent<RectTransform>();
        Vector2 sizeDelta = contentRectTransform.sizeDelta;
        sizeDelta.y = (transform.childCount-1) * itemRectTransform.sizeDelta.y;
        contentRectTransform.sizeDelta = sizeDelta;

        // Set the item active.
        retval.SetActive(true);

        // Return the item.
        return retval;
    }





    /// <summary>
    /// Called to clear the list.
    /// </summary>
    public void Clear()
    {
        // Destroy all but the first item.
        while (transform.childCount > 1)
        {
            Destroy(transform.GetChild(1));
        }
    }





    /// <summary>
    /// Returns the number of items in the list.
    /// </summary>
    /// <returns>Item count.</returns>
    public int ItemCount()
    {
        return transform.childCount - 1;
    }

    #endregion

}
