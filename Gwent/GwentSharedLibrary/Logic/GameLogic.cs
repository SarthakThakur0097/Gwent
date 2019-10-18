﻿using GwentSharedLibrary.Models;
using GwentSharedLibrary.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GwentSharedLibrary.Logic
{
    public class GameLogic
    {

        private GameRepository gameRepository;
        public Game Game { get; private set; }

        public GameLogic(GameRepository gameRepository)
        {
            this.gameRepository = gameRepository;
        }

        public GameLogic(GameRepository gameRepository, int gameId) : this(gameRepository)
        {
            this.Game = gameRepository.GetGameById(gameId);
        }

        public void StartGame(int playerOneId, int playerTwoId)
        {
            Game game = new Game(playerOneId, playerTwoId);
            gameRepository.CreateGame(game);

            Deck playerOneDeck = gameRepository.GetPlayerDeck(playerOneId);
            Deck playerTwoDeck = gameRepository.GetPlayerDeck(playerTwoId);

            Pile playerOneHand = gameRepository.CreateHand(game.Id, playerOneId, playerOneDeck);
            Pile playerTwoHand = gameRepository.CreateHand(game.Id, playerTwoId, playerTwoDeck);

            GameRound currentRound = gameRepository.AddGameRound(game);

            //Sets Active Player to PlayerOne
            currentRound.ActivePlayerId = playerOneId;
            gameRepository.UpdateGameRound(currentRound);

            Game = game;
        }

        public void PlayCard(int pileCardId)
        {
            //foreach(var pileCard in Game.Piles)
            PileCard cardToPlay = gameRepository.GetPileCardById(pileCardId);
            GameRound currentGameRound = gameRepository.GetCurrentGameRounds(cardToPlay.Pile.GameId)[0];
            gameRepository.MakeMove(cardToPlay, currentGameRound);

            List<GameRoundCard> gameRoundCards = gameRepository.GetGameRoundCards(currentGameRound.Id);

            if (cardToPlay.Card.CardType == CardType.Weather)
            {
                if (cardToPlay.Card.SpecialAbility == SpecialAbility.Frost)
                {
                    foreach (var gameRoundCard in gameRoundCards)
                    {
                        if (gameRoundCard.PileCard.Card.CardType == CardType.CloseCombat && gameRoundCard.PileCard.Card.SpecialAbility != SpecialAbility.Hero)
                        {
                            gameRoundCard.PileCard.Card.Strength = 1;
                        }
                    }
                }
                else if (cardToPlay.Card.SpecialAbility == SpecialAbility.Fog)
                {
                    foreach (var gameRoundCard in gameRoundCards)
                    {
                        if (gameRoundCard.PileCard.Card.CardType == CardType.Ranged && gameRoundCard.PileCard.Card.SpecialAbility != SpecialAbility.Hero)
                        {
                            gameRoundCard.PileCard.Card.Strength = 1;
                        }
                    }
                }
                if (cardToPlay.Card.SpecialAbility == SpecialAbility.Rain)
                {
                    foreach (var gameRoundCard in gameRoundCards)
                    {
                        if (gameRoundCard.PileCard.Card.CardType == CardType.Seige && gameRoundCard.PileCard.Card.SpecialAbility != SpecialAbility.Hero)
                        {
                            gameRoundCard.PileCard.Card.Strength = 1;
                        }
                    }
                }
            }
            
            string playCardMessage = currentGameRound.ActivePlayer.FirstName + " played a " + cardToPlay.Card.CardType + " card";
            if (currentGameRound.ActivePlayerId == Game.PlayerOneId)
            {
                SendMessage(Game.PlayerTwoId, playCardMessage);
            }
            else
            {
                SendMessage(Game.PlayerOneId, playCardMessage);
            }          

            if (currentGameRound.ActivePlayerId == currentGameRound.FirstPlayerId && !currentGameRound.SecondPlayerPassed)
            {
                currentGameRound.ActivePlayerId = currentGameRound.SecondPlayerId;

                string message = "It's " + currentGameRound.SecondPlayer.FirstName + "'s turn";
                SendMessage(Game.PlayerOneId, message);
                SendMessage(Game.PlayerTwoId, message);
            }
            else if (currentGameRound.ActivePlayerId == currentGameRound.SecondPlayerId && !currentGameRound.FirstPlayerPassed)
            {
                currentGameRound.ActivePlayerId = currentGameRound.FirstPlayerId;

                string message = "It's " + currentGameRound.FirstPlayer.FirstName + "'s turn";
                SendMessage(Game.PlayerOneId, message);
                SendMessage(Game.PlayerTwoId, message);
            }
            gameRepository.UpdateGameRound(currentGameRound);
        }

        public void PassMove(int playerId)
        {
            Game myGame = gameRepository.GetGameById(Game.Id);
            GameRound currentGameRound = gameRepository.GetCurrentGameRounds(myGame.Id)[0];
            currentGameRound = gameRepository.PassTurn(currentGameRound, playerId);

            if (Game.PlayerOneId == playerId)
            {
                string message = Game.PlayerOne.FirstName + " has passed their turn.";
                SendMessage(Game.PlayerTwoId, message);
            }
            else if (Game.PlayerTwoId == playerId)
            {
                string message = Game.PlayerTwo.FirstName + " has passed their turn.";
                SendMessage(Game.PlayerOneId, message);            
            }

            if (HasRoundEnded(currentGameRound))
            {
                WhoWins(playerId);
            }
            else
            {
                string message = "It's " + currentGameRound.ActivePlayer.FirstName + "'s turn.";
                SendMessage(Game.PlayerOneId, message);
                SendMessage(Game.PlayerTwoId, message);
            }


        }

        public bool HasRoundEnded(GameRound currentRound)
        {
            //each time a player passes, this method is called
            return (currentRound.FirstPlayerPassed && currentRound.SecondPlayerPassed);
        }

        public void WhoWins(int playerId)
        {
            GameState gameState = GetGameState(playerId);
            List<GameRound> rounds = gameRepository.GetCurrentGameRounds(Game.Id);
            GameRound currentRound = rounds[0];
            var roundWinnerId = 0;
            //Each time round ends, this method is called
            
            foreach (var pile in Game.Piles)
            {
                gameRepository.MoveCardsToDiscardPile(pile);
            }

            //gameRepository.MoveCardsToDiscardPile();
            //gameRepository.MoveCardsToDiscardPile(player2hand);
            if (gameState.Round.Player.Score > gameState.Round.PlayerOpponent.Score)
            {
                currentRound.WinnerPlayerId = gameState.Player.PlayerId;
                roundWinnerId = gameState.Player.PlayerId;

                //Do I send this message to both players or just one player????
                string message = gameState.Player.FirstName + " has won the round.";
                SendMessage(gameState.Player.PlayerId, message);
                SendMessage(gameState.PlayerOpponent.PlayerId, message);
            }
            else if (gameState.Round.Player.Score < gameState.Round.PlayerOpponent.Score)
            {
                currentRound.WinnerPlayerId = gameState.PlayerOpponent.PlayerId;
                roundWinnerId = gameState.PlayerOpponent.PlayerId;

                //Do I send this message to both players or just one player????
                string message = gameState.PlayerOpponent.FirstName + " has won the round.";
                SendMessage(gameState.Player.PlayerId, message);
                SendMessage(gameState.PlayerOpponent.PlayerId, message);
            }
            //Equal scores?

            gameRepository.UpdateGameRound(currentRound);

            int playerOneWins = rounds.Where(r => r.WinnerPlayerId == Game.PlayerOneId).Count();
            int playerTwoWins = rounds.Where(r => r.WinnerPlayerId == Game.PlayerTwoId).Count();

            if (playerOneWins < 2 && playerTwoWins < 2)
            {
                currentRound = gameRepository.AddGameRound(Game);

                if (roundWinnerId != 0)
                {
                    currentRound.ActivePlayerId = roundWinnerId;
                }
                gameRepository.UpdateGameRound(currentRound);
            }
        }

        private List<PlayerHandState> PlayerStateHelper(Pile pile)
        {
            var playerPileInfo = new List<PlayerHandState>();

            foreach (var pileCard in pile.PileCards)
            {
                if(pileCard.Location == Location.Hand)
                {
                    PlayerHandState playerHandState = new PlayerHandState()
                    {
                        PileCardId = pileCard.Id,
                        ImageUrl = pileCard.Card.ImageUrl
                    };
                    playerPileInfo.Add(playerHandState);
                }
            }
            return playerPileInfo;
        }

        //Helper function to get cards on board of type
        public List<BoardCardState> GetCardsOnBoard(CardType cardType, Pile pile)
        {
            var playerBoardInfo = new List<BoardCardState>();

            foreach (var pileCard in pile.PileCards)
            {
                if (pileCard.Card.CardType == cardType && pileCard.Location == Location.Board)
                {
                    //score += pileCard.Card.Strength.Value;
                    BoardCardState boardCardState = new BoardCardState();

                    boardCardState.PileCardId = pileCard.Id;
                    boardCardState.ImageUrl = pileCard.Card.ImageUrl;
                    boardCardState.SetScore(pileCard.Card.Strength.Value);

                    playerBoardInfo.Add(boardCardState);
                }
            }
            return playerBoardInfo;
        }

        public void SendMessage (int recepientUserId, string message)
        {
            gameRepository.AddGameMessage(Game.Id, message, recepientUserId);
        }

        public GameState GetGameState(int userId)
        {
            Game myGame = gameRepository.GetGameById(Game.Id);
            var rounds = gameRepository.GetCurrentGameRounds(Game.Id);
            var roundNumber = rounds.Count;
            var activePlayerId = rounds[0].ActivePlayerId.Value;

            User player = null;
            User playerOpponent = null;

            if (myGame.PlayerOneId == userId)
            {
                player = myGame.PlayerOne;
                playerOpponent = myGame.PlayerTwo;
            }
            else
            {
                player = myGame.PlayerTwo;
                playerOpponent = myGame.PlayerOne;
            }

            Pile playerPile = Game.Piles.Where(p => p.UserId == player.Id).Single();
            Pile playerOpponentPile = Game.Piles.Where(p => p.UserId == playerOpponent.Id).Single();

            var playerState = new PlayerState()
            {
                FirstName = player.FirstName,
                PlayerId = player.Id,
                RoundsWon = rounds.Where(r => r.WinnerPlayerId == player.Id).Count(),
                Hand = PlayerStateHelper(playerPile),
                IsActive = player.Id == activePlayerId
            };

            var playerOpponentState = new PlayerState()
            {
                FirstName = playerOpponent.FirstName,
                PlayerId = playerOpponent.Id,
                RoundsWon = rounds.Where(r => r.WinnerPlayerId == playerOpponent.Id).Count(),
                Hand = PlayerStateHelper(playerOpponentPile),
                IsActive = playerOpponent.Id == activePlayerId
            };

            var gameState = new GameState()
            {
                GameId = myGame.Id,
                RoundNumber = roundNumber,
                Player = playerState,
                PlayerOpponent = playerOpponentState,
                Round = new RoundState()
                {
                    GameRoundId = rounds[0].Id,
                    Player = new PlayerRoundState()
                    {
                        CloseCombat = new CardTypeState()
                        {
                            Cards = GetCardsOnBoard(CardType.CloseCombat, playerPile),
                        },
                        Range = new CardTypeState()
                        {
                            Cards = GetCardsOnBoard(CardType.Ranged, playerPile)
                        },
                        Siege = new CardTypeState()
                        {
                            Cards = GetCardsOnBoard(CardType.Seige, playerPile)
                        },
                    },
                    PlayerOpponent = new PlayerRoundState()
                    {
                        CloseCombat = new CardTypeState()
                        {
                            Cards = GetCardsOnBoard(CardType.CloseCombat, playerOpponentPile),
                        },
                        Range = new CardTypeState()
                        {
                            Cards = GetCardsOnBoard(CardType.Ranged, playerOpponentPile)
                        },
                        Siege = new CardTypeState()
                        {
                            Cards = GetCardsOnBoard(CardType.Seige, playerOpponentPile)
                        },
                    }
                }
            };

            if (gameState.Winner != null)
            {
                // TODO winning messages
                string winnerMessage = gameState.Winner.PlayerName + " Has Won The Game. Congratulations!";
                string loserMessage = gameState.Winner.PlayerName + " Has Won The Game. Better luck next time!";

                if (gameState.Winner.PlayerId == myGame.PlayerOneId)
                {
                    SendMessage(myGame.PlayerOneId, winnerMessage);
                    SendMessage(myGame.PlayerTwoId, loserMessage);
                }
                else
                {
                    SendMessage(myGame.PlayerTwoId, winnerMessage);
                    SendMessage(myGame.PlayerOneId, loserMessage);
                }
            }

            var messages = gameRepository.getUndeliveredMessages(myGame.Id, userId);

            gameState.Messages = messages.Count > 0 ? messages.Select(m => m.Message).ToList() : null;

            return gameState;
        }
    }
}
