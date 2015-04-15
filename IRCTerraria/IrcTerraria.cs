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
    [ApiVersion(1, 16)]
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
            get { return new Version("1.0.6"); }
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
            Order = 1;

            chatIndex = 0;
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
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            connect(config);
        }
        private void connect(Configuration config)
        {
            String host = config.AppSettings.Settings["host"].Value;
            int port = Convert.ToInt32(config.AppSettings.Settings["port"].Value);
            String nick = config.AppSettings.Settings["nick"].Value;
            String name = config.AppSettings.Settings["name"].Value;
            String channel = config.AppSettings.Settings["channel"].Value;
            NetworkStream stream;
            StreamReader reader;

            try
            {
                irc = new TcpClient(host, port);
                stream = irc.GetStream();
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);
                writer.WriteLine("USER " + name + " 0 * :IRCTerraria Bot");
                writer.Flush();
                writer.WriteLine("NICK " + nick);
                writer.Flush();
                writer.WriteLine("JOIN " + channel);
                writer.Flush();
                while (true)
                {
                    getInput();
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
                Thread.Sleep(5000);
                string[] argv = { };
                connect(config);
            }
        }

        private void getInput()
        {
            String inputLine;
            while ((inputLine = reader.ReadLine () ) != null)
            {
                string message = reader.ReadLine();
                if (message.Equals(".list"))
                {
                    TSPlayer[] players = TShock.Players;
                    string playerList = "Online (" + players.Length + "/8): ";
                    for (int i = 0; i <= players.Length; i++)
                    {
                        playerList = playerList + players[i].Name;
                    }
                    writer.WriteLine(playerList);
                    writer.Flush();
                }
                else if (message.Equals(".version"))
                {
                    writer.WriteLine("IRCTerraria is running on version: " + Version);
                    writer.Flush();
                }
                else if (message.Equals(".help"))
                {
                    writer.WriteLine("Valid commands: help, list, version");
                    writer.Flush();
                }
                else
                {
                    Chat(Color.LightPink, message);
                }


            }
        }
        private void OnLeave(LeaveEventArgs args)
        {

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
                args.Handled = true;
                string words = args.Text;
                words = player.Name + "> " + words;

                writer.WriteLine(words);
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
            config.AppSettings.Settings.Add("channel", "#ExampleChannel");
            config.Save(ConfigurationSaveMode.Full);
            return;
        }
    }
}
