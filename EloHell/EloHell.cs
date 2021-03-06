﻿/* EloHell.cs
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
using System.Linq;
using System.Text;

namespace PRoConEvents
{
    //Aliases

    public class EloHell : PRoConPluginAPI, IPRoConPluginInterface
	{
		Dictionary<String, ELO> ELOList = new Dictionary<string, ELO>();

        private bool fIsEnabled;
		private int fDebugLevel;
		private int TopNRanks;
		private int ELOConstant;

		public EloHell()
		{
			fIsEnabled = false;
			fDebugLevel = 2;
			TopNRanks = 10;
			ELOConstant = 30;
		}

        #region Utility
		public void ConsoleWriteObjectProperties(object obj) 
		{
			StringBuilder s = new StringBuilder();
			foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
			{
				string name = descriptor.Name;
				object value = descriptor.GetValue(obj);
				s.AppendFormat("{0}={1}\n", name, value);
			}
			ConsoleWrite(s.ToString());
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

        class ELO : IComparable<ELO>
        {
			public static int ELOConstant { get; set; } = 30;

            public string Name { get; private set; }
			public float Rating { get; private set; }
            public int Kills { get; set; }
            public int Deaths { get; set; }

			public float KDR => (float)Kills / (Deaths == 0 ? 1 : Deaths);

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

				this.Kills++;
				player.Deaths++;

				EloRating(this, player, true);

				return (this.Rating - oldARating, player.Rating - oldBRating);
			}

			// Function to calculate the Probability 
			float Probability(float playerOneRating, float playerTwoRating)
			{
				return 1f / (1f + (float)Math.Pow(10, (playerTwoRating - playerOneRating) / 400.0));
			}

			// Adjusts ELOs of Ra and Rb based on AWon
			public void EloRating(ELO Ra, ELO Rb, bool AWon)
			{
				int delta = (int)(ELO.ELOConstant * (Convert.ToInt32(AWon) - Probability(Ra.Rating, Rb.Rating)));

				Ra.Rating += delta;
				Rb.Rating -= delta;
			}

            public int CompareTo(ELO other)
            {
				return (int)(other.Rating - this.Rating);
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
			lstReturn.Add(new CPluginVariable("Settings|Display Top N Ranks", TopNRanks.GetType(), TopNRanks));
			lstReturn.Add(new CPluginVariable("Settings|ELO Constant", ELOConstant.GetType(), ELOConstant));

			return lstReturn;
		}

		public void SetPluginVariable(String strVariable, String strValue)
		{
			ConsoleWrite($"Var {strVariable} Val {strValue}");
			if (Regex.Match(strVariable, @"Debug level").Success)
			{
				int tmp = 2;
				int.TryParse(strValue, out tmp);
				fDebugLevel = tmp;
			}
			else if (Regex.Match(strVariable, @"Display Top N Ranks").Success)
			{
				int tmp = 10;
				int.TryParse(strValue, out tmp);
				TopNRanks = tmp;
			}
			else if (Regex.Match(strVariable, @"ELO Constant").Success)
			{
				int tmp = 30;
				int.TryParse(strValue, out tmp);
				ELOConstant = tmp;
				ELO.ELOConstant = ELOConstant;
			}
		}

		public List<CPluginVariable> GetPluginVariables()
		{
			return GetDisplayPluginVariables();
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
			List<ELO> ELOObjects = ELOList.Values.ToList();
			ELOObjects.Sort();
			
			StringBuilder s = new StringBuilder($"\n^4---Top {TopNRanks} ELO Rank---^0\n");
			
            for (int i = 0, rank = 1; i < TopNRanks; i++)
            {
				ELO player = ELOObjects[i];
				s.AppendLine($"#{rank++} {player.Name} --> {player.Rating}".PadRight(75) + $"KD({player.Kills}/{player.Deaths}) |{ player.KDR.ToString("F2")}|");
			} 
			ConsoleWrite(s.ToString());
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
			if(!kKillerVictimDetails.IsSuicide || kKillerVictimDetails.DamageType != "DamageArea")
            {
				string killerName = kKillerVictimDetails.Killer.SoldierName;
				string victimName = kKillerVictimDetails.Victim.SoldierName;

				ELO killer = ELOList[killerName];
				ELO victim = ELOList[victimName];

				(float killer, float victim) diff = killer.Beat(victim);

				ConsoleWrite($"{killerName} ({(int)killer.Rating}) |{(diff.killer >= 0f ? "+" : "") + diff.killer}|".PadRight(40) +
							 $" [{kKillerVictimDetails.DamageType}] ".PadLeft(20) +
							 $"{victimName} ({(int)victim.Rating}) |{(diff.victim >= 0f ? "+" : "") + diff.victim}|".PadLeft(40));
			}
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