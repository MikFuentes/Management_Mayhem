using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phone_Script : MonoBehaviour
{
    AudioSource audio;
    Coroutine ringing;
    public bool beingUsedBySomeoneElse = false;
    // Start is called before the first frame update
    void Start()
    {
        audio = gameObject.GetComponent<AudioSource>();
        ringing = StartCoroutine(ring());
    }

    private void Update()
    {
        //if (beingUsedBySomeoneElse)
        //{
        //    gameObject.GetComponent<CapsuleCollider2D>().enabled = false;
        //}
        //else
        //{
        //    gameObject.GetComponent<CapsuleCollider2D>().enabled = true;
        //}
    }

    private IEnumerator ring()
    {
        gameObject.GetComponent<CapsuleCollider2D>().enabled = false;
        audio.Stop();
        yield return new WaitForSeconds(45);
        audio.Play();
        gameObject.GetComponent<CapsuleCollider2D>().enabled = true;
        yield return new WaitForSeconds(15);
        ringing = StartCoroutine(ring());
    }

    public void answeringPhone()
    {
        Debug.Log("Answeringphone()");
        if (ringing != null)
        {
            StopCoroutine(ringing);
            audio.Stop();
        }

        //beingUsedBySomeoneElse = true;
        // called when the phone is answered

        //gameObject.GetComponent<CapsuleCollider2D>().enabled = false; //so that no other people can use the phone
    }

    public void doneAnsweringPhone()
    {
        ringing = StartCoroutine(ring());
    }
}
