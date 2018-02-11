﻿using System;
using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;

namespace MarginTrading.OrderbookBestPricesBroker.Repositories
{
    internal class OrderbookBestPricesRepository : IOrderbookBestPricesRepository
    {
        private readonly INoSQLTableStorage<OrderbookBestPricesEntity> _tableStorage;

        public OrderbookBestPricesRepository(IReloadingManager<Settings> settings, ILog log)
        {
            _tableStorage = AzureTableStorage<OrderbookBestPricesEntity>.Create(settings.Nested(s => s.Db.HistoryConnString),
                "OrderbookBestPrices", log);
        }

        public Task InsertOrReplaceAsync(OrderbookBestPricesEntity entity, DateTime time)
        {
            return _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity, time);
        }
    }
}
