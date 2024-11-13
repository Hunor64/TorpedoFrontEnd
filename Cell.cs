﻿using System.ComponentModel;

namespace TorpedoFrontEnd
{
    public class Cell : INotifyPropertyChanged
    {
        private string display = "";
        public string Display
        {
            get => display;
            set
            {
                display = value;
                OnPropertyChanged(nameof(Display));
            }
        }

        public bool IsHit { get; set; } = false;
        public Ship Ship { get; set; } = null;

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}