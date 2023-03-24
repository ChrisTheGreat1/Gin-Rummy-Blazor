﻿using BlazorGinRummy.GinRummyGame.Helpers;
using BlazorGinRummy.GinRummyGame.Models;
using static BlazorGinRummy.GinRummyGame.Helpers.GameLogicMethods;
using System.Text;

namespace BlazorGinRummy.GinRummyGame
{
    public class GinRummyGameService
    {
        private List<Card> deck = new List<Card>();
        private const int HAND_SIZE = 10;
        public List<Card> discardPile { get; private set; } = new List<Card>();
        public List<Card> handPlayerOne { get; private set; } = new List<Card>(); // Human player
        public List<Card> handPlayerTwo { get; private set; } = new List<Card>(); // Simple computer player

        public bool canPlayerOneKnock { get; private set; } = false;
        public bool isPlayerOneKeepPlaying { get; private set; } = true;
        private bool canPlayerTwoKnock = false;
        public bool isGameOver { get; private set; } = false;

        public bool isPlayerOneTurn { get; private set; }

        private Card? pickedUpCard;
        public Card? playerOnePickedUpCard { get; private set; }

        private int handPlayerOneValue;
        private int handPlayerTwoValue;
        public int playerOneRoundScore { get; private set; } = 0;
        public int playerTwoRoundScore { get; private set; } = 0;
        private int winnerNumber = 0;
        public bool isWaitingForPlayerOneInput { get; private set; } = false;
        private bool didNonDealerPickupAtFirstChance = false;
        public bool isPlayerOneMakingFirstCardChoice { get; private set; } = false;
        private bool didPlayerOneStartAsDealer;
        public bool didPlayerOnePickupCard { get; private set; } = false;

        // TODO: rewrite index into a component, create another seperate component for score listing
        // TODO: have toggle button to see opponents hand or hide it
        public List<string> GameStateMessage { get; private set; } = new();

        public GinRummyGameService()
        {
            // TODO: create seperate method for starting new game, see how to incorporate game history into browser storage
            deck = DeckMethods.ShuffleDeck(DeckMethods.CreateDeck());
            DealOutHands();
            SortHandsDetectMelds();
            DetermineIfKnockingEligible();

            //isPlayerOneTurn = false; // TODO: uncomment
            isPlayerOneTurn = DetermineDealer();
            didPlayerOneStartAsDealer = isPlayerOneTurn;

            FirstTurnChanceToPickupFromDiscardPile_Initialize();
        }

        public void PlayerOneChoseKnock()
        {
            //PlayerOneAfterDiscardTasks(true);
            //PlayerOneAfterDiscardTasks();
            AddGameStateMessage_PlayerKnocked();
            SetGameOver();
            NonKnockerCombinesUnmatchedCardsWithKnockersMelds();
            UpdatePlayerScoresAfterKnocking();
        }

        public void PlayerOneChoseKeepPlaying()
        {
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has chosen to continue playing.");
            isPlayerOneKeepPlaying = true;
            //PlayerOneAfterDiscardTasks(true);
            //PlayerOneAfterDiscardTasks();
            PrepareNextTurn();
            SimpleAgentPlaysHand();
        }

        private void NonKnockerCombinesUnmatchedCardsWithKnockersMelds()
        {
            if (isPlayerOneTurn) handPlayerTwo = HandMethods.NonKnockerCombinesUnmatchedCardsWithKnockersMelds(handPlayerOne, handPlayerTwo);
            else handPlayerOne = HandMethods.NonKnockerCombinesUnmatchedCardsWithKnockersMelds(handPlayerTwo, handPlayerOne);
        }

        private void UpdatePlayerScoresAfterKnocking()
        {
            handPlayerOneValue = HandMethods.CalculateHandValue(handPlayerOne);
            handPlayerTwoValue = HandMethods.CalculateHandValue(handPlayerTwo);

            int points = handPlayerOneValue - handPlayerTwoValue;

            if (isPlayerOneTurn)
            {
                if (points == 0)
                {
                    playerTwoRoundScore += 10;
                    winnerNumber = 2;
                    return;
                }

                if (points < 0)
                {
                    playerOneRoundScore += Math.Abs(points);
                    winnerNumber = 1;
                }
                else
                {
                    playerTwoRoundScore += Math.Abs(points);
                    playerTwoRoundScore += 10;
                    winnerNumber = 2;
                }
            }
            else
            {
                if (points == 0)
                {
                    playerOneRoundScore += 10;
                    winnerNumber = 1;
                    return;
                }

                if (points > 0)
                {
                    playerTwoRoundScore += Math.Abs(points);
                    winnerNumber = 2;
                }
                else
                {
                    playerOneRoundScore += Math.Abs(points);
                    playerOneRoundScore += 10;
                    winnerNumber = 1;
                }
            }
        }

        public void PlayerOnePickedUpCardFromDeck()
        {
            AddGameStateMessage_PlayerChoseToPickupCardFromDeck();

            pickedUpCard = deck.Last();
            deck.Remove(deck.Last());

            PlayerOnePickedUpCardTasks();
        }

        public void PlayerOnePickedUpCardFromDiscardPile()
        {
            pickedUpCard = discardPile.Last();
            discardPile.Remove(discardPile.Last());

            PlayerOnePickedUpCardTasks();
        }

        private void PlayerOnePickedUpCardTasks()
        {
            playerOnePickedUpCard = pickedUpCard;

            AddGameStateMessage_PlayerPickedUpCard(pickedUpCard.ToString());
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " - Select a card from your hand to discard.");
            didPlayerOnePickupCard = true;
        }

        private void PrepareNextTurn()
        {
            if (deck.Count == 0)
            {
                EndOfDeckReached();
                return;
            }

            isPlayerOneTurn = !isPlayerOneTurn;
        }

        public void PlayerOneChoseDiscard_DeckCard()
        {
            discardPile.Add(pickedUpCard);

            //PlayerOneAfterDiscardTasks(false);
            PlayerOneAfterDiscardTasks();
        }

        public void PlayerOneChoseDiscard(int userInput)
        {
            discardPile.Add(handPlayerOne[userInput]);
            handPlayerOne[userInput] = pickedUpCard;

            //PlayerOneAfterDiscardTasks(false);
            PlayerOneAfterDiscardTasks();
        }

        // TODO: argument?    bool didPlayerOneAlreadyPass    
        private void PlayerOneAfterDiscardTasks()
        {
            AddGameStateMessage_PlayerDiscarded(discardPile.Last().ToString());

            //if (!didPlayerOneAlreadyPass)
            //{
            SortHandsDetectMelds();
            DetectIfGinHasOccurred();
            DetermineIfKnockingEligible();
            PromptPlayerToKnock();

            pickedUpCard = null;
            playerOnePickedUpCard = null;
            didPlayerOnePickupCard = false;

            //if (!isPlayerOneKeepPlaying) return;
            //}

            if (!isGameOver && isPlayerOneKeepPlaying) PrepareNextTurn();

            if (isPlayerOneMakingFirstCardChoice)
            {
                isPlayerOneMakingFirstCardChoice = false;

                if (!didPlayerOneStartAsDealer)
                {
                    didNonDealerPickupAtFirstChance = true;
                    isWaitingForPlayerOneInput = false;
                    PrepareNextTurn();
                    FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
                }
            }
        }

        public void PlayerOneChosePass()
        {
            isPlayerOneMakingFirstCardChoice = false;

            AddGameStateMessage_PlayerPassed();

            if (didPlayerOneStartAsDealer)
            {
                PrepareNextTurn();
                SimpleAgentPlaysHand();
            }
            else
            {
                isWaitingForPlayerOneInput = false;
                didNonDealerPickupAtFirstChance = false;
                FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
            }
        }

        private void FirstTurnChanceToPickupFromDiscardPile_Initialize()
        {
            if (canPlayerOneKnock || canPlayerTwoKnock)
            {
                GameStateMessage.Add("MISDEAL - atleast one player can knock before any cards have been exchanged.");
                SetGameOver();
                winnerNumber = 0;
                return;
            }

            GameStateMessage.Add($"First turn phase - the non-dealer ({CurrentPlayerString(!didPlayerOneStartAsDealer)}) may choose " +
                $"to pick up the first card from the discard pile, or pass. If the non-dealer passes, " +
                $"the dealer ({CurrentPlayerString(didPlayerOneStartAsDealer)}) is given the " +
                "opportunity to pick up the first card from the discard pile, or pass.");

            PrepareNextTurn();
            AddGameStateMessage_PromptPlayerPickOrPass();

            OfferChanceToPickUpFirstCardFromDiscardPile();

            if (isWaitingForPlayerOneInput) return;
            FirstTurnChanceToPickupFromDiscardPile_DealerTurn();
        }

        private void FirstTurnChanceToPickupFromDiscardPile_DealerTurn()
        {
            PrepareNextTurn();

            // If non-dealer passed up first chance at discard pile, dealer is given chance to pickup the card
            if (!didNonDealerPickupAtFirstChance)
            {
                GameStateMessage.Add("Non-dealer chose to pass - dealer now has chance to pick up card from discard pile.");
                AddGameStateMessage_PromptPlayerPickOrPass();

                OfferChanceToPickUpFirstCardFromDiscardPile();

                if (isWaitingForPlayerOneInput) return;

                PrepareNextTurn();
            }

            SortHandsDetectMelds();
            DetermineIfKnockingEligible();
        }

        private void OfferChanceToPickUpFirstCardFromDiscardPile()
        {
            if (isPlayerOneTurn)
            {
                isPlayerOneMakingFirstCardChoice = true;
                isWaitingForPlayerOneInput = true;
                return;
            }
            else
            {
                var discardPileCard = discardPile.Last();

                handPlayerTwo.Add(discardPileCard);
                handPlayerTwo = HandMethods.DetermineMeldsInHand(handPlayerTwo);

                var nonMeldedCards = handPlayerTwo.Where(c => !c.IsInMeld).ToList();
                var highestDeadwoodCard = nonMeldedCards.OrderByDescending(c => c.Rank).First();

                if (nonMeldedCards.Contains(discardPileCard))
                {
                    if (highestDeadwoodCard == discardPileCard)
                    {
                        handPlayerTwo.Remove(discardPileCard);
                        didNonDealerPickupAtFirstChance = false;

                        AddGameStateMessage_PlayerPassed();
                    }
                    else
                    {
                        PlayerTwoPickUpAndDiscard(discardPileCard, highestDeadwoodCard);
                    }
                }
                else
                {
                    PlayerTwoPickUpAndDiscard(discardPileCard, highestDeadwoodCard);
                }
            }

            void PlayerTwoPickUpAndDiscard(Card discardPileCard, Card highestDeadwoodCard)
            {
                handPlayerTwo.Remove(highestDeadwoodCard);
                discardPile.Remove(discardPileCard);
                discardPile.Add(highestDeadwoodCard);

                didNonDealerPickupAtFirstChance = true;

                AddGameStateMessage_PlayerPickedUpCard(discardPileCard.ToString());
                AddGameStateMessage_PlayerDiscarded(highestDeadwoodCard.ToString());
            }
        }

        private void EndOfDeckReached()
        {
            GameStateMessage.Add("End of deck reached - tallying points remaining in player hands.");

            handPlayerOneValue = HandMethods.CalculateHandValue(handPlayerOne);
            handPlayerTwoValue = HandMethods.CalculateHandValue(handPlayerTwo);

            playerOneRoundScore += handPlayerTwoValue;
            playerTwoRoundScore += handPlayerOneValue;

            SetGameOver();
        }

        public void SimpleAgentPlaysHand()
        {
            if (isGameOver) return;

            var discardPileCard = discardPile.Last();

            handPlayerTwo.Add(discardPileCard);
            handPlayerTwo = HandMethods.DetermineMeldsInHand(handPlayerTwo);

            var nonMeldedCards = handPlayerTwo.Where(c => !c.IsInMeld).ToList();

            // If hand is in gin, remove a card from the players hand
            if (nonMeldedCards.Count == 0)
            {
                var groupedMelds = handPlayerTwo.GroupBy(c => c.MeldGroupIdentifier).ToList();

                var largestMeldGroup = groupedMelds.Where(m => (m.Count() > 3)).First(); // Find a meld with more than 3 cards in it

                handPlayerTwo.Remove(largestMeldGroup.Last());

                return;
            }

            // If card from discard pile doesn't form a meld, pick up a card from the deck
            if (nonMeldedCards.Contains(discardPileCard))
            {
                handPlayerTwo.Remove(discardPileCard);

                pickedUpCard = deck.Last();
                AddGameStateMessage_PlayerChoseToPickupCardFromDeck();
                AddGameStateMessage_PlayerPickedUpCard(pickedUpCard.ToString());

                deck.Remove(deck.Last());

                handPlayerTwo.Add(pickedUpCard);
                handPlayerTwo = HandMethods.DetermineMeldsInHand(handPlayerTwo);

                nonMeldedCards = handPlayerTwo.Where(c => !c.IsInMeld).ToList();

                // If hand is in gin, remove a card from the players hand
                if (nonMeldedCards.Count == 0)
                {
                    var groupedMelds = handPlayerTwo.GroupBy(c => c.MeldGroupIdentifier).ToList();

                    var largestMeldGroup = groupedMelds.Where(m => (m.Count() > 3)).First(); // Find a meld with more than 3 cards in it

                    handPlayerTwo.Remove(largestMeldGroup.Last());

                    return;
                }

                var highestDeadwoodCard = nonMeldedCards.OrderByDescending(c => c.Rank).First();

                handPlayerTwo.Remove(highestDeadwoodCard);
                discardPile.Add(highestDeadwoodCard);

                AddGameStateMessage_PlayerDiscarded(highestDeadwoodCard.ToString());
            }

            // If card did complete a meld, discard the highest deadwood value non-melded card remaining in hand
            else
            {
                var highestDeadwoodCard = nonMeldedCards.OrderByDescending(c => c.Rank).First();

                handPlayerTwo.Remove(highestDeadwoodCard);
                discardPile.Remove(discardPileCard);
                discardPile.Add(highestDeadwoodCard);

                AddGameStateMessage_PlayerPickedUpCard(discardPileCard.ToString());
                AddGameStateMessage_PlayerDiscarded(highestDeadwoodCard.ToString());
            }

            DetectIfGinHasOccurred();
            DetermineIfKnockingEligible();
            PromptPlayerToKnock();
            PrepareNextTurn();
        }

        private void DealOutHands()
        {
            for (int i = 0; i < HAND_SIZE; i++)
            {
                handPlayerOne.Add(deck.Last());
                deck.Remove(deck.Last());

                handPlayerTwo.Add(deck.Last());
                deck.Remove(deck.Last());
            }

            discardPile.Add(deck.Last());
            deck.Remove(deck.Last());
        }

        public string HandToString(List<Card> hand)
        {
            var sb = new StringBuilder();

            foreach (var card in hand)
            {
                sb.Append(card.ToString());
                sb.Append(' ');
            }

            return sb.ToString();
        }

        private void SortHandsDetectMelds()
        {
            handPlayerOne = HandMethods.DetermineMeldsInHand(handPlayerOne);
            handPlayerTwo = HandMethods.DetermineMeldsInHand(handPlayerTwo);
        }

        private void DetermineIfKnockingEligible()
        {
            if (isGameOver) return;

            canPlayerOneKnock = HandMethods.CanPlayerKnock(handPlayerOne);
            canPlayerTwoKnock = HandMethods.CanPlayerKnock(handPlayerTwo);
        }

        private void DetectIfGinHasOccurred()
        {
            if (isPlayerOneTurn)
            {
                if (HandMethods.DetectGin(handPlayerOne))
                {
                    playerOneRoundScore += 20;
                    playerOneRoundScore += HandMethods.CalculateHandValue(handPlayerTwo);
                    winnerNumber = 1;
                    SetGameOver();
                }
            }
            else
            {
                if (HandMethods.DetectGin(handPlayerTwo))
                {
                    playerTwoRoundScore += 20;
                    playerTwoRoundScore += HandMethods.CalculateHandValue(handPlayerOne);
                    winnerNumber = 2;
                    SetGameOver();
                }
            }
        }

        private void SetGameOver()
        {
            isGameOver = true;
            GameStateMessage.Add("GAME OVER");
        }

        private void PromptPlayerToKnock()
        {
            if (isGameOver) return;
            if ((isPlayerOneTurn && !canPlayerOneKnock) || (!isPlayerOneTurn && !canPlayerTwoKnock)) return;

            AddGameStateMessage_PlayerCanKnock();

            if (isPlayerOneTurn)
            {
                isPlayerOneKeepPlaying = false;
                return;
            }
            else
            {
                // TODO: uncomment
                //AddGameStateMessage_PlayerKnocked();
                //SetGameOver();
                //NonKnockerCombinesUnmatchedCardsWithKnockersMelds();
                //UpdatePlayerScoresAfterKnocking();
            }
        }

        private void AddGameStateMessage_PlayerCanKnock()
        {
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " can knock (hand value less than 10 points).");
        }

        private void AddGameStateMessage_PlayerKnocked()
        {
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has chosen to knock and end the game.");
        }

        private void AddGameStateMessage_PlayerPickedUpCard(string card)
        {
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " picked up " + card);
        }

        private void AddGameStateMessage_PlayerChoseToPickupCardFromDeck()
        {
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has chosen to pick up a card from the deck.");
        }

        private void AddGameStateMessage_PlayerDiscarded(string card)
        {
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " discarded " + card);
        }

        private void AddGameStateMessage_PlayerPassed()
        {
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " has chosen to pass.");
        }

        private void AddGameStateMessage_PromptPlayerPickOrPass()
        {
            GameStateMessage.Add(CurrentPlayerString(isPlayerOneTurn) + " - Click the card in the discard pile if you wish to pick it up, or press the pass button.");
        }
    }
}
