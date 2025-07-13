using UnityEngine;
using Alteruna;
using System;   

public class CardSpawner : AttributesSync
{
    // All card spawning is handled here
    public GameObject cardPrefab;
    private GameManager gameManager;


    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    public void spawnCardInMiddle(Card cardInfo, ushort Id)
    {
        string tag;
        if (Id == 0) tag = "cardPlayingFor";
        else tag = "playedCard" + (Id - 1).ToString();
        spawnCardAtLocation(cardInfo, new Vector3(-6.817f + 2.5f * Id, 2.5f, 0), new Vector3(2, 3, 1), tag);
    }

    public void spawnCardInHand(Card cardInfo, ushort Id)
    {
        int handSize = gameManager.localPlayer.currentHand.Count;
        gameManager.localPlayer.currentHand.Add(spawnCardAtLocation(cardInfo, new Vector3(-6.7f + 0.8f * handSize, -2.5f, 0.0f - handSize * 0.3f), Vector3.zero, "Card"));
    }

    public void spawnCardInBattleHand(Card cardInfo, ushort Id)
    {
        int battleHandSize = gameManager.localPlayer.armyPile.Count;
        gameManager.localPlayer.armyPile.Add(spawnCardAtLocation(cardInfo, new Vector3(6f + 0.4f * battleHandSize - 2.8f * (battleHandSize / 7), -1.4f - 2.5f * (battleHandSize / 7), 0.0f - battleHandSize * 0.3f), new Vector3(1.5f, 2, 1), "NO"));
    }

    public void spawnCardInVictoryHand(Card cardInfo, ushort Id)
    {
        int victoryHandSize = gameManager.localPlayer.victoryPile.Count;
        gameManager.localPlayer.victoryPile.Add(spawnCardAtLocation(cardInfo, new Vector3(6f + 0.4f * victoryHandSize - 2.8f * (victoryHandSize / 7), 3.8f - 2.5f * (victoryHandSize / 7), 0.0f - victoryHandSize * 0.3f), new Vector3(1.5f, 2, 1), "NO"));
    }
    public Card spawnCardAtLocation(Card cardInfo, Vector3 location, Vector3 size, string newTag)
    {
        GameObject newCard = Instantiate(cardPrefab, location, Quaternion.identity);
        CardDisplay cardDisplay = newCard.GetComponent<CardDisplay>();
        newCard.GetComponent<CardDisplay>().initialize(cardInfo);
        cardInfo.setCardDisplay(cardDisplay);
        newCard.transform.SetParent(GameObject.Find("CardSpawner").transform);
        if (size != Vector3.zero) newCard.GetComponent<Transform>().localScale = size;
        if (newTag != "NO") newCard.tag = newTag;
        return newCard.GetComponent<CardDisplay>().card;
    }
}
