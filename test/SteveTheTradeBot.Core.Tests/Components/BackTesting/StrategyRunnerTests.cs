﻿using System;
using System.Linq;
using System.Threading.Tasks;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Skender.Stock.Indicators;
using SteveTheTradeBot.Core.Components.BackTesting;
using SteveTheTradeBot.Core.Components.Storage;
using SteveTheTradeBot.Core.Components.Strategies;
using SteveTheTradeBot.Core.Framework.MessageUtil;
using SteveTheTradeBot.Core.Tests.Components.Storage;
using SteveTheTradeBot.Core.Tests.Components.Strategies;
using SteveTheTradeBot.Core.Utils;
using SteveTheTradeBot.Dal.Models.Trades;
using SteveTheTradeBot.Dal.Tests;

namespace SteveTheTradeBot.Core.Tests.Components.BackTesting
{
    public class StrategyRunnerTests
    {
        private StrategyRunner _strategyRunner;
        private Mock<IDynamicGraphs> _mockIDynamicGraphs;
        private Mock<ITradeQuoteStore> _mockITradeHistoryStore;
        private FakeStrategy _fakeStrategy;
        private StrategyInstanceStore _strategyInstanceStore;
        private Mock<IParameterStore> _mockIParameterStore;


        #region Setup/Teardown

        public void Setup()
        {
            _mockIDynamicGraphs = new Mock<IDynamicGraphs>();
            _strategyInstanceStore = new StrategyInstanceStore(TestTradePersistenceFactory.UniqueDb());
            _mockITradeHistoryStore = new Mock<ITradeQuoteStore>();
            _mockIParameterStore = new Mock<IParameterStore>();
            
            var fakeBroker = new FakeBroker(Messenger.Default);
            _fakeStrategy = new FakeStrategy();
            var strategyPicker = new StrategyPicker().Add("FakeStrategy", () => _fakeStrategy);
            
            _strategyRunner = new StrategyRunner(strategyPicker, _mockIDynamicGraphs.Object, _strategyInstanceStore, fakeBroker, _mockITradeHistoryStore.Object, Messenger.Default, _mockIParameterStore.Object);
        }

        [TearDown]
        public void TearDown()
        {

        }

        #endregion

        [Test]
        [TestCase(PeriodSize.FiveMinutes, "2021-07-23T01:00:00", true)]
        [TestCase(PeriodSize.FiveMinutes, "2021-07-23T01:01:00", false)]
        [TestCase(PeriodSize.FiveMinutes, "2021-07-23T01:05:00", true)]
        [TestCase(PeriodSize.FiveMinutes, "2021-07-23T01:06:00", false)]
        [TestCase(PeriodSize.OneMinute, "2021-07-23T01:06:00", true)]
        [TestCase(PeriodSize.FifteenMinutes, "2021-07-23T01:14:00", false)]
        [TestCase(PeriodSize.FifteenMinutes, "2021-07-23T01:15:00", true)]
        [TestCase(PeriodSize.FifteenMinutes, "2021-07-23T01:00:00", true)]
        [TestCase(PeriodSize.OneHour, "2021-07-23T01:00:02", true)]
        [TestCase(PeriodSize.OneHour, "2021-07-23T01:01:00", false)]
        [TestCase(PeriodSize.FourHours, "2021-07-23T01:00:00", false)]
        [TestCase(PeriodSize.FourHours, "2021-07-23T00:00:00", true)]
        [TestCase(PeriodSize.FourHours, "2021-07-23T04:00:00", true)]
        [TestCase(PeriodSize.Day, "2021-07-23T04:00:00", false)]
        [TestCase(PeriodSize.Day, "2021-07-23T00:00:00", true)]
        [TestCase(PeriodSize.Week, "2021-07-25T00:00:00", true)]
        public void Process_GivenBackTest_ShouldThrowException(PeriodSize period,string date,bool expected)
        {
            // arrange
            Setup();
            // action
            var result = _strategyRunner.IsCorrectTime(period, DateTime.Parse(date));
            // assert
            result.Should().Be(expected);
        }

        [Test]
        public void Process_GivenBackTest_ShouldThrowException()
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.ForBackTest("FakeStrategy", CurrencyPair.BTCZAR);
            // action
            Action testCall = () => { _strategyRunner.Process(forBackTest,DateTime.Now).Wait(); };
            // assert
            testCall.Should().Throw<ArgumentException>().WithMessage("Cannot process back test strategy!");
        }
        
        
        [Test]
        public void Process_GivenInActiveStrategy_ShouldThrowException()
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.From("FakeStrategy", CurrencyPair.BTCZAR,100, PeriodSize.FiveMinutes);
            forBackTest.IsActive = false;
            // action
            Action testCall = () => { _strategyRunner.Process(forBackTest,DateTime.Now).Wait(); };
            // assert
            testCall.Should().Throw<ArgumentException>().WithMessage("Cannot process strategy that is marked as inactive!");
        }

        [Test]
        public async Task Process_GivenStrategyThatHasAlreadyRun_ShouldDoNothing()
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.From("FakeStrategy", CurrencyPair.BTCZAR, 100, PeriodSize.FiveMinutes);
            forBackTest.LastDate = DateTime.Parse("2021-07-23T01:01:00");
            // action
            await _strategyRunner.Process(forBackTest, DateTime.Parse("2021-07-23T01:01:10"));
            // assert
            _mockITradeHistoryStore.Verify(mc => mc.FindRecent(PeriodSize.FiveMinutes, It.IsAny<DateTime>(), 500, CurrencyPair.BTCZAR, forBackTest.Feed), Times.Never);
        }


        [Test]
        public async Task Process_GivenCorrectTime_ShouldLoadTheRecentQuotes() 
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.From("FakeStrategy", CurrencyPair.BTCZAR, 100, PeriodSize.FiveMinutes);
            var beforeDate = DateTime.Parse("2021-07-23T00:00:00");
            SetupContext(beforeDate, forBackTest);
            // action
            await _strategyRunner.Process(forBackTest, beforeDate);
            // assert
            _mockITradeHistoryStore.Verify(mc => mc.FindRecent(PeriodSize.FiveMinutes, beforeDate.ToUniversalTime(), 500, CurrencyPair.BTCZAR, forBackTest.Feed),Times.Once);
        }

        [Test]
        public async Task Process_GivenCorrectTime_ShouldCallStrategyWithContext() 
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.From("FakeStrategy", CurrencyPair.BTCZAR, 100, PeriodSize.FiveMinutes);
            var beforeDate = DateTime.Parse("2021-07-23T00:00:00");
            SetupContext(beforeDate, forBackTest);
            // action
            await _strategyRunner.Process(forBackTest, beforeDate);
            // assert
            _fakeStrategy.DateRecievedValues.Should().HaveCount(1);
            _fakeStrategy.DateRecievedValues[0].StrategyInstance.Id.Should().Be(forBackTest.Id);
            _fakeStrategy.DateRecievedValues[0].Quotes.Should().HaveCount(100);
        }

        [Test]
        public async Task Process_GivenDynamicPlotValues_ShouldFlushValues() 
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.From("FakeStrategy", CurrencyPair.BTCZAR, 100, PeriodSize.FiveMinutes);
            var beforeDate = DateTime.Parse("2021-07-23T00:00:00");
            SetupContext(beforeDate, forBackTest);
            var dateTime = DateTime.Now;
            // action
            await _strategyRunner.Process(forBackTest, beforeDate);
            await _fakeStrategy.DateRecievedValues.First().PlotRunData(dateTime, "test", 123);
            // assert
            _mockIDynamicGraphs.Verify(x=>x.Plot(forBackTest.Reference, dateTime, "test", 123),Times.Once);
            _mockIDynamicGraphs.Verify(x=>x.Flush(),Times.Once);
        }

        [Test]
        public async Task Process_GivenSomeTrades_ShouldCalculateValues() 
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.From("FakeStrategy", CurrencyPair.BTCZAR, 100, PeriodSize.FiveMinutes);
            var beforeDate = DateTime.Parse("2021-07-23T00:00:00");
            SetupContext(beforeDate, forBackTest);
            // action
            await _strategyRunner.Process(forBackTest, beforeDate);
            var strategyContext = _fakeStrategy.DateRecievedValues.First();
            await _fakeStrategy.Buy(strategyContext,100);
            await _strategyRunner.PostTransaction(strategyContext.StrategyInstance);
            // assert
            strategyContext.StrategyInstance.Print();
            strategyContext.StrategyInstance.TotalActiveTrades.Should().Be(1);
        }

        [Test]
        public async Task Process_GivenSomeTrades_ShouldStoreTrades()   
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.From("FakeStrategy", CurrencyPair.BTCZAR, 100, PeriodSize.FiveMinutes);
            var beforeDate = DateTime.Parse("2021-07-23T00:00:00");
            SetupContext(beforeDate, forBackTest);
            // action
            await _strategyRunner.Process(forBackTest, beforeDate);
            // assert
            var findById = await _strategyInstanceStore.FindById(forBackTest.Id);
            findById.Should().HaveCount(1);
        }


        [Test]
        public async Task Process_GivenStaleData_ShouldSkipTrade()
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.From("FakeStrategy", CurrencyPair.BTCZAR, 100, PeriodSize.FiveMinutes);
            var beforeDate = DateTime.Parse("2021-07-23T00:00:00");
            SetupContext(beforeDate, forBackTest);
            var first = await _strategyRunner.Process(forBackTest, beforeDate);
            // action
            var process = await _strategyRunner.Process(forBackTest, beforeDate);
            // assert
            first.Should().BeTrue();
            process.Should().BeFalse();
        }


        [Test]
        public void Process_GivenDataThatIsMissing_ShouldThrowException()
        {
            // arrange
            Setup();
            var forBackTest = StrategyInstance.From("FakeStrategy", CurrencyPair.BTCZAR, 100, PeriodSize.FiveMinutes);
            var beforeDate = DateTime.Parse("2021-07-23T00:00:00");
            SetupContext(beforeDate, forBackTest,false);

            // action
            Action testCall = () => { _strategyRunner.Process(forBackTest, beforeDate).Wait(); };
            testCall.Should().Throw<Exception>().WithMessage("Missing Quotes data!");
        }


        #region Private Methods

        private void SetupContext(DateTime beforeDate, StrategyInstance forBackTest, bool addMetric = true)
        {
            var tradeFeedQuotes = Builder<TradeQuote>.CreateListOfSize(100).WithValidData().Build().ToList();
            if (!addMetric)
                tradeFeedQuotes.ForEach(x=>x.Metric.Clear());
            _strategyInstanceStore.Add(forBackTest).Wait();
            _mockITradeHistoryStore.Setup(mc => mc.FindRecent(PeriodSize.FiveMinutes, beforeDate.ToUniversalTime(), 500, CurrencyPair.BTCZAR, forBackTest.Feed))
                .ReturnsAsync(tradeFeedQuotes);
            _mockITradeHistoryStore.Setup(mc => mc.FindRecent(PeriodSize.Day, beforeDate.ToUniversalTime(), It.IsAny<int>(), CurrencyPair.BTCZAR, forBackTest.Feed))
                .ReturnsAsync(tradeFeedQuotes);
        }

        #endregion
    }
}