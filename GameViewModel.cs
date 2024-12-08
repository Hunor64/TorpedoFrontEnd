﻿using System;
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
        public ObservableCollection<Cell> Player1Cells { get; set; }
        public ObservableCollection<Cell> Player2Cells { get; set; }

        public ObservableCollection<Ship> Player1Ships { get; set; }
        public ObservableCollection<Ship> Player2Ships { get; set; }

        public ICommand RotateShipCommand { get; }
        public ICommand PlaceShipCommand { get; }
        public ICommand FireCommand { get; }

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

        private bool isPlayerTurn;
        public bool IsPlacementPhase { get; private set; } = true;
        public string CurrentPlayer => isPlayerTurn ? "Your Turn" : "Opponent's Turn";

        private readonly MainWindow mainWindow;

        public GameViewModel(MainWindow window)
        {
            mainWindow = window;

            Player1Cells = new ObservableCollection<Cell>();
            Player2Cells = new ObservableCollection<Cell>();
            InitializeCells(Player1Cells);
            InitializeCells(Player2Cells);

            InitializePlayerShips();

            RotateShipCommand = new RelayCommand<object>(RotateShip);
            PlaceShipCommand = new RelayCommand<Cell>(PlaceShip, CanPlaceShip);
            FireCommand = new RelayCommand<Cell>(Fire, CanFire);

            SelectedShip = Player1Ships.FirstOrDefault();

            isPlayerTurn = mainWindow.playerID == 1;
            OnPropertyChanged(nameof(CurrentPlayer));
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

            var playerCells = Player1Cells;
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

            var playerCells = Player1Cells;

            int startIndex = playerCells.IndexOf(startCell);
            if (startIndex == -1)
                return;

            int row = startIndex / 10;
            int column = startIndex % 10;
            var shipCells = new List<Cell>();

            SelectedShip.Cells = new List<Cell>();

            for (int i = 0; i < SelectedShip.Size; i++)
            {
                int index;
                if (ShipOrientation == ShipOrientation.Horizontal)
                {
                    index = startIndex + i;
                }
                else
                {
                    index = startIndex + i * 10;
                }

                var cell = playerCells[index];
                shipCells.Add(cell);
            }

            foreach (var cell in shipCells)
            {
                cell.Ship = SelectedShip;
                cell.Display = "⛵";
                SelectedShip.Cells.Add(cell);
            }

            SelectedShip.IsPlaced = true;

            Player1Ships.Remove(SelectedShip);

            if (Player1Ships.Count == 0)
            {
                SendShipsToServer();
                IsPlacementPhase = false;
                OnPropertyChanged(nameof(IsPlacementPhase));
            }
            else
            {
                SelectedShip = Player1Ships.FirstOrDefault();
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private void SendShipsToServer()
        {
            var shipsData = new List<ShipData>();

            foreach (var ship in Player1Ships.Concat(Player2Ships))
            {
                if (ship.Cells == null || ship.Cells.Count == 0)
                    continue;

                var shipData = new ShipData
                {
                    Name = ship.Name,
                    Cells = ship.Cells.Select(c => new CellData { X = c.X, Y = c.Y }).ToList()
                };
                shipsData.Add(shipData);
            }

            string json = JsonSerializer.Serialize(shipsData);
            mainWindow.SendMessageToServer($"SHIPSPLACED_{json}");
        }

        private bool CanFire(Cell cell)
        {
            return !IsPlacementPhase && cell != null && !cell.IsHit && isPlayerTurn &&
                   Player2Cells.Contains(cell);
        }

        private void Fire(Cell cell)
        {
            if (cell == null || cell.IsHit || !isPlayerTurn)
                return;

            mainWindow.SendMessageToServer($"FIRE_{cell.X}_{cell.Y}");
        }

        public void HandleFireResult(string message)
        {
            var parts = message.Split('_');

            string result = parts[2];
            int x = int.Parse(parts[3]);
            int y = int.Parse(parts[4]);

            var cell = Player2Cells.FirstOrDefault(c => c.X == x && c.Y == y);
            if (cell == null)
                return;

            cell.IsHit = true;

            if (result == "HIT")
            {
                cell.Display = "💥";
            }
            else
            {
                cell.Display = "🌊";
            }
        }

        public void HandleOpponentAction(string message)
        {
            var parts = message.Split('_');
            if (parts.Length != 3)
                return;

            int x = int.Parse(parts[2]);
            int y = int.Parse(parts[3]);

            var cell = Player1Cells.FirstOrDefault(c => c.X == x && c.Y == y);
            if (cell == null)
                return;

            cell.IsHit = true;

            if (message.StartsWith("OPPONENT_HIT_"))
            {
                cell.Display = "💥";
            }
            else if (message.StartsWith("OPPONENT_MISS_"))
            {
                cell.Display = "🌊";
            }
        }

        public void HandleShipSunk(string message)
        {
            string sunkShipName = message.Substring("SHIP_SUNK_".Length);
            MessageBox.Show($"You have sunk the opponent's {sunkShipName}!");
        }

        public void HandleYourShipSunk(string message)
        {
            string sunkShipName = message.Substring("YOUR_SHIP_SUNK_".Length);
            MessageBox.Show($"Your {sunkShipName} has been sunk!");
        }

        public void UpdateTurn(bool isYourTurn)
        {
            isPlayerTurn = isYourTurn;
            OnPropertyChanged(nameof(CurrentPlayer));
            CommandManager.InvalidateRequerySuggested();
        }

        private void RotateShip(object parameter)
        {
            ShipOrientation = ShipOrientation == ShipOrientation.Horizontal
                ? ShipOrientation.Vertical
                : ShipOrientation.Horizontal;
        }

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
