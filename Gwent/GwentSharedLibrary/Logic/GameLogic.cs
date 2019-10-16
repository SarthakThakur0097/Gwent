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

        public GameLogic (GameRepository gameRepository) {
            this.gameRepository = gameRepository;
        }

        public GameState StartGame (int playerOneId, int playerTwoId)
        {
            Game game = gameRepository.CreateGame(playerOneId, playerTwoId);

            Deck playerOneDeck = gameRepository.GetPlayerDeck(playerOneId);
            Deck playerTwoDeck = gameRepository.GetPlayerDeck(playerTwoId);

            Pile playerOneHand = gameRepository.CreateHand(game, playerOneDeck);
            Pile playerTwoHand = gameRepository.CreateHand(game, playerTwoDeck);

            GameRound currentRound = gameRepository.AddGameRound(game);
            return GetGameState(game);
        }

        public GameState PlayCard (int pileCardId, Game myGame)
        {
            PileCard cardToPlay = gameRepository.GetPileCardById(pileCardId);
            GameRound currentGameRound = gameRepository.GetCurrentGameRounds(myGame)[0];
            gameRepository.MakeMove(cardToPlay, currentGameRound);
            return GetGameState(myGame);
        }

        public GameState PassMove (int playerId, Game myGame)
        {
            GameRound currentGameRound = gameRepository.GetCurrentGameRounds(myGame)[0];
            currentGameRound = gameRepository.PassTurn(currentGameRound, playerId);
            return GetGameState(myGame);
        }

        public bool HasRoundEnded (GameRound currentRound)
        {
            //each time a player passes, this method is called
            return (currentRound.FirstPlayerPassed && currentRound.SecondPlayerPassed);
        }

        public GameState WhoWins(GameState gameState, GameRound gameRound, Pile player1hand, Pile player2hand)
        {
            //Each time round ends, this method is called
            if (HasRoundEnded(gameRound))
            {
                gameRepository.MoveCardsToDiscardPile(player1hand);
                gameRepository.MoveCardsToDiscardPile(player2hand);
                if (gameState.RoundState.Player1RoundState.Score > gameState.RoundState.Player2RoundState.Score)
                {
                    gameState.Player1State.RoundsWon++;
                    if(gameState.Player1State.RoundsWon == 2)
                    {
                        gameState.Winner.PlayerId = gameState.Player1State.PlayerId;
                        gameState.Winner.PlayerName = gameState.Player1State.FirstName;
                    }
                }
                else if (gameState.RoundState.Player1RoundState.Score < gameState.RoundState.Player2RoundState.Score)
                {
                    gameState.Player2State.RoundsWon++;
                    if (gameState.Player2State.RoundsWon == 2)
                    {
                        gameState.Winner.PlayerId = gameState.Player2State.PlayerId;
                        gameState.Winner.PlayerName = gameState.Player2State.FirstName;
                    }
                }
                //Equal scores?
            }

            gameRound = gameRepository.AddGameRound(gameRound.Game);
            return gameState;

        }

        //once both players have passed
        //use a private method to see who won
        //update roundswon in PlayerState
        private List<PlayerHandState> PlayerStateHelper(int playerId)
        {
            Pile playerPile = gameRepository.GetPileByDeckId(gameRepository.GetPlayerDeck(playerId).Id);
            List<PileCard> playerPileCards = gameRepository.GetCardsInPile(playerPile);
            var playerPileInfo = new List<PlayerHandState>();

            foreach (var pileCard in playerPileCards)
            {
                PlayerHandState playerHandState = new PlayerHandState()
                {
                    PileCardId = pileCard.Id,
                    ImageUrl = pileCard.Card.ImageUrl
                };
                playerPileInfo.Add(playerHandState);
            }
            return playerPileInfo;
        }

        //Helper function to get cards on board of type
        public List<BoardCardState> GetCardsOnBoard(CardType cardType, int playerId)
        {
            Pile playerPile = gameRepository.GetPileByDeckId(gameRepository.GetPlayerDeck(playerId).Id);
            List<PileCard> playerPileCards = gameRepository.GetCardsInPile(playerPile);
            var playerBoardInfo = new List<BoardCardState>();

            //int score = 0;

            foreach(var pileCard in playerPileCards)
            {
                if(pileCard.Card.CardType == cardType && pileCard.Location == Location.Board)
                {
                    //score += pileCard.Card.Strength.Value;
                    BoardCardState boardCardState = new BoardCardState();

                    boardCardState.PileCardId = pileCard.Id;
                    boardCardState.Image = pileCard.Card.ImageUrl;
                    boardCardState.SetScore(pileCard.Card.Strength.Value);
                    
                    playerBoardInfo.Add(boardCardState);
                }
            }
            return playerBoardInfo;
        }

        public GameState GetGameState(Game myGame)
        {
            return new GameState()
            {
                GameId = myGame.Id,
                RoundNumber = gameRepository.GetCurrentGameRounds(myGame).Count,
                Player1State = new PlayerState()
                {
                    FirstName = myGame.PlayerOne.FirstName,
                    PlayerId = myGame.PlayerOne.Id,
                    //RoundsWon = 0, 
                    PlayerHandState = PlayerStateHelper(myGame.PlayerOneId)
                    //PlayerHandState = new Dictionary<string, PlayerHandState>(
                },
                Player2State = new PlayerState()
                {
                    FirstName = myGame.PlayerTwo.FirstName,
                    PlayerId = myGame.PlayerTwoId,
                    PlayerHandState = PlayerStateHelper(myGame.PlayerTwoId)
                },
                RoundState = new RoundState()
                {
                    GameRoundId = gameRepository.GetCurrentRound(myGame).Id,
                    Player1RoundState = new PlayerRoundState()
                    {
                        CloseCombat = new CardTypeState()
                        {
                            BoardCardState = GetCardsOnBoard(CardType.CloseCombat, myGame.PlayerOneId),
                        },
                        Ranged = new CardTypeState()
                        {
                            BoardCardState = GetCardsOnBoard(CardType.Ranged, myGame.PlayerOneId)
                        },
                        Siege = new CardTypeState()
                        {
                            BoardCardState = GetCardsOnBoard(CardType.Seige, myGame.PlayerOneId)
                        },
                    },
                    Player2RoundState = new PlayerRoundState()
                    {
                        CloseCombat = new CardTypeState()
                        {
                            BoardCardState = GetCardsOnBoard(CardType.CloseCombat, myGame.PlayerTwoId),
                        },
                        Ranged = new CardTypeState()
                        {
                            BoardCardState = GetCardsOnBoard(CardType.Ranged, myGame.PlayerTwoId)
                        },
                        Siege = new CardTypeState()
                        {
                            BoardCardState = GetCardsOnBoard(CardType.Seige, myGame.PlayerTwoId)
                        },
                    },
                },
                Winner = new Winner()
                {
                    PlayerId = 0,
                    PlayerName = ""
                },
            };
        }
    }
}
