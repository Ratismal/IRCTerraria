using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;
using System.Configuration;

using TShockAPI;
using TShockAPI.Extensions;

using Terraria;
using TerrariaApi;
using TerrariaApi.Server;

namespace IRCTerraria
{
    [ApiVersion(1, 14)]
    public class IRCTerraria : TerrariaPlugin
    {
        private int chatIndex;

        private String host = ConfigurationManager.AppSettings["host"];
        private int port = Int32.Parse(ConfigurationManager.AppSettings["port"]);
        //private String pass = null;
        private String nick = ConfigurationManager.AppSettings["nick"];
        //private String user = "HUTerraria";
        private String name = ConfigurationManager.AppSettings["name"];
        private String channel = ConfigurationManager.AppSettings["channel"];
        //private bool ssl = false;

        private TcpClient irc;

        public override Version Version
        {
            get { return new Version("1.0.1"); }
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
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);

            this.irc = new TcpClient(this.host, this.port);

            using (NetworkStream stream = irc.GetStream())
            {
              
                using (StreamReader sr = new StreamReader(stream))
                {
                    
                    using (StreamWriter sw = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true })
                    {
                        sw.WriteLine("NICK " + this.nick);
                        sw.WriteLine("USER " + this.nick + "0 * :" + this.name);
                        sw.WriteLine("JOIN " + this.channel);
                    }
                    
                }
            }
            
        }
        private void getInput()
        {
            while (true)
            {
                using (NetworkStream stream = irc.GetStream())
                {
                    if (stream.Length != 0)
                    {
                        using (StreamReader sr = new StreamReader(stream))
                        {
                            string message = sr.ReadLine();
                            if (message.Equals(".list"))
                            {
                                TSPlayer[] players = TShock.Players;
                                string playerList = "Online (" + players.Length + "/8): ";
                                for (int i = 0; i <= players.Length; i++)
                                {
                                    playerList = playerList + players[i].Name;
                                }
                                using (StreamWriter sw = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true })
                                {
                                    sw.WriteLine(playerList);
                                }
                            }
                            else if (message.Equals(".version"))
                            {
                                using (StreamWriter sw = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true })
                                {
                                    sw.WriteLine("IRCTerraria is running on version: " + Version);
                                }
                            }
                            else if (message.Equals(".help"))
                            {
                                using (StreamWriter sw = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true })
                                {
                                    sw.WriteLine("Valid commands: help, list, version");
                                }
                            }
                            else
                            {
                                Chat(Color.LightPink, message);
                            }
                            
                        }
                    }
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

                using (NetworkStream stream = irc.GetStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        using (StreamWriter sw = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true })
                        {
                            sw.WriteLine(words);
                        }
                    }
                }
            }
        }
        private void Chat(Color color, string message)
        {
            /* Send all players the message in the color
             * specified.
             */
            TSPlayer.All.SendMessage(message, color);
        }
    }
}
