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
        NetworkStream stream;
        StreamReader reader;
        private String host;
        private int port;
        private String channel;
        private String name;
        private String nick;


        private TcpClient irc;

        public override Version Version
        {
            get { return new Version("1.5.3"); }
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



            if (!File.Exists("IIRC/IndigoIRC.settings"))
            {
                CreateConfig();
            }

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
            NetworkStream stream;
            StreamReader reader;
            Console.WriteLine("[IndigoIRC] Connecting to " + host + ":" + port + " on channel " + channel);
            //Console.Write(host);
            String inputLine;
            try
            {
                while (true)
                {
                    irc = new TcpClient(host, port);
                    stream = irc.GetStream();
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
                                TSPlayer[] players = TShock.Players;
                                
                                if (players[0] != null)
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
                                    playerList = "Online (0/" + TShock.Config.MaxSlots + "): ";
                                }
                                writer.WriteLine("PRIVMSG " + channel + " :" + playerList);
                                
                                //writer.WriteLine("PRIVMSG " + channel + " :Online (3/8): XXXXXXXXXX, XXXXXXX, XXXX");
                                writer.Flush();
                            }
                            else if (splitInput[3].Equals(":.version"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :IndigoIRC is running on version: " + Version);
                                writer.Flush();
                            }
                            else if (splitInput[3].Equals(":.help"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :Valid commands: help, list, version");
                                writer.Flush();
                            }
                            else if (splitInput[0].Contains("!~"))
                            {
                                int loc = splitInput[0].IndexOf("!~");
                                Console.WriteLine("Location of \"!~\": " + loc);
                                if (loc > 0)
                                {
                                    splitInput[0] = splitInput[0].Substring(0, loc);
                                    String message = String.Join(" ", splitInput);
                                    message = ReplaceFirst(message, "PRIVMSG", "");
                                    message = ReplaceFirst(message, channel, "");
                                    message = ReplaceFirst(message, "   :", "> ");
                                    Console.WriteLine(message);
                                    //writer.WriteLine("PRIVMSG " + channel + " :Hello there");
                                    //writer.Flush();
                                    message = ReplaceFirst(message, ":", "");
                                    message = "[IRC] " + message;
                                    Chat(Color.LightPink, message);
                                }
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
                words = player.Name + "> " + words;

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
        }
        private void CreateConfig()
        {
            //File.Create("IIRC/IndigoIRC.settings");
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IIRC/IndigoIRC.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            config.AppSettings.Settings.Add("host", "irc.esper.net");
            config.AppSettings.Settings.Add("port", "6667");
            config.AppSettings.Settings.Add("nick", "IndigoIRC_Client");
            config.AppSettings.Settings.Add("name", "ITCTerraria");
            config.AppSettings.Settings.Add("channel", "#examplechannel");
            config.AppSettings.Settings.Add("user", "USER IndigoIRCBot 0 * :IndigoIRC");
            config.Save(ConfigurationSaveMode.Full);
            return;
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
