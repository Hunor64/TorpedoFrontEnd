using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorpedoFrontEnd
{
    public class Cell : INotifyPropertyChanged
    {
        private string _display = "";
        public string Display
        {
            get { return _display; }
            set
            {
                _display = value;
                OnPropertyChanged(nameof(Display));
            }
        }

        public bool IsHit { get; set; } = false;
        // Add properties to represent ships, if necessary

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
