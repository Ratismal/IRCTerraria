using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace IndigoIRC
{
    [ApiVersion(1, 20)]
    public class IndigoIRC : TerrariaPlugin
    {
	    public Configuration config;

        private int chatIndex;
        public static StreamWriter writer;
        public DateTime startTime;

		private KeyValueConfigurationCollection Settings { get; set; }
		private string Channel { get; set; }

        private TcpClient irc;

        public override Version Version
        {
            get { return new Version("2.0"); }
        }
        public override string Name
        {
            get { return "IndigoIRC"; }
        }
        public override string Author
        {
            get { return "Ratismal"; }
        }
        public override string Description
        {
            get { return "IRC in Terraria"; }
        }
        public IndigoIRC(Main game)
            : base(game)
        {
            Order = 0;
        }

        public override void Initialize()
        {
            Console.WriteLine("Initializing IndigoIRC");

            //Store current time to a variable, used later for calculating uptime
	        startTime = DateTime.Now;

            Commands.ChatCommands.Add(new Command("indigoirc.irc", IIRC, "irc"));

	        if (!Directory.Exists("IIRC"))
	        {
		        Directory.CreateDirectory("IIRC");
	        }
            
            //creates config file and bypasses starting the connection thread if config file doesn't exist
            if (!File.Exists("IIRC/IndigoIRC.settings"))
            {
                CreateConfig();
            }
            else //config file exists, starting connection thread
            {
                UpdateConfig();
	            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap
	            {
		            ExeConfigFilename = "IIRC/IndigoIRC.settings"
	            };
	            config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
	            Settings = config.AppSettings.Settings;
	            Channel = Settings["channel"].Value;

                ServerApi.Hooks.ServerChat.Register(this, OnChat);
                ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Register(this, OnLeave);

	            var myThread = new Thread(Connect) {IsBackground = true};

	            Console.WriteLine("[IndigoIRC] Starting IRC thread");

                myThread.Start();
            }
        }

        private void Connect()
        {
            string host = Settings["host"].Value;
            int port = Convert.ToInt32(Settings["port"].Value);
            string nick = Settings["nick"].Value;
            string channel = Settings["channel"].Value;
	        string user = String.Format("USER {0} 0 * :{1}", nick, Settings["name"].Value);
	        string password = Settings["auth"].Value;
            string ingameFormatting = Settings["ingameFormatting"].Value;
            string ingameColour = Settings["ingameColour"].Value;
	        string[] colorCodes = ingameColour.Split(';');
            string commandPrefix = Settings["commandPrefix"].Value;

            byte r = 255;
            byte g = 117;
            byte b = 117;
            if (colorCodes.Length == 3)
            {
                r = Convert.ToByte(colorCodes[0]);
                g = Convert.ToByte(colorCodes[1]);
                b = Convert.ToByte(colorCodes[2]);
            }

	        Console.WriteLine("[IndigoIRC] Connecting to " + host + ":" + port + " on channel " + channel);

	        try
			{
				irc = new TcpClient(host, port);
				var stream = irc.GetStream();
				var reader = new StreamReader(stream);
				writer = new StreamWriter(stream);
				if (!string.IsNullOrEmpty(password))
				{
					writer.WriteLine("PASS " + password);
					writer.Flush();
				}
				writer.WriteLine("NICK " + nick);
				writer.Flush();
				writer.WriteLine(user);
				writer.Flush();

                while (true)
                {
	                string inputLine;
	                while ((inputLine = reader.ReadLine()) != null)
                    {
                        // Split the lines sent from the server by spaces. This seems the easiest way to parse them.
	                    string[] splitInput = inputLine.Split(' ');

                        if (splitInput[0] == "PING")
                        {
                            string pongReply = splitInput[1];
                            writer.WriteLine("PONG " + pongReply);
                            writer.Flush();
                        }
                        else if (splitInput[1].Equals("PRIVMSG"))
                        {
	                        if (splitInput[3].Equals(":" + commandPrefix + "list"))
	                        {
		                        List<string> players = TShock.Players.Where(p => p != null && p.Active)
			                        .Select(p => p.Name).ToList();
		                        var playerList = String.Format("({0}/{1}):", players.Count(), TShock.Config.MaxSlots);
		                        writer.WriteLine("PRIVMSG " + channel + " :" + playerList);

		                        playerList = String.Join(", ", players);
		                        writer.WriteLine("PRIVMSG " + channel + " :" + playerList);
		                        writer.Flush();
	                        }
	                        else if (splitInput[3].Equals(":" + commandPrefix + "uptime"))
	                        {
		                        TimeSpan uptime = DateTime.Now - startTime;
		                        string format = TimeSpanFormatter(uptime);
		                        writer.WriteLine("PRIVMSG " + channel + " :Server uptime: " + format);
		                        writer.Flush();
	                        }
	                        else if (splitInput[3].Equals(":" + commandPrefix + "version"))
	                        {
		                        writer.WriteLine("PRIVMSG " + channel + " :IndigoIRC is running on version: " + Version);
		                        writer.Flush();
	                        }
	                        else if (splitInput[3].Equals(":" + commandPrefix + "help"))
	                        {
		                        writer.WriteLine("PRIVMSG " + channel + " :Valid commands: help, list, version");
		                        writer.Flush();
	                        }
	                        else if (splitInput[0].Contains("!"))
	                        {
		                        int loc = splitInput[0].IndexOf("!", StringComparison.Ordinal);
		                        if (loc > 0)
		                        {
			                        splitInput[0] = splitInput[0].Substring(0, loc);
			                        if (!IgnoreIrc(ReplaceFirst(splitInput[3], ":", "")))
			                        {
				                        splitInput[0] = ingameFormatting.Replace("%NAME%", splitInput[0]);
				                        string message = String.Join(" ", splitInput);
				                        message = ReplaceFirst(message, "PRIVMSG", "");
				                        message = ReplaceFirst(message, channel, "");
				                        message = ReplaceFirst(message, "   :", "");

				                        message = ReplaceFirst(message, ":", "");
				                        TSPlayer.All.SendMessage(message, r, g, b);
			                        }
		                        }
	                        }
                        }
                        else if (splitInput[1].Equals("JOIN"))
                        {
	                        int loc = splitInput[0].IndexOf("!", StringComparison.Ordinal);
	                        if (loc > 0)
	                        {
		                        splitInput[0] = splitInput[0].Substring(0, loc);
		                        splitInput[0] = ReplaceFirst(splitInput[0], ":", "");
	                        }
	                        if (!splitInput[0].Equals(nick))
	                        {
		                        Chat(Color.LightPink, "[IRC] " + splitInput[0] + " joined the channel.");
	                        }
                        }
                        else if (splitInput[1].Equals("QUIT"))
                        {
	                        int loc = splitInput[0].IndexOf("!", StringComparison.Ordinal);
	                        if (loc > 0)
	                        {
		                        splitInput[0] = splitInput[0].Substring(0, loc);
		                        splitInput[0] = ReplaceFirst(splitInput[0], ":", "");
	                        }
	                        if (!splitInput[0].Equals(nick))
	                        {
		                        Chat(Color.LightPink, "[IRC] " + splitInput[0] + " left the channel.");
	                        }
                        }
	                    switch (splitInput[1])
                        {
                            case "001":
                                string joinString = "JOIN " + channel;
                                writer.WriteLine(joinString);
                                writer.Flush();
                                break;
                            case "002":
                                if (!config.AppSettings.Settings["auth"].Value.Equals(""))
                                {
                                    string authenticate = "PRIVMSG NickServ :IDENTIFY " + config.AppSettings.Settings["nick"].Value + " " + config.AppSettings.Settings["password"].Value;
                                    writer.WriteLine(authenticate);
                                    writer.Flush();
                                }
                                
                                break;
                        }
                    }
                    // Close all streams
                    writer.Close();
                    reader.Close();
                    irc.Close();
                }
            }

            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Connect();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            }
            base.Dispose(disposing);
        }

        private void OnChat(ServerChatEventArgs args)
        {
            if (args.Handled)
            {
                return;
            }

            TSPlayer player = TShock.Players[args.Who];

            if (player == null)
            {
                return;
            }

	        if (Ignore(args.Text))
	        {
		        return;
	        }

            if (!args.Text.StartsWith("/"))
            {
                string words = args.Text;
                words = config.AppSettings.Settings["ircFormatting"].Value + words;
                words = words.Replace("%NAME%", player.Name);

                writer.WriteLine("PRIVMSG " + Channel + " :" + words);
                writer.Flush();
            }
        }

        private void Chat(Color color, string message)
        {
            TSPlayer.All.SendMessage(message, color);
        }

        private void CreateConfig()
        {
            Console.WriteLine("[IndigoIRC] No config file found!");
            Console.WriteLine("[IndigoIRC] Generating config file from defaults.");
            Console.WriteLine("[IndigoIRC] Please go to IIRC/IndigoIRC.settings and change the settings. (REQUIRED)");
            Console.WriteLine("[IndigoIRC] This plugin will be activated upon the next reboot.");
	        ExeConfigurationFileMap configMap = new ExeConfigurationFileMap
	        {
		        ExeConfigFilename = "IIRC/IndigoIRC.settings"
	        };
	        config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            config.AppSettings.Settings.Add("host", "irc.esper.net");
            config.AppSettings.Settings.Add("port", "6667");
            config.AppSettings.Settings.Add("nick", "IndigoIRCBot");
            config.AppSettings.Settings.Add("name", "IIRCTerraria");
            config.AppSettings.Settings.Add("channel", "#examplechannel");
            config.AppSettings.Settings.Add("auth", "mypassword");
            config.AppSettings.Settings.Add("ingameFormatting", "[IRC] %NAME%> ");
            config.AppSettings.Settings.Add("ingameColour", "255;117;117");
            config.AppSettings.Settings.Add("ircFormatting", "%NAME%> ");
            config.AppSettings.Settings.Add("commandPrefix", ".");
			config.AppSettings.Settings.Add("ignoredPrefixes", "#@");
	        config.AppSettings.Settings.Add("ignoredIrcPrefixes", "!$");
            config.Save(ConfigurationSaveMode.Full);
        }

        public void UpdateConfig()
        {
	        ExeConfigurationFileMap configMap = new ExeConfigurationFileMap
	        {
		        ExeConfigFilename = "IIRC/IndigoIRC.settings"
	        };
	        config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            if (!config.AppSettings.Settings.AllKeys.Contains("host"))
            {
                config.AppSettings.Settings.Add("host", "irc.esper.net");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("port"))
            {
                config.AppSettings.Settings.Add("port", "6667");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("nick"))
            {
                config.AppSettings.Settings.Add("nick", "IndigoIRCBot");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("name"))
            {
				config.AppSettings.Settings.Add("name", "IIRCTerraria");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("channel"))
            {
                config.AppSettings.Settings.Add("channel", "#examplechannel");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("password"))
            {
                config.AppSettings.Settings.Add("password", "");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("ingameFormatting"))
            {
                config.AppSettings.Settings.Add("ingameFormatting", "[IRC] %NAME%> ");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("ingameColour"))
            {
                config.AppSettings.Settings.Add("ingameColour", "255;117;117");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("ircFormatting"))
            {
                config.AppSettings.Settings.Add("ircFormatting", "%NAME%> ");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("commandPrefix"))
            {
                config.AppSettings.Settings.Add("commandPrefix", ".");
            }
	        if (!config.AppSettings.Settings.AllKeys.Contains("ignoredPrefixes"))
	        {
				config.AppSettings.Settings.Add("ignoredPrefixes", "#@");
			}
	        if (!config.AppSettings.Settings.AllKeys.Contains("ignoredIrcPrefixes"))
	        {
		        config.AppSettings.Settings.Add("ignoredIrcPrefixes", "!$");
	        }
            config.Save(ConfigurationSaveMode.Full);
        }

		/// <summary>
		/// Replaces the first occurence of a string with another character
		/// </summary>
		/// <param name="text">String to search in</param>
		/// <param name="search">String to replace</param>
		/// <param name="replace">New string</param>
		/// <returns></returns>
        private string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        private void OnJoin(JoinEventArgs args)
        {
            TSPlayer player = TShock.Players[args.Who];

            string words = player.Name + " joined the game.";

            writer.WriteLine("PRIVMSG " + Channel + " :" + words);
            writer.Flush();
        }

        private void OnLeave(LeaveEventArgs args)
        {
			TSPlayer player = TShock.Players[args.Who];

			if (player == null)
			{
				return;
			}

            string words = player.Name + " left the game.";

            writer.WriteLine("PRIVMSG " + Channel + " :" + words);
            writer.Flush();
        }

        private void IIRC(CommandArgs args)
        {
			NetworkStream stream = irc.GetStream();
            writer = new StreamWriter(stream);
            
            if (args.Parameters.Count == 0)
            {
                args.Player.SendMessage("==== IndigoIRC Commands ====", 96, 245, 88);
                args.Player.SendMessage("/irc - Display this menu", 96, 245, 88);
                args.Player.SendMessage("/irc say <message> - Posts a message to your connected channel", 96, 245, 88);
                args.Player.SendMessage("/irc sendraw <command> - Sends a raw command that is sent to IRC exactly as it's written", 96, 245, 88);
            }
            else if (args.Parameters[0].Equals("say") && (args.Player.Group.HasPermission("indigoirc.irc.say") || args.Player.Group.HasPermission("indigoirc.*")))
            {
                if (args.Parameters.Count >= 2)
                {
                    string message = ":" + String.Join(" ", args.Parameters);
                    message = ReplaceFirst(message, "say ", "");
                    writer.WriteLine("PRIVMSG " + Channel + " " + message);
                    writer.Flush();
                }
                else
                {
                    args.Player.SendErrorMessage("Not enough parameters. Do /irc say <message>");
                }
            }
            else if (args.Parameters[0].Equals("sendraw") && (args.Player.Group.HasPermission("indigoirc.irc.sendraw") || args.Player.Group.HasPermission("indigoirc.*")))
            {
                if (args.Parameters.Count >= 2)
                {
                    string irccommand = args.Parameters[1];
                    args.Parameters.RemoveAt(0);
                    args.Parameters.RemoveAt(0);
                    string message = String.Join(" ", args.Parameters);
                    writer.WriteLine(irccommand + " " + message);
                    writer.Flush();
                }
                else
                {
                    args.Player.SendErrorMessage("Not enough parameters. Try /irc sendraw <command>");
                }
            }
        }

	    private string TimeSpanFormatter(TimeSpan time)
	    {
			int day = time.Days;
			int hour = time.Hours;
			int minute = time.Minutes;
			int second = time.Seconds;

		    return String.Format("{0} day{4} {1} hour{5} {2} minute{6} {3} second{7}",
			    day, hour, minute, second, Suffix(day), Suffix(hour), Suffix(minute), Suffix(second));
	    }

	    private string Suffix(int num)
	    {
		    return num == 0 || num > 1 ? "s" : "";
	    }

	    private bool Ignore(string text)
	    {
		    return config.AppSettings.Settings["ignoredPrefixes"].Value.Any(c => text.StartsWith(c.ToString()));
	    }

	    private bool IgnoreIrc(string text)
	    {
		    return config.AppSettings.Settings["ignoredIrcPrefixes"].Value.Any(c => text.StartsWith(c.ToString()));
	    }
    }
}
