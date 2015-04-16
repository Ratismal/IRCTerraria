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

namespace IRCTerraria
{
    [ApiVersion(1, 17)]
    public class IRCTerraria : TerrariaPlugin
    {
        private int chatIndex;

        //private String host = ConfigurationManager.AppSettings["host"];
        //private int port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]);
        //private String pass = null;
        //private String nick = ConfigurationManager.AppSettings["nick"];
        //private String user = "HUTerraria";
        //private String name = ConfigurationManager.AppSettings["name"];
        //private String channel = ConfigurationManager.AppSettings["channel"];
        //private bool ssl = false;
        /*
        private String host = IRCTerrariaConfigs.Default.host;
        private int port = IRCTerrariaConfigs.Default.port;
        private String nick = IRCTerrariaConfigs.Default.nick;
        private String name = IRCTerrariaConfigs.Default.name;
        private String channel = IRCTerrariaConfigs.Default.channel;
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
            get { return new Version("1.1.0"); }
        }
        public override string Name
        {
            get { return "IRCTerraria"; }
        }
        public override string Author
        {
            get { return "Ratismal"; }
        }
        public override string Description
        {
            get { return "IRC in Terraria"; }
        }
        public IRCTerraria(Main game)
            : base(game)
        {
            Order = 0;

            //chatIndex = 0;
        }
        public override void Initialize()
        {
            Console.Write("Initializing IRCTerraria\n");



            if (!File.Exists("IRCTerrariaConfigs.settings"))
            {
                CreateConfig();
            }

            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IRCTerrariaConfigs.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);

            ServerApi.Hooks.ServerChat.Register(this, OnChat);

            System.Threading.Thread myThread;
            myThread = new Thread(new ThreadStart(connect));

            myThread.IsBackground = true;
            Console.WriteLine("[IRCTerraria] Starting IRC thread");

            myThread.Start();

        }
        private void connect()
        {
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IRCTerrariaConfigs.settings";
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
            Console.WriteLine("[IRCTerraria] Connecting to " + host + ":" + port + " on channel " + channel);
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
                            continue;
                        }
                        if (splitInput[1].Equals("PRIVMSG"))
                        {
                            String message2 = splitInput[3];
                            //Console.WriteLine(message2 + "hi");
                            String playerList;
                            if (splitInput[3].Equals(":.list"))
                            {
                                
                                TSPlayer[] players = TShock.Players;
                                if (players[0] != null)
                                {
                                    playerList = "Online (" + players.Length + "/8): ";
                                    for (int i = 0; i <= players.Length; i++)
                                    {
                                        playerList = playerList + players[i].Name;
                                    }
                                }
                                else
                                {
                                    playerList = "Online (0/8): ";
                                }
                                writer.WriteLine("PRIVMSG " + channel + " :" + playerList);
                                
                                //writer.WriteLine("PRIVMSG " + channel + " :Online (3/8): XXXXXXXXXX, XXXXXXX, XXXX");
                                writer.Flush();
                            }
                            else if (splitInput[3].Equals(":.version"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :IRCTerraria is running on version: " + Version);
                                writer.Flush();
                            }
                            else if (splitInput[3].Equals(":.help"))
                            {
                                writer.WriteLine("PRIVMSG " + channel + " :Valid commands: help, list, version");
                                writer.Flush();
                            }
                            else
                            {
                                int loc = splitInput[0].IndexOf("!~");
                                splitInput[0] = splitInput[0].Substring(0, loc);
                                String message = String.Join(" ", splitInput);
                                message = ReplaceFirst(message, "PRIVMSG", "");
                                message = ReplaceFirst(message, channel, "");
                                message = ReplaceFirst(message, "   :", "> ");
                                Console.WriteLine(message);
                                //writer.WriteLine("PRIVMSG " + channel + " :Hello there");
                                writer.Flush();
                                Chat(Color.LightPink, message);
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
            }
            base.Dispose(disposing);
        }
        private void OnChat(ServerChatEventArgs args)
        {
            /* This checks to see if the chat has already been handled. If it has, the method will return
             * and the player's chat will not be managed by this plugin
             * Always make sure to check the chat to see if it has been handled
             */
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
                args.Handled = true;
                return;
            }
            if (!args.Text.StartsWith("/"))
            {
                //args.Handled = true;
                string words = args.Text;
                words = player.Name + "> " + words;

                writer.WriteLine("PRIVMSG " + channel + " :what is going on pls halp");
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
            //File.Create("IRCTerrariaConfigs.settings");
            ExeConfigurationFileMap configMap = new ExeConfigurationFileMap();
            configMap.ExeConfigFilename = "IRCTerrariaConfigs.settings";
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(configMap, ConfigurationUserLevel.None);
            config.AppSettings.Settings.Add("host", "irc.esper.net");
            config.AppSettings.Settings.Add("port", "6667");
            config.AppSettings.Settings.Add("nick", "IRCTerraria_Client");
            config.AppSettings.Settings.Add("name", "ITCTerraria");
            config.AppSettings.Settings.Add("channel", "#examplechannel");
            config.AppSettings.Settings.Add("user", "USER IRCTerrariaBot 0 * :IRCTerraria");
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
    }

}
