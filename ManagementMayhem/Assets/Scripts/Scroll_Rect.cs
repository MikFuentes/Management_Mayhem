using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scroll_Rect : MonoBehaviour
{
    public ScrollRect myScrollRect;
    public GameObject moralePage;

    public void OnEnable()
    {
        //Change the current vertical scroll position.
        myScrollRect.verticalNormalizedPosition = 1f;
        moralePage.SetActive(true);
    }
}
