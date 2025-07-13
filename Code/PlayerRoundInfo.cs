using UnityEngine;

public class PlayerRoundInfo
{
    public PlayerInfo user;
    public Card playedCard;
    
    public PlayerRoundInfo(PlayerInfo user, Card playedCard)
    {
        this.user = user;
        this.playedCard = playedCard;
    }
}