using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Spike.Network;
using Spike.Collections;
using Spike;
using MyChat.Properties;

namespace MyChat
{
    public static class MyChatImpl
    {
        /// <summary>
        /// A public static function, when decorated with InvokeAt attribute will be
        /// invoked automatically by Spike Engine. This particular one will be invoked
        /// when the server is initializing (only once).
        /// </summary>
        [InvokeAt(InvokeAtType.Initialize)]
        public static void Initialize()
        {
            // First we need to hook the events while the server is initializing, 
            // giving us the ability to listen on those events.
            MyChatProtocol.JoinMyChat += new RequestHandler(OnJoinChat);
            MyChatProtocol.SendMyChatMessage += new RequestHandler<SendMyChatMessageRequest>(OnSendMessage);

            // Since we will add the clients in the chat, we need to remove them from
            // the chat room once they are disconnected. 
            Service.ClientDisconnect += new ClientDisconnectEventHandler(OnClientDisconnect);
        }


        /// <summary>
        /// A list of all clients currently in the chat. This is a concurrent list that 
        /// helps us to avoid most of the concurrency issues as many clients could be 
        /// added/removed to the list simultaneously.
        /// </summary>
        public static ConcurrentList<IClient> PeopleInChat = new ConcurrentList<IClient>();


        /// <summary>
        /// Static method that is ivoked when a client sends a message to the server.
        /// </summary>
        static void OnSendMessage(IClient client, SendMyChatMessageRequest packet)
        {
            // Validate the message
            var message = packet.Message;
            if (message == null || message == String.Empty || message.Length > 120)
                return;

            // We loop through all people in the chat and we broadcast them
            // the incoming message.
            foreach (var person in PeopleInChat)
            {
                // Send the message now
                person.SendMyChatMessagesInform(
                    (byte[])client["Avatar"], // The avatar of the client who sends the message
                    message            // The message to be sent
                    );
                
            }
        }

        /// <summary>
        /// Static method that is ivoked when a client decides to join the chat.
        /// </summary>
        static void OnJoinChat(IClient client)
        {
            // We add the person to the chat room
            if (!PeopleInChat.Contains(client))
            {
                // Generate the avatar for the client
                client["Avatar"] = IdenticonRenderer.Create(client);

                // Add the person in the room
                PeopleInChat.Add(client);

                // Say something nice
                SayTo(client, "Hi, I'm the author of this sample, hope you like it! Just type a message below and see how it works on CodeProject. éçèâï");
            }
        }

        /// <summary>
        /// Static method that is invoked when a client is disconnected.
        /// </summary>
        static void OnClientDisconnect(ClientDisconnectEventArgs e)
        {
            // Remove the client from the list if he's 
            // disconnected from the chat
            if (PeopleInChat.Contains(e.Client))
                PeopleInChat.Remove(e.Client);
        }

        /// <summary>
        /// Author says something to a particular client in the chat room.
        /// </summary>
        static void SayTo(IClient client, string message)
        {
            client.SendMyChatMessagesInform(Resources.Author, message);
        }

    }
}
