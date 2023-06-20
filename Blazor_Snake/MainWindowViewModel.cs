﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HighScoresApiClient;
using MongoDbData.Data;
using Snake.Core.DataModels;
using Snake.Core.Models;
using Snake.Core.ViewModels;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Blazor_Snake;

/// <summary>
/// The main window view model for the main window
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    #region Fields

    private Random _random = new Random();

    private const int MAX_GAME_GRID_SIZE = 400;
    public const int MAX_GAME_GRID_ROWS = 40;
    public const int MAX_GAME_GRID_COLUMNS = 40;
    private const string HIGH_SCORES_PATH = "Resources/HighScores.json";

    private Direction _currentDirection = Direction.DOWN;
    private Queue<NextMove> _nextMoves = new Queue<NextMove>();
    private object _lock = new object();
    private int _snakeSpeed = 200;

    #endregion

    #region Properties

    public CancellationTokenSource Cancellation { get; } = new CancellationTokenSource();

    public Action UiUpdateAction;
    public Action GameoverAction;

    public int TotalGameGridCells => MAX_GAME_GRID_ROWS * MAX_GAME_GRID_COLUMNS;

    /// <summary>
    /// The size of the game grid
    /// </summary>
    public int GameGridSize => MAX_GAME_GRID_SIZE;

    /// <summary>
    /// The current score
    /// </summary>
    [ObservableProperty]
    private int score = 0;

    /// <summary>
    /// Flag to let us know if the game is over
    /// </summary>
    [ObservableProperty]
    private bool gameOver = true;

    /// <summary>
    /// Flag to let us know if the main menu is visible
    /// </summary>
    [ObservableProperty]
    private bool mainMenuVisible = true;

    /// <summary>
    /// Flag to let us know if the high scores are visible
    /// </summary>
    [ObservableProperty]
    private bool highScoresVisible;

    /// <summary>
    /// Fruit grows randomly and grows the snake
    /// </summary>
    [ObservableProperty]
    private CellViewModel fruit;

    /// <summary>
    /// The snake to draw in the game grid
    /// </summary>
    public ObservableCollection<CellViewModel> Snake { get; set; } = new ObservableCollection<CellViewModel>();

    /// <summary>
    /// The high scores
    /// </summary>
    [ObservableProperty]
    public ObservableCollection<HighScoreViewModel> highScores = new ObservableCollection<HighScoreViewModel>();

    #endregion

    #region Constructor

    public MainWindowViewModel()
    {
        JsInterop.JsKeyUpAction = JsKeyUp;
    }

    #endregion

    #region Command Methods

    /// <summary>
    /// Shows the high scores
    /// </summary>
    [RelayCommand]
    public async Task ShowHighScores()
    {
        var hs = await LoadHighScores();
        HighScores = new ObservableCollection<HighScoreViewModel>(hs);
    }

    /// <summary>
    /// Hides the high scores menu and shows the main menu
    /// </summary>
    [RelayCommand]
    private void ShowMainMenu()
    {
        HighScoresVisible = false;
        MainMenuVisible = true;

        var jsonString = JsonSerializer.Serialize(HighScores, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(HIGH_SCORES_PATH, jsonString);
    }

    /// <summary>
    /// Hides the main menu and starts the gameloop
    /// </summary>
    [RelayCommand]
    public void Play()
    {
        CreateSnake();
        SpawnFruit();
        MainMenuVisible = false;
        GameOver = false;
        Score = 0;
        Task.Run(() => GameLoop());
    }

    public async Task AddNewHighScoreToList(int score)
    {
        var highScores = await LoadHighScores(true);
        var hs = new HighScoreViewModel();
        hs.Score = score;
        hs.IsOldScore = false;
        hs.Focus = true;

        if (score > highScores.Last().Score)
        {
            if (score > highScores.First().Score)
            {
                highScores.Insert(0, hs);
            }
            else
            {
                bool scoreAdded = false;
                for (int i = highScores.Count - 1; i >= 0; i--)
                {
                    if (score <= highScores[i].Score)
                    {
                        highScores.Insert(i + 1, hs);
                        scoreAdded = true;
                        break;
                    }
                }

                if (!scoreAdded)
                {
                    highScores.Add(hs);
                }
            }

            if (highScores.Count > 10)
            {
                highScores.RemoveAt(10);
            }

            while (highScores.Last().Score == 0)
            {
                highScores.Remove(highScores.Last());
            }
        }

        HighScores = new ObservableCollection<HighScoreViewModel>(highScores);
        HighScoresVisible = true;
    }

    public async void SaveHighScoreToDb(string name, int score)
    {
        var highScore = new HighScoreModel()
        {
            Name = name.ToUpper(),
            Score = score
        };

        var result = await HighScoresRequests.SaveHighScore(highScore);
    }

    #endregion

    #region Methods

    private async Task<List<HighScoreViewModel>> LoadHighScores(bool withZeros = false)
    {
        var highScores = new List<HighScoreViewModel>();
        var list = await HighScoresRequests.LoadHighScores();
        list.ForEach(item => highScores.Add(new HighScoreViewModel
        {
            Name = item.Name,
            Score = item.Score,
        }));

        if (withZeros)
        {
            while (highScores.Count < 10)
            {
                var hs = new HighScoreViewModel();
                hs.Score = 0;
                hs.Name = string.Empty;
                highScores.Add(hs);
            }
        }

        return highScores;
    }

    private void CreateSnake()
    {
        Snake.Clear();
        var snakeHead = new CellViewModel(200, 200);
        snakeHead.PositionIndex = 20 + (20 * MAX_GAME_GRID_ROWS);
        snakeHead.Rgb = CellViewModel.SNAKE_HEAD_RGB;
        Snake.Add(snakeHead);
    }

    /// <summary>
    /// The game loop
    /// </summary>
    private void GameLoop()
    {
        while (!GameOver && !Cancellation.Token.IsCancellationRequested)
        {
            Thread.Sleep(_snakeSpeed);

            if (_nextMoves.Any())
            {
                var move = _nextMoves.Dequeue();
                _currentDirection = move.Direction;
                CheckIfSnakeEatSelf(move.Xpos, move.Ypos);
                CheckIfSnakeHitWall(move.Xpos, move.Ypos);
                if (!GameOver)
                {
                    MoveSnake(move.Xpos, move.Ypos);
                }
            }
            else
            {
                MoveSnake();
            }

            CheckIfFruitEaten();
        }

        if (Cancellation.Token.IsCancellationRequested)
        {
            Cancellation.Dispose();
            return;
        }

        GameoverAction?.Invoke();

        
    }

    private void JsKeyUp(int code)
    {
        int xPos = 0;
        int yPos = 0;
        Direction newDirecton = Direction.LEFT;
        
        switch (code)
        {
            case 37:
                if (_currentDirection == Direction.RIGHT && Snake.Count > 2)
                    return;
                xPos -= CellViewModel.CELL_SIZE;
                newDirecton = Direction.LEFT;
                break;
            case 38:
                if (_currentDirection == Direction.DOWN && Snake.Count > 2)
                    return;
                yPos -= CellViewModel.CELL_SIZE;
                newDirecton = Direction.UP;
                break;
            case 39:
                if (_currentDirection == Direction.LEFT && Snake.Count > 2)
                    return;
                xPos += CellViewModel.CELL_SIZE;
                newDirecton = Direction.RIGHT;
                break;
            case 40:
                if (_currentDirection == Direction.UP && Snake.Count > 2)
                    return;
                yPos += CellViewModel.CELL_SIZE;
                newDirecton = Direction.DOWN;
                break;
        }

        _nextMoves.Enqueue(new NextMove(xPos, yPos, newDirecton));
    }

    /// <summary>
    /// Checks if the snake will hit a wall
    /// </summary>
    /// <param name="xPos"></param>
    /// <param name="yPos"></param>
    private void CheckIfSnakeHitWall(int xPos, int yPos)
    {
        var nextHeadPositionX = Snake.Last().XPos + xPos;
        var nextHeadPositionY = Snake.Last().YPos + yPos;

        if(nextHeadPositionX < 0 || 
           nextHeadPositionY < 0 ||
           nextHeadPositionX >= MAX_GAME_GRID_SIZE || 
           nextHeadPositionY >= MAX_GAME_GRID_SIZE)
        {
            GameOver = true;
        }
    }

    /// <summary>
    /// Checks if the snake will eat its self
    /// </summary>
    /// <param name="xPos"></param>
    /// <param name="yPos"></param>
    private void CheckIfSnakeEatSelf(int xPos, int yPos)
    {
        var nextHeadPositionX = Snake.Last().XPos + xPos;
        var nextHeadPositionY = Snake.Last().YPos + yPos;

        for (int index = 1; index < Snake.Count - 1; index++)
        {
            if (Snake[index].YPos.Equals(nextHeadPositionY) && Snake[index].XPos.Equals(nextHeadPositionX))
            {
                GameOver = true;
            }
        }
    }

    /// <summary>
    /// Checks if the snake ate a fruit. If yes than a new fruit will spawn and the snake grows
    /// </summary>
    private void CheckIfFruitEaten()
    {
        var snakeX = Snake.Last().XPos;
        var snakeY = Snake.Last().YPos;
        if (snakeX.Equals(Fruit.XPos) && snakeY.Equals(Fruit.YPos))
        {
            GrowSnake();
            SpawnFruit();
            Score++;
            _snakeSpeed -= 2;
        }
    }

    /// <summary>
    /// Moves the snake one step in current direction
    /// </summary>
    private void MoveSnake()
    {
        int xPos = 0;
        int yPos = 0;

        switch (_currentDirection)
        {
            case Direction.LEFT:
                xPos -= CellViewModel.CELL_SIZE;
                break;
            case Direction.UP:
                yPos -= CellViewModel.CELL_SIZE;
                break;
            case Direction.RIGHT:
                xPos += CellViewModel.CELL_SIZE;
                break;
            case Direction.DOWN:
                yPos += CellViewModel.CELL_SIZE;
                break;
        }

        CheckIfSnakeEatSelf(xPos, yPos);
        CheckIfSnakeHitWall(xPos, yPos);
        if (!GameOver)
        {
            MoveSnake(xPos, yPos);
        }
    }

    /// <summary>
    /// Moves the snake to the next position
    /// </summary>
    /// <param name="xPos"></param>
    /// <param name="yPos"></param>
    private void MoveSnake(int xPos, int yPos)
    {
        for(int index = 0; index < Snake.Count-1; index++)
        {
            Snake[index].XPos = Snake[index + 1].XPos;
            Snake[index].YPos = Snake[index + 1].YPos;
            Snake[index].PositionIndex = Snake[index + 1].PositionIndex;
        }

        int newX = Snake.Last().XPos + xPos;
        int newY = Snake.Last().YPos + yPos;

        if(newX < 0)
        {
            newX = MAX_GAME_GRID_SIZE-10;
        }
        else if(newX > MAX_GAME_GRID_SIZE-10)
        {
            newX = 0;
        }

        if (newY < 0)
        {
            newY = MAX_GAME_GRID_SIZE-10;
        }
        else if (newY > MAX_GAME_GRID_SIZE-10)
        {
            newY = 0;
        }

        Snake.Last().XPos = newX;
        Snake.Last().YPos = newY;
        Snake.Last().PositionIndex = newX / CellViewModel.CELL_SIZE + (newY / CellViewModel.CELL_SIZE * MAX_GAME_GRID_ROWS);
        UiUpdateAction?.Invoke();
    }

    /// <summary>
    /// Grows the snake
    /// </summary>
    private void GrowSnake()
    {
        var snakeSection = new CellViewModel(Snake.First().XPos, Snake.First().YPos);
        if (Snake.First().Rgb.Equals(CellViewModel.SNAKE_HEAD_RGB))
        {
            snakeSection.Rgb = CellViewModel.SNAKE_BODY1_RGB;
        }
        else if (Snake.First().Rgb.Equals(CellViewModel.SNAKE_BODY1_RGB))
        {
            snakeSection.Rgb = CellViewModel.SNAKE_BODY2_RGB;
        }
        else if (Snake.First().Rgb.Equals(CellViewModel.SNAKE_BODY2_RGB))
        {
            snakeSection.Rgb = CellViewModel.SNAKE_BODY3_RGB;
        }
        else
        {
            snakeSection.Rgb = CellViewModel.SNAKE_BODY1_RGB;
        }

        Snake.Insert(0, snakeSection);
    }

    /// <summary>
    /// Generates a new fruit at a random location
    /// </summary>
    private void SpawnFruit()
    {
        bool foundSection = false;
        int xPos = 0;
        int yPos = 0;
        do
        {
            xPos = _random.Next(0, MAX_GAME_GRID_ROWS) * 10;
            yPos = _random.Next(0, MAX_GAME_GRID_COLUMNS) * 10;
            foundSection = Snake.FirstOrDefault(item => item.XPos.Equals(xPos) && item.YPos.Equals(yPos)) != null;
        } while (foundSection);

        Fruit = new CellViewModel(xPos, yPos);
        Fruit.PositionIndex = Fruit.XPos / CellViewModel.CELL_SIZE + (Fruit.YPos / CellViewModel.CELL_SIZE * MAX_GAME_GRID_ROWS);
    }

    #endregion
}
