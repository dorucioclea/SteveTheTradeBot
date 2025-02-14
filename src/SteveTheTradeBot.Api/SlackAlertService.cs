﻿using System;
using System.Threading;
using System.Threading.Tasks;
using SteveTheTradeBot.Core;
using SteveTheTradeBot.Core.Components.Notifications;
using SteveTheTradeBot.Core.Components.SlackResponders;
using SteveTheTradeBot.Core.Components.Strategies;
using SteveTheTradeBot.Core.Framework.MessageUtil;
using SteveTheTradeBot.Core.Framework.Settings;
using SteveTheTradeBot.Core.Framework.Slack;
using SteveTheTradeBot.Core.Utils;

namespace SteveTheTradeBot.Api
{
    public class SlackAlertService : BackgroundService
    {
        private readonly IMessenger _messenger;
        private readonly MessageToNotification _messageToNotification;
        private readonly INotificationChannel _notificationChannel;
        private readonly SlackService _slackService;
        

        public SlackAlertService(IResponseBuilder responseBuilder , IMessenger messenger , MessageToNotification messageToNotification , INotificationChannel notificationChannel)
        {
            _messenger = messenger;
            _messageToNotification = messageToNotification;
            _notificationChannel = notificationChannel;
            _slackService = new SlackService(Settings.Instance.SlackBotKey, responseBuilder);
            
        }

        #region Implementation of IHostedService

        public override async Task ExecuteAsync(CancellationToken token)
        {
            await _notificationChannel.PostAsync($"{SlackHelper.GetGreeting()}, Im awake and up and running v{ConfigurationBuilderHelper.InformationalVersion()}-{ConfigurationBuilderHelper.GetEnvironment().ToLower()} on {Environment.MachineName}.");
            await _slackService.Connect();
            MessengerHelper.RegisterAsync<TradeOrderMadeMessage>(_messenger,this, _messageToNotification.OnTradeOrderMade);
            MessengerHelper.RegisterAsync<PostSlackMessage>(_messenger,this,(r) => _notificationChannel.PostAsync(r.Message));
        }

        #endregion
    }
}