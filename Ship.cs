using System.Collections.Generic;

namespace TorpedoFrontEnd
{
    public class Ship
    {
        public string Name { get; set; }
        public int Size { get; set; }
        public bool IsPlaced { get; set; } = false;
        public List<Cell> Cells { get; set; } = new List<Cell>();
    }
}