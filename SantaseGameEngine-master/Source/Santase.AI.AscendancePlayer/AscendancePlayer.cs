namespace Santase.AI.AscendancePlayer
{
	using System.Collections.Generic;
	using System.Linq;

    using Helpers;

    using Santase.Logic;
    using Santase.Logic.Cards;
    using Santase.Logic.Players;

	public class AscendancePlayer : BasePlayer
	{
		private const int MaxDealWeight = 22;

		public override string Name => "Ascendance Player";

		private readonly ICollection<Card> playedCards = new List<Card>();

		private readonly OpponentPosibleCardsProvider opponentPosibleCardsProvider = new OpponentPosibleCardsProvider();
		
		public override PlayerAction GetTurn(PlayerTurnContext context)
		{
			// trump card logic
			if (this.PlayerActionValidator.IsValid(PlayerAction.ChangeTrump(), context, this.Cards))
            {
                return this.ChangeTrump(context.TrumpCard);
            }

			if(this.ShouldCloseGame(context))
			{
				return this.CloseGame();
			}

			return this.ChooseCard(context);
		}

		public bool ShouldCloseGame(PlayerTurnContext context)
		{			
            //additional logic for game close rule
			if (this.PlayerActionValidator.IsValid(PlayerAction.CloseGame(), context, this.Cards))
			{
				var shouldClose = false;
				if (this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) >= 4)
					shouldClose = true;
				if (this.Cards.Count(x => x.Suit == context.TrumpCard.Suit) == 3 && opponentPosibleCardsProvider.GetOpponentCards(this.Cards, this.playedCards, context.TrumpCard).Count <= 1)
					shouldClose = true;
				if(shouldClose)
					return true;
            }

			return false;
		}

		private PlayerAction ChooseCard(PlayerTurnContext context)
        {
            var possibleCardsToPlay = this.PlayerActionValidator.GetPossibleCardsToPlay(context, this.Cards);

			return this.ChoseAction(context, possibleCardsToPlay, context.State.ShouldObserveRules, context.IsFirstPlayerTurn);
        }

		private PlayerAction ChoseAction(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay, bool shouldObserveRules, bool isFirstPlayerTurn)
		{
			if (isFirstPlayerTurn)
			{ 
				var action = this.TryToAnnounce20Or40(context, possibleCardsToPlay);
				if (action != null)
					return action;
            }

            // get the opponent possible Cards via Helper method
			var oponentPosibleCards = isFirstPlayerTurn ? opponentPosibleCardsProvider.GetOpponentCards(this.Cards, this.playedCards, context.TrumpCard) : new List<Card>() { context.FirstPlayedCard };
			var actionWeights = new Dictionary<Card, int>();
			
            //calculate the weight of every possible combination in the current turn
			foreach(var card in possibleCardsToPlay)
			{
				var cardWeight = CalculateCardPlayedWeight(card, context.TrumpCard, oponentPosibleCards, shouldObserveRules, isFirstPlayerTurn);
				actionWeights.Add(card, cardWeight);
			}

			var maxWeight = -MaxDealWeight;
			var cardToPlay = this.Cards.First();

            // get the heighest possible weight
			foreach(var cardWeight in actionWeights)
			{
				if(cardWeight.Value > maxWeight && PlayerActionValidator.IsValid(PlayerAction.PlayCard(cardWeight.Key),context, this.Cards))
				{
					if (isFirstPlayerTurn && context.TrumpCard != null && cardWeight.Key.Suit == context.TrumpCard.Suit)
						continue;
					cardToPlay = cardWeight.Key;
					maxWeight = cardWeight.Value;
				}
			}

			// optimization TODO: if more then one card is with same weight implement logic that choses between them
            return this.PlayCard(cardToPlay);
		}

		public override void EndRound()
        {
            this.playedCards.Clear();
            base.EndRound();
        }

		public override void EndTurn(PlayerTurnContext context)
        {
            this.playedCards.Add(context.FirstPlayedCard);
            this.playedCards.Add(context.SecondPlayedCard);
        }

        // calculate the average weight for every possible combination in the current turn
		private int CalculateCardPlayedWeight(Card playedCard, Card trumpCard, ICollection<Card> opponentPosibleCards, bool shouldObserveRules , bool isFirstPlayerTurn)
		{
			if (isFirstPlayerTurn)
			{
				var totalWeight = 0;

				foreach(var opponentCard in opponentPosibleCards)
				{
					if(trumpCard != null && playedCard.Suit == trumpCard.Suit)
					{
						if(opponentCard.Suit == trumpCard.Suit)
						{
							totalWeight +=  playedCard.GetValue() > opponentCard.GetValue() ? GetDealWeight(playedCard, opponentCard) : -GetDealWeight(playedCard, opponentCard);
						}
						else
						{
							totalWeight +=  GetDealWeight(playedCard, opponentCard);
						}
					}
					else if(trumpCard != null && playedCard.Suit != trumpCard.Suit)
					{
						if(opponentCard.Suit == trumpCard.Suit)
						{
							totalWeight += -GetDealWeight(playedCard, opponentCard);
						}
						else
						{
							totalWeight +=  playedCard.GetValue() > opponentCard.GetValue() ? GetDealWeight(playedCard, opponentCard) : -GetDealWeight(playedCard, opponentCard);
						}
					}
					else
					{
						if (playedCard.Suit == opponentCard.Suit)
						{
							totalWeight += playedCard.GetValue() > opponentCard.GetValue() ? GetDealWeight(playedCard, opponentCard) : -GetDealWeight(playedCard, opponentCard);
						}
						else
						{
							totalWeight += GetDealWeight(playedCard, opponentCard);
						}
					}
				}

				return opponentPosibleCards.Count == 0 ? totalWeight : totalWeight / opponentPosibleCards.Count;
			}
			else
			{
				var opponentCard = opponentPosibleCards.First();
				if(trumpCard != null && opponentCard.Suit == trumpCard.Suit)
				{
					if(playedCard.Suit == trumpCard.Suit)
					{
						return playedCard.GetValue() > opponentCard.GetValue() ? GetDealWeight(playedCard, opponentCard) : -GetDealWeight(playedCard, opponentCard);
					}
					else
					{
						return -GetDealWeight(playedCard, opponentCard);
					}
				}

				if(trumpCard != null && opponentCard.Suit != trumpCard.Suit)
				{
					if (playedCard.Suit == trumpCard.Suit)
					{
						return GetDealWeight(playedCard, opponentCard);
					}
					else if (playedCard.Suit == opponentCard.Suit)
					{
						return playedCard.GetValue() > opponentCard.GetValue() ? GetDealWeight(playedCard, opponentCard) : -GetDealWeight(playedCard, opponentCard);
					}
					else
					{
						return -GetDealWeight(playedCard, opponentCard);
					}
				}

				if(trumpCard == null)
				{
					if(playedCard.Suit == opponentCard.Suit)
					{
						return playedCard.GetValue() > opponentCard.GetValue() ? GetDealWeight(playedCard, opponentCard) : -GetDealWeight(playedCard, opponentCard);
					}
					else
					{
						return -GetDealWeight(playedCard, opponentCard);
					}
				}
			}

			return 0;
		}

		private static int GetDealWeight(Card firstCard, Card secondCard)
		{
			return firstCard.GetValue() + secondCard.GetValue();
		}

		private PlayerAction TryToAnnounce20Or40(PlayerTurnContext context, ICollection<Card> possibleCardsToPlay)
        {
            // Choose card with announce 40 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard) == Announce.Forty)
                {
                    return this.PlayCard(card);
                }
            }

            // Choose card with announce 20 if possible
            foreach (var card in possibleCardsToPlay)
            {
                if (card.Type == CardType.Queen
                    && this.AnnounceValidator.GetPossibleAnnounce(this.Cards, card, context.TrumpCard)
                    == Announce.Twenty)
                {
                    return this.PlayCard(card);
                }
            }

            return null;
        }
	}
}
