﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Serilog;
using Skender.Stock.Indicators;
using SteveTheTradeBot.Core.Components.Storage;
using SteveTheTradeBot.Dal.Models.Trades;

namespace SteveTheTradeBot.Core.Components.Broker
{
   

    public class HistoricalDataPlayer : IHistoricalDataPlayer
    {
        private static readonly ILogger _log = Log.ForContext(MethodBase.GetCurrentMethod().DeclaringType); 
        private readonly ITradeHistoryStore _tradeHistoryStore;
        private readonly ITradeQuoteStore _tradeQuoteStore;

        public HistoricalDataPlayer(ITradeHistoryStore tradeHistoryStore, ITradeQuoteStore tradeQuoteStore)
        {
            _tradeHistoryStore = tradeHistoryStore;
            _tradeQuoteStore = tradeQuoteStore;
        }

        #region Implementation of IHistoricalDataPlayer

        public IEnumerable<HistoricalTrade> ReadHistoricalTrades(string currencyPair, DateTime from, DateTime to,
            CancellationToken cancellationToken = default, int batchSize = 1000)
        {
            if (from.Kind != DateTimeKind.Utc) throw new ArgumentException("Please provide utc date for this call.", nameof(from));
            if (to.Kind != DateTimeKind.Utc) throw new ArgumentException("Please provide utc date for this call.", nameof(to));

            _log.Information($"ReadHistoricalTrades for {currencyPair} from {from} to {to}");
            var skip = 0;
            int counter;
            do
            {
                counter = 0;
                if (cancellationToken.IsCancellationRequested) break;
                var historicalTrades = _tradeHistoryStore.FindByDate(currencyPair,@from, to, skip, batchSize).Result;
                skip += batchSize;
                foreach (var historicalTrade in historicalTrades.TakeWhile(historicalTrade => !cancellationToken.IsCancellationRequested))
                {
                    yield return historicalTrade;
                    counter++;
                }

            } while (counter > 0);
        }

        public IEnumerable<TradeQuote> ReadHistoricalData(string currencyPair, DateTime @from, DateTime to, PeriodSize periodSize, CancellationToken cancellationToken = default, int batchSize = 1000)
        {
            if (from.Kind != DateTimeKind.Utc) throw new ArgumentException("Please provide utc date for this call.", nameof(from));
            if (to.Kind != DateTimeKind.Utc) throw new ArgumentException("Please provide utc date for this call.", nameof(to));

            _log.Information($"ReadHistoricalTrades {from} {to}");
            var skip = 0;
            int counter;
            do
            {
                counter = 0;
                if (cancellationToken.IsCancellationRequested) break;
                var historicalTrades = _tradeQuoteStore.FindByDate(currencyPair,@from, to,periodSize,skip:skip,take: batchSize).Result;
                skip += batchSize;
                foreach (var historicalTrade in historicalTrades.TakeWhile(historicalTrade => !cancellationToken.IsCancellationRequested))
                {
                    yield return historicalTrade;
                    counter++;
                }

            } while (counter != 0);
        }

        #endregion
    }

}