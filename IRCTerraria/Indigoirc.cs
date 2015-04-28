using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Configuration;
using System.Threading;

using TShockAPI;
using TShockAPI.Extensions;

using Terraria;
using TerrariaApi;
using TerrariaApi.Server;

using IrcDotNet;

namespace IndigoIRC
{
    [ApiVersion(1, 17)]
    public class IndigoIRC : TerrariaPlugin
    {
        private int chatIndex;
        public static List<Player> Players = new List<Player>();
        //public static TimeSpan currentTime = new TimeSpan();
        //private String host = ConfigurationManager.AppSettings["host"];
        //private int port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);
        //private String pass = null;
        //private String nick = ConfigurationManager.AppSettings["nick"];
        //private String user = "HUTerraria";
        //private String name = ConfigurationManager.AppSettings["name"];
        //private String channel = ConfigurationManager.AppSettings["channel"];
        //private bool ssl = false;
        /*
        private String host = IndigoIRC.Default.host;
        private int port = IndigoIRC.Default.port;
        private String nick = IndigoIRC.Default.nick;
        private String name = IndigoIRC.Default.name;
        private String channel = IndigoIRC.Default.channel;
        */
        public static StreamWriter writer;
        /*
        NetworkStream stream;
        StreamReader reader;
        private String host;
        private int port;
        private String channel;
        private String name;
        private String nick;
         * */
        public TimeSpan initTime;

        private TcpClient irc;

        public override Version Version
        {
            get { return new Version("1.6.0"); }
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

            //chatIndex = 0;
        }
        public override void Initialize()
        {
            Console.Write("Initializing IndigoIRC\n");

            //Store current time to a variable, used later for calculating uptime
            this.initTime = DateTime.Now.TimeOfDay;
            this.initTime = this.initTime + TimeSpan.FromDays(DateTime.Now.Day);
            //Console.WriteLine(this.initTime.Days);
            //Console.WriteLine(this.initTime.Hours);
            //Console.WriteLine(this.initTime.Minutes);
            //Console.WriteLine(this.initTime.Seconds);

            Commands.ChatCommands.Add(new Command("indigoirc.irc", IIRC, "irc"));
            
            
            //creates config file and bypasses starting the connection thread if config file doesn't exist
            if (!File.Exists("IIRC/IndigoIRC.settings"))
            {
                CreateConfig();
            }
            else //config file exists, starting connection thread
            {
                updateConfig();
                ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
                configMap.ExeConfigFilename = "IIRC/IndigoIRC.settings";
                Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

                ServerApi.Hooks.ServerChat.Register(this, OnChat);
                ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Register(this, OnLeave);

                System.Threading.Thread myThread;
                myThread = new Thread(new ThreadStart(connect));

                myThread.IsBackground = true;
                Console.WriteLine("[IndigoIRC] Starting IRC thread");

                myThread.Start();
            }
            

        }
        private void connect()
        {
            
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IIRC/IndigoIRC.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

            //Thread.Sleep(8000);

            String host = config.AppSettings.Settings["host"].Value;
            int port = Convert.ToInt32(config.AppSettings.Settings["port"].Value);
            String nick = config.AppSettings.Settings["nick"].Value;
            String name = config.AppSettings.Settings["name"].Value;
            String channel = config.AppSettings.Settings["channel"].Value;
            String user = config.AppSettings.Settings["user"].Value;
            String ingameFormatting = config.AppSettings.Settings["ingameFormatting"].Value;
            String ingameColour = config.AppSettings.Settings["ingameColour"].Value;
            String[] colorCodes = ingameColour.Split(new Char[] { ';' });
            String commandPrefix = config.AppSettings.Settings["commandPrefix"].Value;
            Color color = Color.LightPink;
            int[] colours = new int[] {1};
            Byte r = 255;
            Byte g = 117;
            Byte b = 117;
            if (colorCodes.Length == 3)
            {
                r = Convert.ToByte(colorCodes[0]);
                g = Convert.ToByte(colorCodes[1]);
                b = Convert.ToByte(colorCodes[2]);
            }
            NetworkStream stream;
            StreamReader reader;
            Console.WriteLine("[IndigoIRC] Connecting to " + host + ":" + port + " on channel " + channel);
            //Console.Write(host);
            String inputLine;
            try
            {
                while (true)
                {
                    this.irc = new TcpClient(host, port);
                    stream = this.irc.GetStream();
                    reader = new StreamReader(stream);
                    writer = new StreamWriter(stream);
                    writer.WriteLine("NICK " + nick);
                    writer.Flush();
                    writer.WriteLine(user);
                    writer.Flush();
                    while ((inputLine = reader.ReadLine()) != null)
                    {
                        //Console.WriteLine("<-" + inputLine);

                        // Split the lines sent from the server by spaces. This seems the easiest way to parse them.
                        string[] splitInput = inputLine.Split(new Char[] { ' ' });

                        if (splitInput[0] == "PING")
                        {
                            string PongReply = splitInput[1];
                            //Console.WriteLine("->PONG " + PongReply);
                            writer.WriteLine("PONG " + PongReply);
                            writer.Flush();
                            //continue;
                        }
                        else if (splitInput[1].Equals("PRIVMSG"))
                        {
                            String message2 = splitInput[3];
                            //Console.WriteLine(message2 + "hi");
                            String playerList;
                            if (splitInput[3].Equals(":.list"))
                            {
                                //TSPlayer player = TShock.Players[args.Who];
                                
                                
                                if (Players.Count > 0)
                                {
                                   // Console.WriteLine(players[0].Name);

                                    playerList = "Online (" + Players.Count + "/" + TShock.Config.MaxSlots + "): ";
                                    for (int i = 0; i <= Players.Count - 1; i++)
                                    {
                                        //TSPlayer player = TShock.Players.
                                        int id = Players[i].Index;
                                        TSPlayer currentPlayer = TShock.Players[id];
                                        if (i == Players.Count - 1)
                                        {
                                            playerList = playerList + currentPlayer.Name;
                                        }
                                        else
                                        {
                                            playerList = playerList + currentPlayer.Name + ", ";
                                        }
                                    }
                                    //playerList = "Online (" + players.Length + "/8): ";
                                    //for (int i = 0; i <= players.Length - 1; i++)
                                    //{
                                    //    playerList = playerList + players[i].Name + ", ";
                                    //}
                                }
                                else
                                {
                                    //Console.WriteLine("Detected no one online");
                                    playerList = "Online (0/" + TShock.Config.MaxSlots + "): ";
                                }
                                writer.WriteLine("PRIVMSG " + channel + " :" + playerList);
                                
                                //writer.WriteLine("PRIVMSG " + channel + " :Online (3/8): XXXXXXXXXX, XXXXXXX, XXXX");
                                writer.Flush();
                            }
                            else if (splitInput[3].Equals(":" + commandPrefix + "uptime"))
                            {
                                //DateTime.Now.
                                TimeSpan currTime = DateTime.Now.TimeOfDay;
                                currTime = currTime + TimeSpan.FromDays(DateTime.Now.Day);
                                TimeSpan remainingTime = CalcTime(currTime);
                                int day = remainingTime.Days;
                                int hour = remainingTime.Hours;
                                int minute = remainingTime.Minutes;
                                int second = remainingTime.Seconds;
                                writer.WriteLine("PRIVMSG " + channel + " :Server uptime: " + day + " days " + hour + " hours " + minute + " minutes " + second + " seconds.");
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
                                int loc = splitInput[0].IndexOf("!");
                                //Console.WriteLine("Location of \"!~\": " + loc);
                                if (loc > 0)
                                {
                                    splitInput[0] = splitInput[0].Substring(0, loc);
                                    //splitInput[3] = ReplaceFirst(splitInput[3], ":", "");
                                    splitInput[0] = ingameFormatting.Replace("%NAME%", splitInput[0]);
                                    String message = String.Join(" ", splitInput);
                                    message = ReplaceFirst(message, "PRIVMSG", "");
                                    message = ReplaceFirst(message, channel, "");
                                    message = ReplaceFirst(message, "   :", "");
                                    
                                    //writer.WriteLine("PRIVMSG " + channel + " :Hello there");
                                    //writer.Flush();
                                    message = ReplaceFirst(message, ":", "");
                                    //message = ingameFormatting.Replace("%NAME%",  + message;
                                    Console.WriteLine(message);
                                    //Chat(color, message);
                                    TSPlayer.All.SendMessage(message, r, g, b);
                                }
                            }
                            

                        }
                        else if (splitInput[1].Equals("JOIN"))
                        {
                            int loc = splitInput[0].IndexOf("!");
                                //Console.WriteLine("Location of \"!~\": " + loc);
                                //Console.WriteLine(splitInput[0]);
                            if (loc > 0)
                            {
                                splitInput[0] = splitInput[0].Substring(0, loc);
                                splitInput[0] = ReplaceFirst(splitInput[0], ":", "");
                                Console.WriteLine(splitInput[0]);
                            }
                            //Console.WriteLine(nick);
                            if (!splitInput[0].Equals(nick))
                            {
                                Chat(Color.LightPink, "[IRC] " + splitInput[0] + " joined the channel.");
                            }
                        }
                        else if (splitInput[1].Equals("QUIT"))
                        {
                            int loc = splitInput[0].IndexOf("!");
                            //Console.WriteLine("Location of \"!~\": " + loc);
                            
                            if (loc > 0)
                            {
                                splitInput[0] = splitInput[0].Substring(0, loc);
                                splitInput[0] = ReplaceFirst(splitInput[0], ":", "");
                                //Console.WriteLine(splitInput[0]);
                            }
                            if (!splitInput[0].Equals(nick))
                            {

                                Chat(Color.LightPink, "[IRC] " + splitInput[0] + " left the channel.");
                            }
                        }
                        else
                        {
                            Console.WriteLine(inputLine);
                        }
                        switch (splitInput[1])
                        {
                            // This is the 'raw' number, put out by the server. Its the first one
                            // so I figured it'd be the best time to send the join command.
                            // I don't know if this is standard practice or not.
                            case "001":
                                string JoinString = "JOIN " + channel;
                                writer.WriteLine(JoinString);
                                writer.Flush();
                                break;
                            case "002":
                                if (!config.AppSettings.Settings["password"].Value.Equals(""))
                                {
                                    string authenticate = "PRIVMSG NickServ :IDENTIFY " + config.AppSettings.Settings["nick"].Value + " " + config.AppSettings.Settings["password"].Value;
                                    Console.WriteLine(authenticate);
                                    writer.WriteLine(authenticate);
                                    writer.Flush();
                                }
                                
                                break;
                            default:
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
                // Show the exception, sleep for a while and try to establish a new connection to irc server
                Console.WriteLine(e.ToString());
                //Thread.Sleep(5000);
                string[] argv = { };
                connect();
            }

        }

        protected override void Dispose(bool disposing)
        {
            /* Ensure that we are actually disposing.
             */
            if (disposing)
            {
                /* Using the .Deregister function, we remove our method
                 * from the hook.
                 */
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            }
            base.Dispose(disposing);
        }
        private void OnChat(ServerChatEventArgs args)
        {
            /* This checks to see if the chat has already been handled. If it has, the method will return
             * and the player's chat will not be managed by this plugin
             * Always make sure to check the chat to see if it has been handled
             */
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IIRC/IndigoIRC.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            String channel = config.AppSettings.Settings["channel"].Value;

            if (args.Handled)
            {
                return;
            }

            /* Get the player who sent this request.
             */
            TSPlayer player = TShock.Players[args.Who];

            /* If the player object is null we want to return to avoid NullReference Exceptions
             */
            if (player == null)
            {
                //args.Handled = true;
                return;
            }
            if (!args.Text.StartsWith("/"))
            {
                //args.Handled = true;
                string words = args.Text;
                words = config.AppSettings.Settings["ircFormatting"].Value + words;
                words = words.Replace("%NAME%", player.Name);

                writer.WriteLine("PRIVMSG " + channel + " :" + words);
                writer.Flush();
            }
        }
        private void Chat(Color color, string message)
        {
            /* Send all players the message in the color
             * specified.
             */
            TSPlayer.All.SendMessage(message, color);
            //TSPlayer.All.SendMessage(message, )
        }
        private void CreateConfig()
        {
            
            Console.WriteLine("[IndigoIRC] No config file found!");
            Console.WriteLine("[IndigoIRC] Generating config file from defaults.");
            Console.WriteLine("[IndigoIRC] Please go to IIRC/IndigoIRC.settings and change the settings. (REQUIRED)");
            Console.WriteLine("[IndigoIRC] This plugin will be activated upon the next reboot.");
            //File.Create("IIRC/IndigoIRC.settings");
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IIRC/IndigoIRC.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            config.AppSettings.Settings.Add("host", "irc.esper.net");
            config.AppSettings.Settings.Add("port", "6667");
            config.AppSettings.Settings.Add("nick", "IndigoIRC_Client");
            config.AppSettings.Settings.Add("name", "IIRCTerraria");
            config.AppSettings.Settings.Add("channel", "#examplechannel");
            config.AppSettings.Settings.Add("user", "USER IndigoIRCBot 0 * :IndigoIRC");
            config.AppSettings.Settings.Add("password", "");
            config.AppSettings.Settings.Add("ingameFormatting", "[IRC] %NAME%> ");
            config.AppSettings.Settings.Add("ingameColour", "255;117;117");
            config.AppSettings.Settings.Add("ircFormatting", "%NAME%> ");
            config.AppSettings.Settings.Add("commandPrefix", ".");
            config.Save(ConfigurationSaveMode.Full);
            return;
        }
        public void updateConfig()
        {
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IIRC/IndigoIRC.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            //config.AppSettings.Settings["host"]
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
                config.AppSettings.Settings.Add("nick", "IndigoIRC_Client");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("name"))
            {
                config.AppSettings.Settings.Add("name", "IIRCTerraria");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("channel"))
            {
                config.AppSettings.Settings.Add("channel", "#examplechannel");
            }
            if (!config.AppSettings.Settings.AllKeys.Contains("user"))
            {
                config.AppSettings.Settings.Add("user", "USER IndigoIRCBot 0 * :IndigoIRC");
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
            config.Save(ConfigurationSaveMode.Full);
        }
        public string ReplaceFirst(string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
        private void OnJoin(JoinEventArgs args)
        {
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IIRC/IndigoIRC.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            String channel = config.AppSettings.Settings["channel"].Value;

            TSPlayer player = TShock.Players[args.Who];

            String words = player.Name + " joined the game.";

            writer.WriteLine("PRIVMSG " + channel + " :" + words);
            writer.Flush();
            lock (Players)
                Players.Add(new Player(args.Who));
        }
        private void OnLeave(LeaveEventArgs args)
        {
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IIRC/IndigoIRC.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            String channel = config.AppSettings.Settings["channel"].Value;
            
            TSPlayer player = TShock.Players[args.Who];

            String words = player.Name + " left the game.";

            if (player == null)
            {
                //args.Handled = true;
                return;
            }

            writer.WriteLine("PRIVMSG " + channel + " :" + words);
            writer.Flush();
            lock (Players)
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    if (Players[i].Index == args.Who)
                    {
                        Players.RemoveAt(i);
                        break; //Found the player, break.
                    }
                }
            }
        }
        private void IIRC(CommandArgs args)
        {
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IIRC/IndigoIRC.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            String channel = config.AppSettings.Settings["channel"].Value;
            NetworkStream stream = this.irc.GetStream();
            StreamReader reader = new StreamReader(stream);
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
                    String message = ":" + String.Join(" ", args.Parameters);
                    message = ReplaceFirst(message, "say ", "");
                    writer.WriteLine("PRIVMSG " + channel + " " + message);
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
                    String irccommand = args.Parameters[1];
                    args.Parameters.RemoveAt(0);
                    args.Parameters.RemoveAt(0);
                    String message = String.Join(" ", args.Parameters);
                    //message = ReplaceFirst(message, "sendraw ", "");
                    //Console.WriteLine(irccommand + " " + message);
                    writer.WriteLine(irccommand + " " + message);
                    writer.Flush();
                    
                }
                else
                {
                    args.Player.SendErrorMessage("Not enough parameters. Do /irc sendraw <command>");
                }
            }
                
        }
        private TimeSpan CalcTime(TimeSpan currentTime)
        {
            double initSeconds = this.initTime.TotalSeconds;
            double finalSeconds = currentTime.TotalSeconds;
            double remainingSeconds = finalSeconds - initSeconds;
            //Console.WriteLine(finalSeconds + " - " + initSeconds + " = " + remainingSeconds);
            TimeSpan remainingTime = TimeSpan.FromSeconds(finalSeconds - initSeconds);


            return remainingTime;
        }
    }

    public class Player
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        //Add other variables here - MAKE SURE YOU DON'T MAKE THEM STATIC

        public Player(int index)
        {
            Index = index;
        }
    }
}
