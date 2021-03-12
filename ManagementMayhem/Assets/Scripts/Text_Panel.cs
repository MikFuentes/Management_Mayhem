using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text.RegularExpressions;


// https://youtu.be/ZVh4nH8Mayg
public class Text_Panel : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private GameObject image;
    [SerializeField] private GameObject nextArrow;
    [SerializeField] private GameObject namePanel;
    private Text_Writer.TextWriterSingle textWriterSingle;
    private int index = 0;
    private List<string> messageArray = new List<string>();
    private string fileName = "EMM_Stage_Zero_Script.docx.txt";
    public string message;
    public bool showButtons;

    Queue<List<int>> queue = new Queue<List<int>>();

    private void Awake()
    {
        nextButton.onClick.AddListener(clickFunct);
    }
    void Start()
    {
        var sr = new StreamReader(Application.dataPath + "/" + fileName);
        var fileContents = sr.ReadToEnd();
        sr.Close();

        messageArray.Add("Hello, " + PlayerNameInput.DisplayName + "!");
        var lines = fileContents.Split("\n"[0]);

        foreach (var line in lines)
        {
            string index = line.Split(new[] { '.' }, 2)[0];
            string s = line.Split(new[] { '.' }, 2)[1].Split(new[] {' '}, 2)[1];

            if (line.Contains("["))
            {
                int key = System.Int32.Parse(line.Split(new[] { '[' })[1].Split(',')[0]);
                int val = System.Int32.Parse(line.Split(new[] { '[' })[1].Split(',')[1].Split(']')[0]);
                s = s.Split('[')[0];

                List<int> l = new List<int>() { key, val };
                queue.Enqueue(l);
            }
            messageArray.Add(s);
        }
        clickFunct();
    }

    private void clickFunct()
    {
        if (textWriterSingle != null && textWriterSingle.IsActive())
        {
            // Will finish the current line
            textWriterSingle.WriteAllAndDestroy();

            // Dialogue option
            if (queue.Count != 0 && index == queue.Peek()[0] + 1)
            {
                nextButton.onClick.RemoveListener(clickFunct);
            }
        }
        else{
            // Will go to the next line
            if (index == messageArray.Count)
            {
                nextButton.onClick.RemoveListener(clickFunct);
                nextButton.onClick.AddListener(clickFunct2); //add diff listener with diff function
                return;
            }
            else
            {
                message = messageArray[index];

                startTalking();
                textWriterSingle = Text_Writer.addWriterStatic(messageText, message, 0.03f, true, true, true, stopTalking);
            }

            // Dialogue option
            // check the top of the queue
            // if it matches the index
            // pop
            if (queue.Count != 0 && index == queue.Peek()[0])
            {
                image.gameObject.SetActive(false);
                showButtons = true;
            }

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
        if (index != messageArray.Count-1)
        {
            if (showButtons)
            {
                yesButton.gameObject.SetActive(true);
                noButton.gameObject.SetActive(true);
            }
            else
            {
                nextArrow.SetActive(true);
            }
        }
        if(index == messageArray.Count-1)
        {
            //activate different arrow
            //nextArrow.SetActive(true);
        }
        index++;
    }
}
