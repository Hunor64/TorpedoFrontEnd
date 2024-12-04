using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Windows;
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

        bool placementPhase = true;

        // Ships for each player
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

        private readonly MainWindow mainWindow;

        public GameViewModel(MainWindow window)
        {
            mainWindow = window;

            // Initialize grids
            Player1Cells = new ObservableCollection<Cell>();
            Player2Cells = new ObservableCollection<Cell>();
            InitializeCells(Player1Cells);
            InitializeCells(Player2Cells);

            // Initialize ships
            InitializePlayerShips();

            // Commands
            RotateShipCommand = new RelayCommand<object>(RotateShip);
            PlaceShipCommand = new RelayCommand<Cell>(PlaceShip, CanPlaceShip);
            FireCommand = new RelayCommand<Cell>(Fire, CanFire);

            // Select the first ship to place
            SelectedShip = Player1Ships.FirstOrDefault();
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
            Player2Ships = new ObservableCollection<Ship>(ships.Select(s => new Ship { Name = s.Name, Size = s.Size }));
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
                cell.Display = "⛵";
                SelectedShip.Cells.Add(cell);
            }

            SelectedShip.IsPlaced = true;

            // Remove the placed ship from the player's ships collection
            ships.Remove(SelectedShip);

            // Add the placed ship to the PlacedShips collection

            // Proceed to the next ship or switch player
            if (ships.Count == 0)
            {
                if (placementPhase)
                {
                    SendShipsToServer();
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

        private void SendShipsToServer()
        {
            var shipsData = new List<ShipData>();
            var ships = Player1Ships;

            foreach (var ship in ships)
            {
                var shipData = new ShipData
                {
                    Name = ship.Name,
                    Cells = ship.Cells.Select(c => new CellData { X = c.X, Y = c.Y }).ToList()
                };
                shipsData.Add(shipData);
            }

            string json = JsonSerializer.Serialize(shipsData);
            mainWindow.SendMessageToServer($"SHIPS_{mainWindow.playerID}_{json}");
            MessageBox.Show($"SHIPS_{mainWindow.playerID}_{json}");
        }

        private bool CanFire(Cell cell)
        {
            return !IsPlacementPhase && cell != null && !cell.IsHit &&
                   ((isPlayer1Turn && Player2Cells.Contains(cell)) ||
                    (!isPlayer1Turn && Player1Cells.Contains(cell)));
        }

        private void Fire(Cell cell)
        {
            if (cell == null || cell.IsHit)
                return;

            cell.IsHit = true;

            if (cell.Ship != null)
            {
                cell.Display = "💥"; // Hit
                if (cell.Ship.Cells.All(c => c.IsHit))
                {
                    // Ship is sunk
                    // Optionally notify the player
                }

                if (IsGameOver())
                {
                    string winner = isPlayer1Turn ? "Player 1" : "Player 2";
                    // Notify about the game over
                    mainWindow.SendMessageToServer($"GAME_OVER {winner}");
                    return;
                }
            }
            else
            {
                cell.Display = "🌊"; // Miss
            }

            // Send the firing action to the server
            string message = $"FIRE {cell.X},{cell.Y}";
            mainWindow.SendMessageToServer(message);

            SwitchTurns();
            OnPropertyChanged(nameof(CurrentPlayer));
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
    }
    public class ShipData
    {
        public string Name { get; set; }
        public List<CellData> Cells { get; set; }
    }

    public class CellData
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}