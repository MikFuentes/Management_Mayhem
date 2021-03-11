using System.Collections;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public static TextWriterSingle addWriterStatic(TMP_Text text, string textToWrite, float timePerChar, bool invisibleChar, bool removeWriterBeforeAdd, Action onComplete)
    {
        if (removeWriterBeforeAdd)
        {
            instance.removeWriter(text);
        }
        return instance.addWriter(text, textToWrite, timePerChar, invisibleChar, onComplete);
    }

    public TextWriterSingle addWriter(TMP_Text text, string textToWrite, float timePerChar, bool invisibleChar, Action onComplete)
    {
        TextWriterSingle textWriterSingle = new TextWriterSingle(text, textToWrite, timePerChar, invisibleChar, onComplete);
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
        private float timer;
        private Action onComplete;

        public TextWriterSingle(TMP_Text text, string textToWrite, float timePerChar, bool invisibleChar, Action onComplete)
        {
            this.text = text;
            this.textToWrite = textToWrite;
            this.timePerChar = timePerChar;
            this.invisibleChar = invisibleChar;
            this.onComplete = onComplete;
            characterIndex = 0;
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
            characterIndex = textToWrite.Length;
            if (onComplete != null) onComplete();
            Text_Writer.removeWriterStatic(text);
        }
    }
}
