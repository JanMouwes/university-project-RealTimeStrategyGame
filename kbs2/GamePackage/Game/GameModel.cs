﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using kbs2.Desktop.World.World;
using kbs2.Faction.FactionMVC;

namespace kbs2.GamePackage.Game
{
	class GameModel
	{
		public WorldController World { get; set; }
		public List<Faction_Controller> Factions { get; set; }
		public float Time { get; set; }
		public GameState GameState { get; set; }
		public Enum GameSpeed { get; set; }
	}
}