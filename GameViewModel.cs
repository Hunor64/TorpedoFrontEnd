using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace TorpedoFrontEnd
{
    public enum ShipOrientation
    {
        Horizontal,
        Vertical
    }

    public class GameViewModel : INotifyPropertyChanged
    {
        // Grids for each player
        public ObservableCollection<Cell> Player1Cells { get; set; }
        public ObservableCollection<Cell> Player2Cells { get; set; }

        public bool placementPhase = true;

        // Ships for each player
        public void SetPlacementPhase(bool value)
        {
            placementPhase = value;
        }
        public ICommand RotateShipCommand { get; }

        private void RotateShip(object parameter)
        {
            ShipOrientation = ShipOrientation == ShipOrientation.Horizontal
                ? ShipOrientation.Vertical
                : ShipOrientation.Horizontal;
        }
        public ObservableCollection<Ship> Player1Ships { get; set; }
        public ObservableCollection<Ship> Player2Ships { get; set; }

        // Commands
        public ICommand PlaceShipCommand { get; }
        public ICommand FireCommand { get; }

        // Selected ship and orientation
        private Ship selectedShip;
        public Ship SelectedShip
        {
            get => selectedShip;
            set
            {
                selectedShip = value;
                OnPropertyChanged(nameof(SelectedShip));
            }
        }

        private ShipOrientation shipOrientation = ShipOrientation.Horizontal;
        public ShipOrientation ShipOrientation
        {
            get => shipOrientation;
            set
            {
                shipOrientation = value;
                OnPropertyChanged(nameof(ShipOrientation));
            }
        }

        // Game state tracking
        private bool isPlayer1Turn = true;
        public bool IsPlacementPhase { get; private set; } = true;
        public string CurrentPlayer => isPlayer1Turn ? "Player 1's Turn" : "Player 2's Turn";
        // Inside GameViewModel.cs

        public ObservableCollection<Cell> LocalCells { get; set; }
        public ObservableCollection<Cell> EnemyCells { get; set; }
        public bool IsPlayerTurn
        {
            get => isPlayerTurn;
            set
            {
                isPlayerTurn = value;
                OnPropertyChanged(nameof(IsPlayerTurn));
            }
        }

        private readonly MainWindow mainWindow;

        private bool isPlayerTurn = false;

        public GameViewModel(MainWindow window)
        {
            mainWindow = window;

            // Initialize grids
            Player1Cells = new ObservableCollection<Cell>();
            Player2Cells = new ObservableCollection<Cell>();
            InitializeCells(Player1Cells);
            InitializeCells(Player2Cells);

            // Determine local and enemy cells
            if (mainWindow.playerID == 1)
            {
                LocalCells = Player1Cells;
                EnemyCells = Player2Cells;
            }
            else
            {
                LocalCells = Player2Cells;
                EnemyCells = Player1Cells;
            }

            // Initialize ships
            InitializePlayerShips();

            // Commands
            RotateShipCommand = new RelayCommand<object>(RotateShip);
            PlaceShipCommand = new RelayCommand<Cell>(PlaceShip, CanPlaceShip);
            FireCommand = new RelayCommand<Cell>(Fire, CanFire);

            // Select the first ship to place
            SelectedShip = LocalShips.FirstOrDefault();
        }


        private void InitializeCells(ObservableCollection<Cell> cells)
        {
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 10; x++)
                {
                    cells.Add(new Cell(x, y));
                }
            }
        }

        public ObservableCollection<Ship> LocalShips { get; set; }
        public ObservableCollection<Ship> EnemyShips { get; set; }

        private void InitializePlayerShips()
        {
            var ships = new List<Ship>
            {
                new Ship { Name = "Aircraft Carrier", Size = 5 },
                new Ship { Name = "Battleship", Size = 4 },
                new Ship { Name = "Submarine", Size = 3 },
                new Ship { Name = "Cruiser", Size = 3 },
                new Ship { Name = "Destroyer", Size = 2 }
            };

            Player1Ships = new ObservableCollection<Ship>(ships);
            Player2Ships = new ObservableCollection<Ship>(ships);

            if (mainWindow.playerID == 1)
            {
                LocalShips = Player1Ships;
                EnemyShips = Player2Ships;
            }
            else
            {
                LocalShips = Player2Ships;
                EnemyShips = Player1Ships;
            }
        }
        private bool CanPlaceShip(Cell cell)
        {
            if (!IsPlacementPhase || SelectedShip == null || cell == null)
                return false;

            var playerCells = isPlayer1Turn ? Player1Cells : Player2Cells;
            int startIndex = playerCells.IndexOf(cell);
            if (startIndex == -1)
                return false;

            int row = startIndex / 10;
            int column = startIndex % 10;

            var shipIndices = new List<int>();

            for (int i = 0; i < SelectedShip.Size; i++)
            {
                int index;
                if (ShipOrientation == ShipOrientation.Horizontal)
                {
                    if (column + i >= 10)
                        return false;
                    index = startIndex + i;
                }
                else
                {
                    if (row + i >= 10)
                        return false;
                    index = startIndex + i * 10;
                }

                if (playerCells[index].Ship != null)
                    return false; 

                shipIndices.Add(index);
            }

            foreach (var idx in shipIndices)
            {
                int r = idx / 10;
                int c = idx % 10;

                int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
                int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

                for (int k = 0; k < 8; k++)
                {
                    int nr = r + dr[k];
                    int nc = c + dc[k];
                    if (nr >= 0 && nr < 10 && nc >= 0 && nc < 10)
                    {
                        int neighborIndex = nr * 10 + nc;
                        if (shipIndices.Contains(neighborIndex))
                            continue;
                        if (playerCells[neighborIndex].Ship != null)
                            return false;
                    }
                }
            }

            return true;
        }


        private void PlaceShip(Cell startCell)
        {
            if (startCell == null || SelectedShip == null)
                return;

            var playerCells = isPlayer1Turn ? Player1Cells : Player2Cells;
            var ships = isPlayer1Turn ? Player1Ships : Player2Ships;

            int startIndex = playerCells.IndexOf(startCell);
            if (startIndex == -1)
                return; // Cell not found in the list

            int row = startIndex / 10;
            int column = startIndex % 10;
            var shipCells = new List<Cell>();

            for (int i = 0; i < SelectedShip.Size; i++)
            {
                int index;
                if (ShipOrientation == ShipOrientation.Horizontal)
                {
                    if (column + i >= 10)
                        return; // Ship doesn't fit horizontally
                    index = startIndex + i;
                }
                else // Vertical
                {
                    if (row + i >= 10)
                        return; // Ship doesn't fit vertically
                    index = startIndex + i * 10;
                }

                var cell = playerCells[index];
                if (cell.Ship != null)
                    return; // Cell is already occupied
                shipCells.Add(cell);
            }

            // Place the ship
            foreach (var cell in shipCells)
            {
                cell.Ship = SelectedShip;
                if (mainWindow.playerID == 1)
                {
                    Player1Ships = new ObservableCollection<Ship>(ships);
                    Player2Ships = new ObservableCollection<Ship>();
                    LocalShips = Player1Ships;
                    EnemyShips = Player2Ships;
                }
                else
                {
                    Player1Ships = new ObservableCollection<Ship>();
                    Player2Ships = new ObservableCollection<Ship>(ships);
                    LocalShips = Player2Ships;
                    EnemyShips = Player1Ships;
                }
                cell.Display = "⛵";
                SelectedShip.Cells.Add(cell);
            }

            SelectedShip.IsPlaced = true;

            // Remove the placed ship from the player's ships collection
            ships.Remove(SelectedShip);
            if (ships == null)
            {
                throw new InvalidOperationException("Ships collection is not initialized.");
            }

            ships.Remove(SelectedShip);
            // Proceed to the next ship or switch player
            if (ships.Count == 0)
            {
                if (placementPhase)
                {
                    mainWindow.SendMessageToServer($"SHIPSPLACED_{mainWindow.playerID}");
                    placementPhase = false;
                }
                if (!isPlayer1Turn)
                {
                    // Both players have placed their ships
                    IsPlacementPhase = false;
                    
                    isPlayer1Turn = true;
                    SelectedShip = null;
                }
                else
                {
                    // Switch to player 2 for ship placement
                    isPlayer1Turn = false;
                    SelectedShip = Player2Ships.FirstOrDefault();
                }
            }
            else
            {
                // Select the next ship to place
                SelectedShip = ships.FirstOrDefault();
            }

            OnPropertyChanged(nameof(CurrentPlayer));
            if (!IsPlacementPhase)
            {
                // Send a message to the server indicating ships have been placed
                mainWindow.SendMessageToServer("SHIPS_PLACED");
            }
        }

        private bool CanFire(Cell cell)
        {
            return !IsPlacementPhase && IsPlayerTurn && cell != null && !cell.IsHit && EnemyCells.Contains(cell);
        }
        private void Fire(Cell cell)
        {
            if (cell == null || cell.IsHit)
                return;

            // Disable firing until we receive a response
            IsPlayerTurn = false;

            // Send the coordinates to the server in the format "{X}_{Y}"
            string message = $"{cell.X}_{cell.Y}";
            mainWindow.SendMessageToServer(message);
        }

        private void SwitchTurns()
        {
            isPlayer1Turn = !isPlayer1Turn;
            OnPropertyChanged(nameof(CurrentPlayer));
        }

        private bool IsGameOver()
        {
            var opponentCells = isPlayer1Turn ? Player2Cells : Player1Cells;
            return opponentCells.Where(c => c.Ship != null).All(c => c.IsHit);
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public bool IsShipAtCoordinates(int x, int y)
        {
            var cell = LocalCells.FirstOrDefault(c => c.X == x && c.Y == y);
            return cell?.Ship != null;
        }
        public void ProcessShotResult(int x, int y, bool isHit)
        {
            var cell = EnemyCells.FirstOrDefault(c => c.X == x && c.Y == y);
            if (cell != null)
            {
                cell.IsHit = true;
                cell.Display = isHit ? "💥" : "🌊"; // Hit or Miss
                // Optionally, set cell color: green for hit, red for miss
            }
        }

        public void ProcessIncomingShot(int x, int y, bool isHit)
        {
            var cell = LocalCells.FirstOrDefault(c => c.X == x && c.Y == y);
            if (cell != null)
            {
                cell.IsHit = true;
                cell.Display = isHit ? "💥" : "🌊"; // Hit or Miss
            }
        }
        

    }
}