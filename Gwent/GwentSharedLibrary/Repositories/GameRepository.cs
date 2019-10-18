﻿using GwentSharedLibrary.Data;
using GwentSharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace GwentSharedLibrary.Repositories
{
    public class GameRepository
    {
        private Context context;

        public GameRepository(Context context)
        {
            this.context = context;
        }

        public void CreateGame(Game game)
        {
            context.Games.Add(game);
            context.SaveChanges();
        }

        //public Game GetGame (int player1Id, int player2Id)
        //{
        //    //Get their game from the DB
        //    Game myGame = context.Games
        //                    .Include(g => g.PlayerOne)
        //                    .Include(g => g.PlayerTwo)
        //                    .Where(g => g.PlayerOneId == player1Id && g.PlayerTwoId == player2Id)
        //                    .SingleOrDefault();

        //    return myGame;
        //}

        public void AddGameMessage (int gameId, string message, int recepientUserId)
        {
            GameMessage myMessage = new GameMessage(gameId, message, recepientUserId);
            context.GameMessages.Add(myMessage);
            context.SaveChanges();
        }

        //get undelivered messages (update isDelivered to true once message is sent back)
        public List<GameMessage> getUndeliveredMessages (int gameId, int recepientUserId)                               //returns undelivered messages and sets isDelivered=true;
        {
            List<GameMessage> messages = new List<GameMessage>();
            messages = context.GameMessages
                    .Include(gm => gm.Game)
                    .Include(gm => gm.RecepientUser)
                    .Where(gm => gm.GameId == gameId && gm.RecepientUserId == recepientUserId && gm.IsDelivered==false)
                    .OrderBy(gm=>gm.Id)
                    .ToList();
            foreach (var message in messages)
            {
                message.IsDelivered = true;
                context.Entry(message).State = EntityState.Modified;
                context.SaveChanges();
            }
            return messages;
        }

        public Game GetGameById (int gameId)
        {
            Game myGame = context.Games
                            .Include(g => g.PlayerOne)
                            .Include(g => g.PlayerTwo)
                            .Include(g => g.Piles.Select(p => p.PileCards.Select(pc => pc.Card)))
                            //.Include(g=> g.Messages)
                            .Where(g => g.Id == gameId)
                            .SingleOrDefault();
            return myGame;
        }


        public GameRound AddGameRound (Game game)
        {

            GameRound gameRound = new GameRound(0, game.Id, game.PlayerOneId, game.PlayerTwoId);

            //Sets activePlayer to PlayerOne no matter who wins
            //gameRound.ActivePlayerId = game.PlayerOneId;

            context.GameRounds.Add(gameRound);
            context.SaveChanges();
            return gameRound;
        }

        public List<GameRound> GetCurrentGameRounds(int gameId)
        {
            return context.GameRounds
                    .Include(gr => gr.Game)
                    .Include(gr => gr.FirstPlayer)
                    .Include(gr => gr.SecondPlayer)
                    .OrderByDescending(gr => gr.Id)
                    .Where(gr => gr.GameId == gameId)
                    .ToList();
        }

        public void UpdateGameRound (GameRound gameRound)
        {
            context.Entry(gameRound).State = EntityState.Modified;
            context.SaveChanges();
        }

        public Deck GetPlayerDeck(int playerId)
        {
            //Deck deck = context.Decks
            //    .Include(d => d.DeckUsers.Select(du => du.User))
            //    .Select(d)
            //    .Where(d => d.DeckUsers.Select(du => du.User))
            //    .FirstOrDefault();

            DeckUser deckUsers = context.DeckUsers
                                    .Include(du=>du.Deck)
                                    .Where(du => du.UserId == playerId)
                                    .FirstOrDefault();


            return deckUsers.Deck;
        }

        public List<Card> DrawCards (int deckId, int numberOfCards)            //Draws card from deck, sets IsDrawn property for card = true
        {
            List<Card> cardList = new List<Card>();
            List<DeckCard> deckCards = context.DeckCards
                                        .Include(dc => dc.Card)
                                        .Where(dc => dc.DeckId == deckId && dc.IsDrawn==false)
                                        .Take(numberOfCards)            //change this to however many cards you need
                                        .ToList();

            foreach(var card in deckCards)
            {
                cardList.Add(card.Card);
                card.IsDrawn = true;
                context.Entry(card).State = EntityState.Modified;
                context.SaveChanges();
            }
            return cardList;
        }

        //public Card DrawOneCard (int deckId)
        //{
        //    DeckCard deckCard = context.DeckCards
        //                        .Include(dc => dc.Card)
        //                        .Where(dc => dc.DeckId == deckId || dc.IsDrawn == false)
        //                        .FirstOrDefault();
        //    return deckCard.Card;
        //}

        public Pile GetPile(int gameId, int userId) {
            return context.Piles
                    .Include(p => p.Game)
                    .Include(p => p.PileCards.Select(pc => pc.Card))
                    .Where(p => p.GameId == gameId && p.UserId == userId)
                    .FirstOrDefault();
        }

        public Pile CreateHand(int gameId, int userId, Deck myDeck)            //Creates Pile (hand) and adds PileCards to that Pile
        {
            Pile hand = new Pile()
            {
                GameId = gameId,
                UserId = userId
            };

            List<Card> cardsList = DrawCards(myDeck.Id, 10);

            foreach (var card in cardsList)
            {
                PileCard pileCard = new PileCard()
                {
                    CardId = card.Id,
                    Card = card,
                    PileId = hand.Id,
                    Pile = hand,
                    Location = Location.Hand
                };
                context.PileCards.Add(pileCard);
                context.SaveChanges();
            }
            return hand;
        }

        public List<PileCard> GetCardsInPile (Pile hand)
        {
            return context.PileCards
                    .Include(pc => pc.Card)
                    .Include(pc => pc.Pile)
                    .Where(pc => pc.PileId == hand.Id)
                    .ToList();

        }

        public List<PileCard> GetCardsInHand(Pile hand)         //returns list of cards currently in hand
        {
            return context.PileCards
                    .Include(pc => pc.Card)
                    .Include(pc => pc.Pile)
                    .Where(pc => pc.PileId == hand.Id && pc.Location == Location.Hand)
                    .ToList();

        }

        public PileCard GetPileCardByCardId (int cardId)        //Returns PileCard based on cardId
        {
            return context.PileCards
                    .Include(pc => pc.Card)
                    .Include(pc => pc.Pile)
                    .Where(pc => pc.CardId == cardId)
                    .FirstOrDefault();
        }

        public PileCard GetPileCardById (int pileCardId)
        {
            return context.PileCards
                    .Include(pc => pc.Card)
                    .Include(pc => pc.Pile)
                    .Where(pc => pc.Id == pileCardId)
                    .FirstOrDefault();
        }

        public void MakeMove(PileCard myHandCard, GameRound currentGameRound)
        {
            myHandCard.Location = Location.Board;
            context.Entry(myHandCard).State = EntityState.Modified;

            //Update player's score

            GameRoundCard myGameRoundCard = new GameRoundCard()
            {
                GameRoundId = currentGameRound.Id,
                GameRound = currentGameRound,
                PileCard = myHandCard,
                PileCardId = myHandCard.Id
            };
            context.GameRoundCards.Add(myGameRoundCard);
            context.SaveChanges();

            //Move PileCard to correct spot (for corresponding player)
            //Update corresponding player's score
            //Update Location of card (remove from hand, move to board)        
        }

        public List<GameRoundCard> GetGameRoundCards(int gameRoundId)
        {
            return context.GameRoundCards
                .Include(grc => grc.GameRound)
                .Include(grc => grc.PileCard)
                .Where(grc => grc.GameRoundId == gameRoundId)
                .ToList();
        }

        public GameRound PassTurn(GameRound myCurrentRound, int playerId)
        {
            if (myCurrentRound.FirstPlayerId == playerId)
            {
                myCurrentRound.FirstPlayerPassed = true;
                myCurrentRound.ActivePlayerId = myCurrentRound.SecondPlayerId;
            }
            else if (myCurrentRound.SecondPlayerId == playerId)
            {
                myCurrentRound.SecondPlayerPassed = true;
                myCurrentRound.ActivePlayerId = myCurrentRound.FirstPlayerId;
            }

            context.Entry(myCurrentRound).State = EntityState.Modified;
            context.SaveChanges();
            return myCurrentRound;
        }

        public void MoveCardsToDiscardPile(Pile hand)
        {
            List<PileCard> pileCards = GetCardsInPile(hand);
            foreach(var pileCard in pileCards)
            {
                if(pileCard.Location == Location.Board)
                {
                    pileCard.Location = Location.Discard;
                    context.Entry(pileCard).State = EntityState.Modified;
                    context.SaveChanges();
                }
            }
        }
    }
}
