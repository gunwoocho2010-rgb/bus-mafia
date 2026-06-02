using System.Collections.Generic;

namespace BusMafia.Models
{
    public class GameRoom
    {
        public static List<Player> ConnectedPlayers { get; set; } = new List<Player>();
        public static string CurrentGameState { get; set; } = "Lobby"; // Lobby, Day, Night
    }
}