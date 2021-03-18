using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;

// https://youtu.be/ZVh4nH8Mayg
public class Text_Writer : MonoBehaviour
{
    private static Text_Writer instance;
    private List<TextWriterSingle> textWriterSingleList;

    private void Awake()
    {
        instance = this;
        textWriterSingleList = new List<TextWriterSingle>();
    }

    public static TextWriterSingle addWriterStatic(TMP_Text text, string textToWrite, float timePerChar, bool invisibleChar, bool hasHighlights, bool removeWriterBeforeAdd, Action onComplete)
    {
        if (removeWriterBeforeAdd)
        {
            instance.removeWriter(text);
        }
        return instance.addWriter(text, textToWrite, timePerChar, invisibleChar, hasHighlights, onComplete);
    }

    public TextWriterSingle addWriter(TMP_Text text, string textToWrite, float timePerChar, bool invisibleChar, bool hasHighlights, Action onComplete)
    {
        TextWriterSingle textWriterSingle = new TextWriterSingle(text, textToWrite, timePerChar, invisibleChar, hasHighlights, onComplete);
        textWriterSingleList.Add(textWriterSingle);
        return textWriterSingle;
    }
    public static void removeWriterStatic(TMP_Text text)
    {
        instance.removeWriter(text);
    }
    public void removeWriter(TMP_Text text)
    {
        for (int i = 0; i < textWriterSingleList.Count; i++)
        {
            if (textWriterSingleList[i].GetUIText() == text)
            {
                textWriterSingleList.RemoveAt(i);
                i--;
            }
        }
    }

    private void Update()
    {
        for(int i = 0; i < textWriterSingleList.Count; i++)
        {
            bool destroyInstance = textWriterSingleList[i].Update();
            if (destroyInstance)
            {
                textWriterSingleList.RemoveAt(i);
                i--;
            }
        }
    }


    // Represents a single textWriter instance
    public class TextWriterSingle
    {
        private TMP_Text text;
        private string textToWrite;
        private float timePerChar;

        private int characterIndex;
        private bool invisibleChar;
        private bool hasHighlights;
        private float timer;
        private Action onComplete;

        //private List<string> keyWords = new List<string>();
        //private string fileName = "EMM_Stage_Zero_Keywords.txt";

        public TextWriterSingle(TMP_Text text, string textToWrite, float timePerChar, bool invisibleChar, bool hasHighlights, Action onComplete)
        {
            this.text = text;
            this.textToWrite = textToWrite;
            this.timePerChar = timePerChar;
            this.invisibleChar = invisibleChar;
            this.hasHighlights = hasHighlights;
            this.onComplete = onComplete;
            characterIndex = 0;

            //var sr = new System.IO.StreamReader(Application.dataPath + "/" + fileName);
            //var fileContents = sr.ReadToEnd();
            //sr.Close();

            //var lines = fileContents.Split("\n"[0]);

            //foreach (var line in lines)
            //{
            //    keyWords.Add(line);
            //}
        }

        // returns true when complete
        public bool Update()
        {
            timer -= Time.deltaTime;
            while (timer <= 0f)
            {
                //Display next char
                timer += timePerChar;
                characterIndex++;
                string temp = textToWrite.Substring(0, characterIndex);

                if (hasHighlights)
                {
                    temp = HighlightKeyWords(temp);
                }
                if (invisibleChar)
                {
                    temp += "<color=#00000000>" + textToWrite.Substring(characterIndex) + "</color>";
                }

                text.text = temp;

                // When it reaches the end of the line
                if (characterIndex >= textToWrite.Length)
                {
                    // Entire string displayed
                    if (onComplete != null) onComplete();
                    return true;
                }
            }
            return false;
        }

        public TMP_Text GetUIText()
        {
            return text;
        }

        public bool IsActive()
        {
            return characterIndex < textToWrite.Length;
        }

        public void WriteAllAndDestroy()
        {
            text.text = textToWrite;
            if(hasHighlights) text.text = HighlightKeyWords(textToWrite);

            characterIndex = textToWrite.Length;

            if (onComplete != null) onComplete();
            Text_Writer.removeWriterStatic(text);
        }

        public void destroy()
        {
            text.text = "";
            Text_Writer.removeWriterStatic(text);
        }

        public string HighlightKeyWords(string s)
        {
            List<string> keyWords = new List<string>()
            {
                "ATM",
                "Bank",
                "Event Management Mayhem",
                "Finance",
                "Gato Game Summit",
                "GGS",
                "Logistics",
                "Main Menu",
                "NPC",
                "Phone",
                "Rules",
                "Programs",
                "Stage Area",
                "Warehouse",
                "coins",
                "communicate",
                "conveyor belts",
                "goal",
                "items",
                "latest technologies",
                "roles",
                "sponsorship funds",
                "stations"
            };

            foreach (string keyWord in keyWords)
            {
                Match match = Regex.Match(s, keyWord);
                string color = "white";

                if (keyWord == "ATM" || 
                    keyWord == "Phone") { color = "red"; }
                else if (
                        keyWord == "Bank" || 
                        keyWord == "Finance") { color = "green";  }
                else if (
                        keyWord == "Event Management Mayhem" ||
                        keyWord == "Gato Game Summit" ||
                        keyWord == "GGS" ||
                        keyWord == "Rules" ||
                        keyWord == "Main Menu" ||
                        keyWord == "communicate" ||
                        keyWord == "latest technologies" ||
                        keyWord == "roles" ||
                        keyWord == "stations") { color = "#FF00F1"; }
                else if (
                        keyWord == "Logistics" ||
                        keyWord == "Warehouse"){ color = "#00EFFF"; }
                else if (
                        keyWord == "Programs" ||
                        keyWord == "Program" ||
                        keyWord == "Stage Area") { color = "orange"; }
                else if (
                        keyWord == "coins" ||
                        keyWord == "conveyor belts" ||
                        keyWord == "sponsorship funds" ||
                        keyWord == "NPC" ||
                        keyWord == "items") { color = "yellow"; }

                    System.Text.StringBuilder builder = new System.Text.StringBuilder(s);
                    builder.Replace(keyWord, "<color=" + color + ">" + keyWord + "</color>");
                    s = builder.ToString();
            }

            return s;
        }
    }
}
