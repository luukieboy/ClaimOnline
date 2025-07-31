using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using Alteruna;
using System;

public class CardsCheckBox : AttributesSync
{
    // This code is on the checkboxes in the starting screen and is made to ensure synchronisation accross clients
    // of selected decks
    private Settings settingsScript;
    private Toggle m_Toggle;
    private bool updatingToggle = false;

    void Start()
    {
        settingsScript = GameObject.Find("Settings").GetComponent<Settings>();
    }

    public void boolChanged()
    {
        // Called when a tickbox is clicked
        settingsScript.UpdateToggle(Int32.Parse(gameObject.name), gameObject.GetComponent<Toggle>().isOn);
        if (settingsScript.amITheHost) ChangeTheBool();
    }

    [SynchronizableMethod]
    public void ChangeTheBool()
    {
        string text = gameObject.GetComponentInChildren<Text>().text;
        if (text.Contains("&"))
        {
            foreach (string cardName in text.Split("&"))
            {
                settingsScript.changeCardsInDeck(cardName.Trim());
            }
        }
        else settingsScript.changeCardsInDeck(text.Trim());
    }
}
