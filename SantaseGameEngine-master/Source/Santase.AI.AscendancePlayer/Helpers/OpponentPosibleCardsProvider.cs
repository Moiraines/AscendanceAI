using System.Collections.Generic;
using System.Linq;

using Santase.Logic.Cards;

namespace Santase.AI.AscendancePlayer.Helpers
{
	public class OpponentPosibleCardsProvider
	{
		public static readonly HashSet<Card> AllCards;

		static OpponentPosibleCardsProvider()
		{
			AllCards = new HashSet<Card>();

			foreach (var suit in new CardSuit[4] { CardSuit.Club, CardSuit.Diamond, CardSuit.Heart, CardSuit.Spade })
				foreach (var type in new CardType[6] { CardType.Nine, CardType.Jack, CardType.Queen, CardType.King, CardType.Ten, CardType.Ace})
					AllCards.Add(new Card(suit, type));
		}

		public ICollection<Card> GetOpponentCards(ICollection<Card> myCards, ICollection<Card> playedCards, Card activeTrumpCard)
        {
            return AllCards.Where(c => !myCards.Any(m => c.Equals(m)))
				.Where(c => !playedCards.Any(p => c.Equals(p)))
				.Where(c => activeTrumpCard == null ? true : !c.Equals(activeTrumpCard)).ToList();
        }
	}
}
