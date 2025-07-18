using UnityEngine;

public class Card
{
    // Add cardDisplay here for easier sorting and moving around cards
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
