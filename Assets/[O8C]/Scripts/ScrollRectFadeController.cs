using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the top and bottom fades in a vertical scroll view.
/// </summary>
public class ScrollRectFadeController : MonoBehaviour
{
    #region Editor Parameters

    /// <summary>
    /// Top fade image.
    /// </summary>
    [SerializeField]
    [Tooltip("Top fade image.")]
    Image topFade;

    /// <summary>
    /// Bottom fade image.
    /// </summary>
    [SerializeField]
    [Tooltip("Bottom fade image.")]
    Image bottomFade;

    /// <summary>
    /// The vertical scrollbar for the scroll view.
    /// This is used to help control display of the bottom fade.
    /// </summary>
    [SerializeField]
    [Tooltip("Vertical scrollbar; used to help control display of the bottom fade.")]
    GameObject verticalScrollbar;

    #endregion





    #region Private Variables

    /// <summary>
    /// Top fade state.
    /// </summary>
    FadeState topFadeState = FadeState.fadedOut;

    /// <summary>
    /// Bottom fade state.
    /// </summary>
    FadeState bottomFadeState = FadeState.fadedOut;

    /// <summary>
    /// Top fade coroutine.
    /// </summary>
    Coroutine topFadeCorot;

    /// <summary>
    /// Bottom fade coroutine.
    /// </summary>
    Coroutine bottomFadeCorot;

    /// <summary>
    /// Fade speed; fraction of a second.
    /// </summary>
    const float fadeSpeed = 10f;


    #endregion





    #region MonoBehaviour Callbacks

    /// <summary>
    /// MonoBehaviour callback; initializes the fade images.
    /// </summary>
    private void OnEnable()
    {
        // Initialize the fade images.
        topFade.gameObject.SetActive(false);
        Color c = topFade.color;
        c.a = 0;
        topFade.color = c;
        bottomFade.gameObject.SetActive(false);
        c = bottomFade.color;
        c.a = 0;
        bottomFade.color = c;

        // Perform OnEnable delayed tasks.
        StartCoroutine(OnEnableAsync());
    }

    #endregion





    #region Public Methods

    /// <summary>
    /// Called by the scroll rect upon scroll value changed.
    /// Updates the top and bottom fade.
    /// </summary>
    public void OnScrollValueChanged(Vector2 pos)
    {
        UpdateTopFade(pos);
        UpdateBottomFade(pos);
    }

    #endregion





    #region Private Methods

    /// <summary>
    /// Called by OnEnable to initialize the scroll fade images.
    /// If this is not done, it is possible the bottom fade shows
    /// for an empty list.
    /// </summary>
    IEnumerator OnEnableAsync()
    {
        yield return null;
        yield return null;
        ScrollRect sr = GetComponent<ScrollRect>();
        OnScrollValueChanged(sr.normalizedPosition);
    }





    /// <summary>
    /// Called upon scroll change to update the top fade.
    /// </summary>
    /// <param name="pos"></param>
    void UpdateTopFade(Vector2 pos)
    {
        // If the top fade should be displayed...
        if (pos.y < 1)
        {
            // If the top fade is not already displayed or fading in...
            if ((topFadeState != FadeState.fadedIn) && (topFadeState != FadeState.fadingIn))
            {
                // If fading out, then stop.
                if (null != topFadeCorot)
                {
                    StopCoroutine(topFadeCorot);
                }

                // Fade the top fade in.
                topFadeCorot = StartCoroutine(FadeTopFade(true));
            }
        }

        // Otherwise, the top fade should not be displayed.
        else
        {
            // If the bottom fade is not already hidden or fading out...
            if ((topFadeState != FadeState.fadedOut) && (topFadeState != FadeState.fadingOut))
            {
                // If doing a fade, stop.
                if (null != topFadeCorot)
                {
                    StopCoroutine(topFadeCorot);
                }

                // Fade the top fade out.
                topFadeCorot = StartCoroutine(FadeTopFade(false));
            }
        }
    }





    /// <summary>
    /// Called upon scroll change to update the bottom fade.
    /// </summary>
    /// <param name="pos"></param>
    void UpdateBottomFade(Vector2 pos)
    {
        // If the bottom fade should be displayed...
        if ((pos.y > 0) && (verticalScrollbar.activeInHierarchy))
        {
            // If the bottom fade is not already displayed or fading in...
            if ((bottomFadeState != FadeState.fadedIn) && (bottomFadeState != FadeState.fadingIn))
            {
                // If doing a fade, stop.
                if (null != bottomFadeCorot)
                {
                    StopCoroutine(bottomFadeCorot);
                }

                // Fade the bottom fade in.
                bottomFadeCorot = StartCoroutine(FadeBottomFade(true));
            }
        }
        else
        {
            // If the bottom fade is not already hidden or fading out...
            if ((bottomFadeState != FadeState.fadedOut) && (bottomFadeState != FadeState.fadingOut))
            {
                // If doing a fade, stop.
                if (null != bottomFadeCorot)
                {
                    StopCoroutine(bottomFadeCorot);
                }

                // Fade the bottom fade out.
                bottomFadeCorot = StartCoroutine(FadeBottomFade(false));
            }
        }
    }





    /// <summary>
    /// Coroutine to update top fade.
    /// </summary>
    IEnumerator FadeTopFade(bool isFadeIn)
    {
        float dest;
        float scale;

        // Initialize depending on fade in/out.
        if (isFadeIn)
        {
            topFade.gameObject.SetActive(true);
            topFadeState = FadeState.fadingIn;
            dest = 1;
            scale = 1;
        }
        else
        {
            topFadeState = FadeState.fadingOut;
            dest = 0;
            scale = -1;
        }

        // Update fade over frames.
        while (topFade.color.a != dest)
        {
            yield return null;
            Color c = topFade.color;
            c.a += Time.deltaTime * fadeSpeed * scale;
            c.a = Mathf.Clamp(c.a, 0, 1);
            topFade.color = c;
        }

        // Complete the fade.
        if (!isFadeIn)
        {
            topFade.gameObject.SetActive(false);
            topFadeState = FadeState.fadedOut;
        }
        else
        {
            topFadeState = FadeState.fadedIn;
        }
        topFadeCorot = null;
    }





    /// <summary>
    /// Coroutine to update bottom fade.
    /// </summary>
    IEnumerator FadeBottomFade(bool isFadeIn)
    {
        float dest;
        float scale;

        // Initialize depending on fade in/out.
        if (isFadeIn)
        {
            bottomFade.gameObject.SetActive(true);
            bottomFadeState = FadeState.fadingIn;
            dest = 1;
            scale = 1;
        }
        else
        {
            bottomFadeState = FadeState.fadingOut;
            dest = 0;
            scale = -1;
        }

        // Update fade over frames.
        while (bottomFade.color.a != dest)
        {
            yield return null;
            Color c = bottomFade.color;
            c.a += Time.deltaTime * fadeSpeed * scale;
            c.a = Mathf.Clamp(c.a, 0, 1);
            bottomFade.color = c;
        }


        // Complete the fade.
        if (!isFadeIn)
        {
            bottomFade.gameObject.SetActive(false);
            bottomFadeState = FadeState.fadedOut;
        }
        else
        {
            bottomFadeState = FadeState.fadedIn;
        }
    }

    #endregion





    #region Data Types

    /// <summary>
    /// Fade states
    /// </summary>
    enum FadeState
    {
        fadedOut,
        fadingIn,
        fadedIn,
        fadingOut,
    };

    #endregion
}
