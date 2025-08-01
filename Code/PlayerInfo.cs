using UnityEngine;
using System.Collections.Generic;
using Alteruna;
using System.Collections;

public class PlayerInfo
{
    public Card cardPlayed;
    public User user;
    public List<Card> currentHand;
    public List<Card> victoryPile;
    public List<Card> armyPile;
    public List<string> winningSets = new List<string>();

    private CardSpawner cardSpawner;
    private GameManager gameManager;


    public PlayerInfo(User newUser)
    {
        cardPlayed = null;
        user = newUser;
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        currentHand = new List<Card>();
        victoryPile = new List<Card>();
        armyPile = new List<Card>();
        winningSets = new List<string>();
    }

    public void SetupForGame()
    {
        if (gameManager == null) gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        cardSpawner = gameManager.cardSpawner;
    }

    public void PlayCard()
    {
        // Card cardInfo = cardPlayed.GetComponent<CardDisplay>().card;
        // foreach (Card card in currentHand)
        // {
        //     if (card.faction == cardInfo.faction && card.value == cardInfo.value)
        //     {
        //         currentHand.Remove(card);
        //         break;
        //     }
        // }
        currentHand.Remove(cardPlayed);
        // return cardInfo;
    }

    public void SetupSecondPhase()
    {
        currentHand = new List<Card>();
        foreach (Card card in armyPile)
        {
            gameManager.cardSpawner.spawnCardInHand(card, user.Index);
            addToHand(card);
            card.DestroyCard();
            // armyPile.Remove(card);
        }
        armyPile = null;
    }
    public void addToHand(Card cardInfo)
    {
        currentHand.Add(cardInfo);
    }

    private void sortCards()
    {
        List<Card> tempCardHand = new List<Card>();
        List<string> factionNames = new List<string>();

        foreach (Card card in currentHand)
        {
            if (!factionNames.Contains(card.faction))
            {
                factionNames.Add(card.faction);
            }
            factionNames.Sort();
        }
        foreach (string factionName in factionNames)
        {
            List<int> tempFactionScores = new List<int>();
            foreach (Card card in currentHand)
            {
                if (card.faction == factionName)
                {
                    tempFactionScores.Add(card.value);
                }
            }
            tempFactionScores.Sort();
            foreach (int tempFactionScore in tempFactionScores)
            {
                tempCardHand.Add(new Card(factionName, tempFactionScore));
            }
        }
        currentHand = tempCardHand;
    }


}
