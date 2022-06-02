﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Geek.Server
{

    public class TcpConnectionHandler : ConnectionHandler
    {

        static readonly NLog.Logger LOGGER = NLog.LogManager.GetCurrentClassLogger();

        private readonly ILogger _logger;

        public TcpConnectionHandler(ILogger<TcpConnectionHandler> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            OnConnection(connection);
            NetChannel channel = new NetChannel(connection);
            while (true)
            {
                try
                {
                    var result = await channel.Reader.ReadAsync(channel.Protocol);
                    var message = result.Message;

                    _logger.LogInformation("Received a message of {Length} bytes", message.Payload.Length);

                    //分发消息
                    _ = Dispatcher(channel, MsgDecoder.Decode(connection, message));

                    if (result.IsCompleted)
                        break;
                }
                finally
                {
                    channel.Reader.Advance();
                }
            }
            OnDisconnection(channel);
        }

        protected void OnConnection(ConnectionContext connection)
        {
            _logger.LogInformation("{ConnectionId} connected", connection.ConnectionId);
        }

        protected void OnDisconnection(NetChannel channel)
        {
            _logger.LogInformation("{ConnectionId} disconnected", channel.Context.ConnectionId);
            var sessionId = channel.GetSessionId();
            if (sessionId > 0)
                SessionManager.Remove(sessionId);
        }

        protected async Task Dispatcher(NetChannel channel, IMessage msg)
        {
            if (msg == null)
                return;
            try
            {
                var handler = TcpHandlerFactory.GetHandler(msg.MsgId);
                LOGGER.Debug($"-------------get msg {msg.MsgId} {msg.GetType()}");

                if (handler == null)
                {
                    LOGGER.Error("找不到对应的handler " + msg.MsgId);
                    return;
                }

                //握手
                //var session = ctx.Channel.GetAttribute(SessionManager.SESSION).Get();
                long sessionId = SessionManager.GetSessionId(channel);
                if (sessionId > 0)
                    EventDispatcher.DispatchEvent(sessionId, (int)InnerEventID.OnMsgReceived);

                handler.Time = DateTime.Now;
                handler.Channel = channel;
                handler.Msg = msg;
                if (handler is TcpCompHandler compHandler)
                {
                    var entityId = await compHandler.GetEntityId();
                    if (entityId != 0)
                    {
                        var agent = await EntityMgr.GetCompAgent(entityId, compHandler.CompAgentType);
                        if (agent != null)
                            _ = agent.Owner.Actor.SendAsync(compHandler.ActionAsync);
                        else
                            LOGGER.Error($"handler actor 为空 {msg.MsgId} {handler.GetType()}");
                    }
                    else
                    {
                        LOGGER.Error($"EntityId 为0 {msg.MsgId} {handler.GetType()} {sessionId}");
                    }
                }
                else
                {
                    await handler.ActionAsync();
                }
            }
            catch (Exception e)
            {
                LOGGER.Error(e.ToString());
            }
        }


    }
}