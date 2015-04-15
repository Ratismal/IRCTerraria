using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Net.Sockets;
using System.IO;

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

        private String host = "irc.esper.net";
        private int port = 6667;
        private String pass = null;
        private String nick = "HU_Terraria";
        private String user = "HUTerraria";
        private String name = "IRCTerraria";
        private String channel = "#hysteriaunleashed";
        private bool ssl = false;

        private TcpClient irc;


        public override void Initialize()
        {
        }
        public override Version Version
        {
            get { return new Version("1.0"); }
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
                            string[] words = message.Split();
                            Chat(Color.LightPink, words);
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
                string[] words = args.Text.Split();

                string[] nWords = new string[words.Length + 1];
                nWords[0] = player.Name + ">";
                Array.Copy(words, 0, nWords, 1, words.Length);
                using (NetworkStream stream = irc.GetStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        using (StreamWriter sw = new StreamWriter(stream) { NewLine = "\r\n", AutoFlush = true })
                        {
                            sw.WriteLine(nWords);
                        }
                    }
                }
            }
        }
        private void Chat(Color color, string[] words)
        {
            /* Put the string back together, seperating
             * each word with a space.
             */
            String message = String.Join(" ", words);

            /* Send all players the message in the color
             * specified.
             */
            TSPlayer.All.SendMessage(message, color);
        }
    }
}
