using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// https://youtu.be/ZVh4nH8Mayg
public class Text_Panel : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject nextArrow;
    private Text_Writer.TextWriterSingle textWriterSingle;
    private int index = 0;
    private string[] messageArray;

    private void Awake()
    {
        nextButton.onClick.AddListener(clickFunct);
    }
    void Start()
    {
        messageArray = new string[]
        {
            "Hi Core Team! Welcome to Event Management Mayhem. You guys are tasked to keep this Gato Game Summit running smoothly.",
            "Decide who will be the Finance, Programs, and Logistics person in your group of three.",
            "There are three stations that correspond to each of your roles. It is highly recommended for each one to stay in their respective stations. Don’t forget to communicate!"
        };
        clickFunct();
    }

    private void clickFunct()
    {
        if (textWriterSingle != null && textWriterSingle.IsActive()){
            //currently active textwriter
            textWriterSingle.WriteAllAndDestroy();
        }
        else
        {   
            if (index == messageArray.Length)
            {
                nextButton.onClick.RemoveListener(clickFunct);
                nextButton.onClick.AddListener(clickFunct2); //add diff listener with diff function
                return;
            }
            string message = messageArray[index];

            startTalking();
            textWriterSingle = Text_Writer.addWriterStatic(messageText, message, 0.03f, true, true, stopTalking);
        }
    }
    private void clickFunct2()
    {
        Debug.Log("hi");
    }

    private void startTalking()
    {
        nextArrow.SetActive(false);
    }

    private void stopTalking()
    {
        if(index != messageArray.Length-1)
        {
            nextArrow.SetActive(true);
        }
        if(index == messageArray.Length-1)
        {
            //activate different arrow
            //nextArrow.SetActive(true);
        }
        index++;
    }
}
