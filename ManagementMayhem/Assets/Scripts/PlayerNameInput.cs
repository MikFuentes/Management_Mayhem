using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNameInput : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInputField = null;
    [SerializeField] private Button continueButton = null;
    [SerializeField] private TMP_Text helloText = null;

    public static string DisplayName { get; private set; } //for grabbing the display name in the UI

    private const string PlayerPrefsNameKey = "PlayerName";

    void Start() => SetUpInputField();

    public void SetUpInputField()
    {   
        // If no name, do nothing
        if (!PlayerPrefs.HasKey(PlayerPrefsNameKey)) {
            nameInputField.text = "";
            return; 
        } 

        string defaultName = PlayerPrefs.GetString(PlayerPrefsNameKey, "");

        nameInputField.text = defaultName;

        SetPlayerName(defaultName);
    }

    public void SetPlayerName(string name)
    {
        continueButton.interactable = !string.IsNullOrEmpty(name);

        if (string.IsNullOrEmpty(name))
        {
            continueButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0.5f);
        }
        else
        {
            continueButton.transform.Find("Text").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 1);
        }
    }

    public void SavePlayerName()
    {
        DisplayName = nameInputField.text;

        PlayerPrefs.SetString(PlayerPrefsNameKey, DisplayName);

        helloText.text = "Hello " + DisplayName + "!";
    }

    public void ClearPlayerName()
    {
        nameInputField.text = null;
        SetPlayerName(null);
    }

}
