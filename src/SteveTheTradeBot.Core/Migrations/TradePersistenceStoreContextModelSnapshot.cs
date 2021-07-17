﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using SteveTheTradeBot.Core.Components.Storage;

namespace SteveTheTradeBot.Core.Migrations
{
    [DbContext(typeof(TradePersistenceStoreContext))]
    partial class TradePersistenceStoreContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.7")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("SteveTheTradeBot.Dal.Models.Trades.DynamicPlotter", b =>
                {
                    b.Property<string>("Feed")
                        .HasColumnType("text");

                    b.Property<string>("Label")
                        .HasColumnType("text");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("Value")
                        .HasColumnType("numeric");

                    b.HasKey("Feed", "Label", "Date");

                    b.ToTable("DynamicPlots");
                });

            modelBuilder.Entity("SteveTheTradeBot.Dal.Models.Trades.HistoricalTrade", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("text");

                    b.Property<string>("CurrencyPair")
                        .HasColumnType("text");

                    b.Property<decimal>("Price")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Quantity")
                        .HasColumnType("numeric");

                    b.Property<decimal>("QuoteVolume")
                        .HasColumnType("numeric");

                    b.Property<int>("SequenceId")
                        .HasColumnType("integer");

                    b.Property<string>("TakerSide")
                        .HasColumnType("text");

                    b.Property<DateTime>("TradedAt")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("TradedAt", "SequenceId");

                    b.ToTable("HistoricalTrades");
                });

            modelBuilder.Entity("SteveTheTradeBot.Dal.Models.Trades.TradeFeedCandle", b =>
                {
                    b.Property<string>("Feed")
                        .HasColumnType("text");

                    b.Property<int>("PeriodSize")
                        .HasColumnType("integer");

                    b.Property<DateTime>("Date")
                        .HasColumnType("timestamp without time zone");

                    b.Property<decimal>("Close")
                        .HasColumnType("numeric");

                    b.Property<decimal>("High")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Low")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Open")
                        .HasColumnType("numeric");

                    b.Property<decimal>("Volume")
                        .HasColumnType("numeric");

                    b.HasKey("Feed", "PeriodSize", "Date");

                    b.ToTable("TradeFeedCandles");
                });
#pragma warning restore 612, 618
        }
    }
}
