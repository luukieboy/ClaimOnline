using UnityEngine;
using Alteruna;
using System;   

public class CardSpawner : AttributesSync
{
    public GameObject cardPrefab;
    private GameManager gameManager;
    private GameObject newCard = null;

    private int handToAddTo;
    private ushort playerId;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    void Update()
    {
        if (newCard != null)
        {
            PlayerInfo player = FindPlayer(playerId);
            if (handToAddTo == 0) player.currentHand.Add(newCard.GetComponent<CardDisplay>().card);
            else if (handToAddTo == 1) player.armyPile.Add(newCard.GetComponent<CardDisplay>().card);
            else if (handToAddTo == 2) player.victoryPile.Add(newCard.GetComponent<CardDisplay>().card);
            newCard = null;
        }

    }

    public void spawnCardInMiddle(Card cardInfo, int playerNumber)
    {
        handToAddTo = 4;
        BroadcastRemoteMethod("spawnCardAtLocation", cardInfo.faction, cardInfo.value, new Vector3(-6.817f + 2.5f * playerNumber, 2.5f, 0), new Vector3(2, 3, 1), "cardPlayingFor");
    }

    public void MoveCardToMiddle(ushort Id, GameObject cardObject)
    {
        handToAddTo = 4;
        Card cardInfo = cardObject.GetComponent<CardDisplay>().card;
        BroadcastRemoteMethod("spawnCardAtLocation", cardInfo.faction, cardInfo.value, new Vector3(-4.317f + 2.5f * Id, 2.52f, 0), new Vector3(2, 3, 1), "playedCard" + Id.ToString());
        InvokeRemoteMethod("AddPlayedCard", gameManager.settings.hostId, Id);
        Destroy(cardObject);
    }

    public void spawnCardInHand(Card cardInfo, ushort Id)
    {
        // spawnCardAtLocation(cardInfo.faction, cardInfo.value, new Vector3(-6.7f + 0.8f * (handSize - 1), -2.5f, 0 - (handSize - 1) * 0.3f), Vector3.zero, true);
        PlayerInfo player = FindPlayer(Id);
        int handSize = player.currentHand.Count;
        handToAddTo = 0;
        playerId = Id;
        InvokeRemoteMethod("spawnCardAtLocation", Id, cardInfo.faction, cardInfo.value, new Vector3(-6.7f + 0.8f * handSize, -2.5f, 0.0f - handSize * 0.3f), Vector3.zero, "Card");

    }

    public void spawnCardInBattleHand(Card cardInfo, ushort Id)
    {
        // Get battleHandSize and then spawn new card in victory hand
        PlayerInfo player = FindPlayer(Id);
        int battleHandSize = player.armyPile.Count;
        InvokeRemoteMethod("spawnCardAtLocation", Id, cardInfo.faction, cardInfo.value, new Vector3(6f + 0.4f * battleHandSize - 2.8f * (battleHandSize / 7), -1.4f - 2.5f * (battleHandSize / 7), 0.0f - battleHandSize * 0.3f), new Vector3(1.5f, 2, 1), "NO");
        handToAddTo = 1;
        playerId = Id;
    }

    public void spawnCardInVictoryHand(Card cardInfo, ushort Id)
    {
        // Get victoryHandSize and then spawn new card in victory hand
        PlayerInfo player = FindPlayer(Id);
        int victoryHandSize = player.victoryPile.Count;
        handToAddTo = 2;  
        playerId = Id;  
        InvokeRemoteMethod("spawnCardAtLocation", Id, cardInfo.faction, cardInfo.value, new Vector3(6f + 0.4f * victoryHandSize - 2.8f * (victoryHandSize / 7), 3.8f - 2.5f * (victoryHandSize / 7), 0.0f - victoryHandSize * 0.3f), new Vector3(1.5f, 2, 1), "NO");
    }

    private PlayerInfo FindPlayer(ushort Id)
    {
        foreach (PlayerInfo player in gameManager.players) if (player.user.Index == Id) return player;
        return null;
    }

    [SynchronizableMethod]
    public void spawnCardAtLocation(string cardFaction, int cardValue, Vector3 location, Vector3 size, string newTag)
    {
        Card cardInfo = new Card(cardFaction, cardValue);
        newCard = Instantiate(cardPrefab, location, Quaternion.identity);
        newCard.transform.SetParent(GameObject.Find("CardSpawner").transform);
        if (size != Vector3.zero) newCard.GetComponent<Transform>().localScale = size;
        CardDisplay cardDisplay = newCard.GetComponent<CardDisplay>();
        newCard.GetComponent<CardDisplay>().initialize(cardInfo);
        cardInfo.setCardDisplay(cardDisplay);
        if (newTag != "NO") newCard.tag = newTag;
    }
    
    [SynchronizableMethod]
    public void AddPlayedCard(ushort userId)
    {
        gameManager.players[userId].cardPlayed = gameManager.getObjectWithTag(gameObject.transform, "playedCard" + userId.ToString()).objectOfInterest;
    }
}
