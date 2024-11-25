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

        public GameViewModel()
        {
            // Initialize grids
            Player1Cells = new ObservableCollection<Cell>();
            Player2Cells = new ObservableCollection<Cell>();
            InitializeCells(Player1Cells);
            InitializeCells(Player2Cells);
            RotateShipCommand = new RelayCommand<object>(RotateShip);

            // Initialize ships
            InitializePlayerShips();

            // Commands
            PlaceShipCommand = new RelayCommand<Cell>(PlaceShip, CanPlaceShip);
            FireCommand = new RelayCommand<Cell>(Fire, CanFire);

            // Select the first ship to place
            SelectedShip = Player1Ships.FirstOrDefault();
        }

        private void InitializeCells(ObservableCollection<Cell> cells)
        {
            for (int i = 0; i < 100; i++)
            {
                cells.Add(new Cell());
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
                return false; // Cell not found in the list

            int row = startIndex / 10;
            int column = startIndex % 10;

            var shipIndices = new List<int>();

            for (int i = 0; i < SelectedShip.Size; i++)
            {
                int index;
                if (ShipOrientation == ShipOrientation.Horizontal)
                {
                    if (column + i >= 10)
                        return false; // Out of bounds horizontally
                    index = startIndex + i;
                }
                else // Vertical
                {
                    if (row + i >= 10)
                        return false; // Out of bounds vertically
                    index = startIndex + i * 10;
                }

                if (playerCells[index].Ship != null)
                    return false; // Cell already occupied

                shipIndices.Add(index);
            }

            // Check adjacent cells around the ship placement area
            foreach (var idx in shipIndices)
            {
                int r = idx / 10;
                int c = idx % 10;

                // Offsets to get adjacent cells including diagonals
                int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
                int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };

                for (int k = 0; k < 8; k++)
                {
                    int nr = r + dr[k];
                    int nc = c + dc[k];
                    if (nr >= 0 && nr < 10 && nc >= 0 && nc < 10)
                    {
                        int neighborIndex = nr * 10 + nc;
                        // Skip indices that are part of the ship itself
                        if (shipIndices.Contains(neighborIndex))
                            continue;
                        if (playerCells[neighborIndex].Ship != null)
                            return false; // Adjacent cell is occupied
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

            // Proceed to the next ship or switch player
            if (ships.Count == 0)
            {
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
                    // Game over logic
                    string winner = isPlayer1Turn ? "Player 1" : "Player 2";
                    // Display winner or end game
                }
            }
            else
            {
                cell.Display = "🌊"; // Miss
            }

            SwitchTurns();
            OnPropertyChanged(nameof(CurrentPlayer));
        }

        private void SwitchTurns()
        {
            isPlayer1Turn = !isPlayer1Turn;
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
}