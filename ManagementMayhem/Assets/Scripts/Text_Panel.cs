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

    [SerializeField] private GameObject stageZero;
    [SerializeField] private GameObject landingPage;
    [SerializeField] private GameObject rulesPage;

    public int index = 0;
    private List<string> messageArray = new List<string>();
    private string fileName = "EMM_Stage_Zero_Script.txt";
    public string message;
    public bool showButtons;

    Queue<List<int>> queue = new Queue<List<int>>();

    private void Awake()
    {
        nextButton.onClick.AddListener(clickFunct);
        noButton.onClick.AddListener(nextText);
    }
    public void Start()
    {

    }

    private void OnEnable()
    {
        index = 0;
        messageArray = new List<string>();
        queue = new Queue<List<int>>();

        nextArrow.SetActive(false);
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(clickFunct);

        if (textWriterSingle != null && textWriterSingle.IsActive())
        {
            Debug.Log("writer is active");
            clickFunct(); //write all the text
            if (PlayerPrefs.HasKey("PlayerName"))
                messageArray.Add("Hello, " + PlayerNameInput.DisplayName + "!");
        }

        foreach (Transform child in image.transform)
        {
            child.gameObject.SetActive(false);
        }
        image.gameObject.SetActive(true);

        var sr = new StreamReader(Application.dataPath + "/" + fileName);
        var fileContents = sr.ReadToEnd();
        sr.Close();

        if (PlayerPrefs.HasKey("PlayerName"))
            messageArray.Add("Hello, " + PlayerNameInput.DisplayName + "!");
        var lines = fileContents.Split("\n"[0]);

        foreach (var line in lines)
        {
            string index = line.Split(new[] { '.' }, 2)[0];
            string s = line.Split(new[] { '.' }, 2)[1].Split(new[] { ' ' }, 2)[1];

            if (line.Contains("["))
            {
                int key = System.Int32.Parse(line.Split(new[] { '[' })[1].Split(',')[0]);
                int val = System.Int32.Parse(line.Split(new[] { '[' })[1].Split(',')[1].Split(']')[0]);
                s = s.Split('[')[0];

                if (!PlayerPrefs.HasKey("PlayerName"))
                    key--; val--;

                List<int> l = new List<int>() { key, val };
                queue.Enqueue(l);
            }

            messageArray.Add(s);
        }

        Debug.Log("writer is NOT active");
        clickFunct();

        //clickFunct();

        Debug.Log("Enabled");
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
                Debug.Log("bro");
                nextButton.onClick.RemoveListener(clickFunct);
                nextButton.onClick.AddListener(clickFunct2); //add diff listener with diff function
                clickFunct2();
                return;
            }
            else
            {
                string imgName = "";
                message = messageArray[index];
                if (message.Contains("{"))
                {                    
                    imgName = message.Split('{')[1].Split('}')[0];

                    message = message.Split('{')[0];

                }

                startTalking();
                if(imgName != "")
                {
                    foreach (Transform child in image.transform)
                    {
                        child.gameObject.SetActive(false);
                    }
                    image.transform.Find(imgName).gameObject.SetActive(true);
                }

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

                yesButton.onClick.RemoveAllListeners();
                yesButton.onClick.AddListener(delegate {repeatText(queue.Peek()[1]);});
            }
        }            
    }

    private void repeatText(int stringIndex)
    {
        index = stringIndex;
        nextButton.onClick.AddListener(clickFunct);
        clickFunct();

        foreach (Transform child in image.transform)
        {
            child.gameObject.SetActive(false);
        }
        image.gameObject.SetActive(true);

        showButtons = false;
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
    }

    private void nextText()
    {
        queue.Dequeue();
        index++;
        nextButton.onClick.AddListener(clickFunct);
        clickFunct();

        foreach (Transform child in image.transform)
        {
            child.gameObject.SetActive(false);
        }
        image.gameObject.SetActive(true);

        showButtons = false;
        yesButton.gameObject.SetActive(false);
        noButton.gameObject.SetActive(false);
    }

    private void clickFunct2()
    {
        if (PlayerPrefs.HasKey("PlayerName"))
        {
            stageZero.SetActive(false);
            landingPage.SetActive(true);
        }
        else
        {
            stageZero.SetActive(false);
            rulesPage.SetActive(true);
        }


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
            nextArrow.SetActive(true);
        }
        index++;
    }
}
