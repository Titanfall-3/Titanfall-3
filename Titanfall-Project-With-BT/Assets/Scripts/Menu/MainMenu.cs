using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    
    public TMP_InputField inputUserName;
    public TMP_InputField inputPassword;
    public Toggle Toggle;
    public Button Button;

    public void Awake()
    {
        if (PlayerPrefs.GetInt("remember") == 1)
        {
            if (PlayerPrefs.HasKey("username"))
            {
                inputUserName.text = PlayerPrefs.GetString("username");
            }

            if (PlayerPrefs.HasKey("password"))
            {
                inputPassword.text = PlayerPrefs.GetString("password");
            }

            Toggle.isOn = true;
        }

        if (!PlayerPrefs.HasKey("remember") || PlayerPrefs.GetInt("remember") == 0)
        {
            Toggle.isOn = true;
        }

    }

    public void saveLoginInfo()
    {
        PlayerPrefs.SetString("username", inputUserName.text);
        PlayerPrefs.SetString("password", inputPassword.text);
        PlayerPrefs.SetInt("remember", Toggle.isOn ? 1 : 0);
        PlayerPrefs.Save();

        inputUserName.interactable = false;
        inputPassword.interactable = false;
        Toggle.interactable = false;
        Button.interactable = false;
        Button.GetComponentInChildren<TMP_Text>().text = "Loading...";
        
        SceneManager.LoadSceneAsync(1);
    }
    
}