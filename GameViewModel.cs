using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace TorpedoFrontEnd
{
    public class GameViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Cell> Player1Cells { get; set; }
        public ObservableCollection<Cell> Player2Cells { get; set; }
        public ICommand FireCommand { get; }

        private bool isPlayer1Turn = true;

        public GameViewModel()
        {
            Player1Cells = new ObservableCollection<Cell>();
            Player2Cells = new ObservableCollection<Cell>();
            InitializeCells(Player1Cells);
            InitializeCells(Player2Cells);
            FireCommand = new RelayCommand<Cell>(Fire);
        }

        private void InitializeCells(ObservableCollection<Cell> cells)
        {
            for (int i = 0; i < 100; i++)
            {
                cells.Add(new Cell());
            }
        }

        private void Fire(Cell cell)
        {
            if (cell != null && !cell.IsHit)
            {
                // Check if the cell is in the opponent's grid
                if ((isPlayer1Turn && Player2Cells.Contains(cell)) ||
                    (!isPlayer1Turn && Player1Cells.Contains(cell)))
                {
                    cell.IsHit = true;
                    cell.Display = "X"; // Mark the cell as hit
                                        // Implement hit or miss logic here

                    SwitchTurns();
                }
                else
                {
                    // Optionally notify the player that they can't fire on their own grid
                }
            }
        }

        private void SwitchTurns()
        {
            isPlayer1Turn = !isPlayer1Turn;
            OnPropertyChanged(nameof(CurrentPlayer));
        }

        public string CurrentPlayer => isPlayer1Turn ? "Player 1's Turn" : "Player 2's Turn";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
