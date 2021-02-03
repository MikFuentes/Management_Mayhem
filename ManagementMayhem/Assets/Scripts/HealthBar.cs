using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    private Transform bar;

    // Start is called before the first frame update
    private void Start()
    {
        bar = transform.Find("Bar");
    }

    public void SetSize(float size_normalized)
    {
        bar.localScale = new Vector3(size_normalized, 1f);
    }

    public void SetColor(Color color)
    {
        bar.transform.Find("Bar_Sprite").GetComponent<Image>().color = color;
    }

}
