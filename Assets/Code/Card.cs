using UnityEngine;

public class Card
{
    // Each card has a value and faction which are vital for the game. A cardDisplay is added to easily find the card.
    public string faction;
    public int value;

    public CardDisplay cardDisplay;

    public Card(string faction, int value)
    {
        this.faction = faction;
        this.value = value;
    }

    public void setCardDisplay(CardDisplay cardDisplay)
    {
        this.cardDisplay = cardDisplay;
    }

    public void DestroyCard()
    {
        cardDisplay.DestroyCard();
    }

    public override string ToString()
    {
        return $"{faction} : {value}";
    }
}
