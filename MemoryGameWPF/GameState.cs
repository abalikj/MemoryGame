using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemoryGameWPF
{
    [Serializable]
    public class GameState
    {
        public List<Card> Cards { get; set; }
        public int TotalMatches { get; set; }
        public int Tries { get; set; }
        public int Seconds { get; set; }
        public bool IsPlayerTurn { get; set; }
        public string Difficulty { get; set; }

        // Чуваме индексите на картите што ботот ги памети, по патеката (сликата)
        public Dictionary<string, List<int>> BotMemoryIndices { get; set; }
    }
}
