using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryGameWPF
{
    public class Card
    {
        public int Id { get; set; }
        public bool IsFlipped { get; set; }
        public bool IsMatched { get; set; }
        public string ImagePath { get; set; }
    }
}

