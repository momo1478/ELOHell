/* EloHell.cs
by monish1478@gmail.com
Free to use as is in any way you want with no warranty.
*/

using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;
using System.ComponentModel;

namespace PRoConEvents
{
    //Aliases

    public class EloHell : PRoConPluginAPI, IPRoConPluginInterface
	{
		Dictionary<String, ELO> ELOList = new Dictionary<string, ELO>();

        private bool fIsEnabled;
		private int fDebugLevel;

		public EloHell()
		{
			fIsEnabled = false;
			fDebugLevel = 2;
		}

        #region Utility
		public void ConsoleWriteObjectProperties(object obj) 
		{
			string s = string.Empty;
			foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
			{
				string name = descriptor.Name;
				object value = descriptor.GetValue(obj);
				s += string.Format("{0}={1}\n", name, value);
			}
			ConsoleWrite(s);
		}
        #endregion

        #region Server Utility Methods

        public enum MessageType { Warning, Error, Exception, Normal };

		public String FormatMessage(String msg, MessageType type)
		{
			String prefix = "[^b" + GetPluginName() + "^n] ";

			if (type.Equals(MessageType.Warning))
				prefix += "^1^bWARNING^0^n: ";
			else if (type.Equals(MessageType.Error))
				prefix += "^1^bERROR^0^n: ";
			else if (type.Equals(MessageType.Exception))
				prefix += "^1^bEXCEPTION^0^n: ";

			return prefix + msg;
		}

		public void LogWrite(String msg)
		{
			this.ExecuteCommand("procon.protected.pluginconsole.write", msg);
		}

		private void LogChat(string Message)
		{
			ExecuteCommand("procon.protected.chat.write", Message);
		}

		public void ConsoleWrite(String msg, MessageType type = MessageType.Normal)
		{
			LogWrite(FormatMessage(msg, type));
		}

		public void DebugWrite(String msg, int level)
		{
			if (fDebugLevel >= level) ConsoleWrite(msg, MessageType.Normal);
		}

		private void Yell(string Message)
		{
			ExecuteCommand("procon.protected.send", "admin.yell", Message, "8", "all");
			LogChat(Message);
		}
		private void ListPlayers()
		{
			ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
		}

		public void ServerCommand(params String[] args)
		{
			List<String> list = new List<String>();
			list.Add("procon.protected.send");
			list.AddRange(args);
			this.ExecuteCommand(list.ToArray());
		}

        #endregion

        #region ELO

        class ELO
        {
			public static int ELOConstant { get; set; } = 30;

            public string Name { get; private set; }
			public float Rating { get; private set; }
            public int Kills { get; set; }
            public int Deaths { get; set; }

            public ELO(string name, float rating = 1500f, int kills = 0, int deaths = 0)
            {
				Name = name;
				Rating = rating;
				Kills = kills;
				Deaths = deaths;
            }

			public (float,float) Beat(ELO player)
            {
				float oldARating = this.Rating;
				float oldBRating = player.Rating;

				EloRating(this.Rating, player.Rating, ELOConstant, true);

				return (this.Rating - oldARating, player.Rating - oldBRating);
			}

			// Function to calculate the Probability 
			static float Probability(float rating1, float rating2)
			{
				return 1.0f * 1.0f / (1 + 1.0f * (float)(Math.Pow(10, 1.0f * (rating1 - rating2) / 400)));
			}

			// Function to calculate Elo rating 
			// K is a constant. 
			// d determines whether Player A wins or 
			// Player B.  
			public static void EloRating(float Ra, float Rb, int K, bool d)
			{
				// To calculate the Winning 
				// Probability of Player B 
				float Pb = Probability(Ra, Rb);

				// To calculate the Winning 
				// Probability of Player A 
				float Pa = Probability(Rb, Ra);

				// Case -1 When Player A wins 
				// Updating the Elo Ratings 
				if (d == true)
				{
					Ra = Ra + K * (1 - Pa);
					Rb = Rb + K * (0 - Pb);
				}

				// Case -2 When Player B wins 
				// Updating the Elo Ratings 
				else
				{
					Ra = Ra + K * (0 - Pa);
					Rb = Rb + K * (1 - Pb);
				}

				//Console.Write("Updated Ratings:-\n");

				//Console.Write("Ra = " + (Math.Round(Ra
				//			 * 1000000.0) / 1000000.0)
				//			+ " Rb = " + Math.Round(Rb
				//			 * 1000000.0) / 1000000.0);
			}
		}

        #endregion

        #region Plugin Info
        public String GetPluginName()
		{
			return "Elo Hell";
		}

		public String GetPluginVersion()
		{
			return "0.0.0.1";
		}

		public String GetPluginAuthor()
		{
			return "MrLoveCrouches";
		}

		public String GetPluginWebsite()
		{
			return "website?lololololol";
		}

		public String GetPluginDescription()
		{
			return @"
			<h1>ELO Hell</h1>
			<p>ELO Hell is a plugin that assigns Chess Stle ELO Rankings to Players based on Kill/Death Statistics</p>
			<h2>Description</h2>
			<p>TBD</p>
			<h2>Commands</h2>
			<p>TBD</p>
			<h2>Settings</h2>
			<p>TBD</p>
			<h2>Development</h2>
			<p>TBD</p>
			<h3>Changelog</h3>
			<blockquote><h4>1.0.0.0 (11-30-2020)</h4>
			- initial version<br/>
			</blockquote>
			";
		}

        #endregion

        public List<CPluginVariable> GetDisplayPluginVariables()
		{
			List<CPluginVariable> lstReturn = new List<CPluginVariable>();

			lstReturn.Add(new CPluginVariable("Settings|Debug level", fDebugLevel.GetType(), fDebugLevel));

			return lstReturn;
		}

		public List<CPluginVariable> GetPluginVariables()
		{
			return GetDisplayPluginVariables();
		}

		public void SetPluginVariable(String strVariable, String strValue)
		{
			if (Regex.Match(strVariable, @"Debug level").Success)
			{
				int tmp = 2;
				int.TryParse(strValue, out tmp);
				fDebugLevel = tmp;
			}
		}

		public void OnPluginLoaded(String strHostName, String strPort, String strPRoConVersion)
		{
			this.RegisterEvents(this.GetType().Name, "OnVersion", "OnServerInfo", "OnResponseError", "OnListPlayers", "OnPlayerJoin", "OnPlayerLeft", "OnPlayerKilled", "OnPlayerSpawned", "OnPlayerTeamChange", "OnGlobalChat", "OnTeamChat", "OnSquadChat", "OnRoundOverPlayers", "OnRoundOver", "OnRoundOverTeamScores", "OnLoadingLevel", "OnLevelStarted", "OnLevelLoaded");
		}

		public void OnPluginEnable()
		{
			fIsEnabled = true;
			ConsoleWrite("Enabled!");
			ListPlayers();
		}

		public void OnPluginDisable()
		{
			fIsEnabled = false;
			ConsoleWrite("Disabled!");
		}


		public override void OnVersion(String serverType, String version) { }

		public override void OnServerInfo(CServerInfo serverInfo)
		{
			
		}

		public override void OnResponseError(List<String> requestWords, String error) { }

		public override void OnListPlayers(List<CPlayerInfo> players, CPlayerSubset subset)
		{
			foreach (var player in players)
			{
				if (!ELOList.ContainsKey(player.SoldierName))
				{
					ELOList[player.SoldierName] = new ELO(player.SoldierName);
				}
			}
        }

        public override void OnPlayerJoin(String soldierName)
		{
			if(!ELOList.ContainsKey(soldierName))
            {
				ELOList[soldierName] = new ELO(soldierName);
            }
		}

		public override void OnPlayerLeft(CPlayerInfo playerInfo)
		{
		}

		public override void OnPlayerKilled(Kill kKillerVictimDetails) 
		{
			string killerName = kKillerVictimDetails.Killer.SoldierName;
			string victimName = kKillerVictimDetails.Victim.SoldierName;

			ELO killer = ELOList[killerName];
			ELO victim = ELOList[victimName];
			
			(float killer, float victim) diff = killer.Beat(victim);

			ConsoleWrite($"{killerName} ({(int)killer.Rating}) |{(diff.killer >= 0f ? "+" : "") + diff.killer}|".PadRight(40) +
				         $" [{kKillerVictimDetails.DamageType}] " +
						 $"{victimName} ({(int)victim.Rating}) |{(diff.victim >= 0f ? "+" : "") + diff.victim}|".PadLeft(40));
		}

		public override void OnPlayerSpawned(String soldierName, Inventory spawnedInventory) { }

		public override void OnPlayerTeamChange(String soldierName, int teamId, int squadId) { }

		public override void OnGlobalChat(String speaker, String message) { }

		public override void OnTeamChat(String speaker, String message, int teamId) { }

		public override void OnSquadChat(String speaker, String message, int teamId, int squadId) { }

		public override void OnRoundOverPlayers(List<CPlayerInfo> players) { }

		public override void OnRoundOverTeamScores(List<TeamScore> teamScores) { }

		public override void OnRoundOver(int winningTeamId) { }

		public override void OnLoadingLevel(String mapFileName, int roundsPlayed, int roundsTotal) { }

		public override void OnLevelStarted() { }

		public override void OnLevelLoaded(String mapFileName, String Gamemode, int roundsPlayed, int roundsTotal) { } // BF3
    }

}