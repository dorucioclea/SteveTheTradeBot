﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bumbershoot.Utilities.Helpers;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Skender.Stock.Indicators;
using SteveTheTradeBot.Core.Components.Broker;
using SteveTheTradeBot.Core.Components.Storage;
using SteveTheTradeBot.Core.Framework.MessageUtil;
using SteveTheTradeBot.Core.Tests.Components.Storage;
using SteveTheTradeBot.Dal.Models.Trades;
using SteveTheTradeBot.Dal.Tests;

namespace SteveTheTradeBot.Api.Tests
{
    public class PopulateOneMinuteCandleServiceTests
    {
        private PopulateOneMinuteQuoteService _service;
        private Mock<IHistoricalDataPlayer> _mockIHistoricalDataPlayer;
        private ITradePersistenceFactory _factory;

        [Test]
        public async Task Populate_GivenNoExistingRecords_ShouldLoadAllQuotes()  
        {
            // arrange
            Setup();
            CancellationToken cancellationToken = default;
            var historicalTrades = Builder<HistoricalTrade>.CreateListOfSize(2).WithValidData().Build();
            var context = await _factory.GetTradePersistence();
            context.HistoricalTrades.AddRange(historicalTrades);
            context.SaveChanges();
            // action
            await _service.Populate(cancellationToken, CurrencyPair.BTCZAR, "valr");
            // assert
            _mockIHistoricalDataPlayer.VerifyAll();
            var tradeFeedQuotes = _factory.GetTradePersistence().Result.TradeQuotes.AsQueryable().ToList();
            tradeFeedQuotes.Should().HaveCount(2);
        }

        [Test]
        public async Task Populate_GivenSomeExistingRecords_ShouldSaveAllQuotes()      
        {
            // arrange
            Setup();
            CancellationToken cancellationToken = default;
            var historicalTrades = Builder<HistoricalTrade>.CreateListOfSize(5).WithValidData().Build().Reverse().ToArray();
            var context = await _factory.GetTradePersistence();
            context.HistoricalTrades.AddRange(historicalTrades);
            context.SaveChanges();

            context.TradeQuotes.AddRange(historicalTrades.Take(2)
                .ToCandleOneMinute()
                .Select(x => TradeQuote.From(x, "valr", PeriodSize.OneMinute, CurrencyPair.BTCZAR)));
            context.SaveChanges();
            
            // action
            await _service.Populate(cancellationToken, CurrencyPair.BTCZAR, "valr");
            // assert
            _mockIHistoricalDataPlayer.VerifyAll();
            var tradeFeedQuotes = context.TradeQuotes.AsQueryable().ToList();
            tradeFeedQuotes.Should().HaveCount(5);
        }

        private void Setup()
        {
            _mockIHistoricalDataPlayer = new Mock<IHistoricalDataPlayer>();
            _factory = TestTradePersistenceFactory.UniqueDb();
            var tradeHistoryStore = new TradeHistoryStore(_factory);
            _service = new PopulateOneMinuteQuoteService(_factory, new HistoricalDataPlayer(tradeHistoryStore, new TradeQuoteStore(_factory)),new Messenger());
        }
    }
}