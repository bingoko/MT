﻿﻿using Lykke.ClientGenerator;

namespace MarginTrading.Backend.Contracts.DataReaderClient
{
    internal class MtDataReaderClient : IMtDataReaderClient
    {
        public IAssetPairsReadingApi AssetPairsRead { get; }
        public IAccountHistoryApi AccountHistory { get; }
        public IAccountsApi AccountsApi { get; }
        public IAccountAssetPairsReadingApi AccountAssetPairsRead { get; }
        public ITradeMonitoringReadingApi TradeMonitoringRead { get; }
        public ITradingConditionsReadingApi TradingConditionsRead { get; }
        public IAccountGroupsReadingApi AccountGroups { get; }
        public IDictionariesReadingApi Dictionaries { get; }
        public IRoutesReadingApi Routes { get; }
        public ISettingsReadingApi Settings { get; }

        public MtDataReaderClient(IClientProxyGenerator clientGenerator)
        {
            AssetPairsRead = clientGenerator.Generate<IAssetPairsReadingApi>();
            AccountHistory = clientGenerator.Generate<IAccountHistoryApi>();
            AccountsApi = clientGenerator.Generate<IAccountsApi>();
            AccountAssetPairsRead = clientGenerator.Generate<IAccountAssetPairsReadingApi>();
            TradeMonitoringRead = clientGenerator.Generate<ITradeMonitoringReadingApi>();
            TradingConditionsRead = clientGenerator.Generate<ITradingConditionsReadingApi>();
            AccountGroups = clientGenerator.Generate<IAccountGroupsReadingApi>();
            Dictionaries = clientGenerator.Generate<IDictionariesReadingApi>();
            Routes = clientGenerator.Generate<IRoutesReadingApi>();
            Settings = clientGenerator.Generate<ISettingsReadingApi>();
            AccountGroups = RestService.For<IAccountGroupsReadingApi>(url, settings);
            Dictionaries = RestService.For<IDictionariesReadingApi>(url, settings);
            Routes = RestService.For<IRoutesReadingApi>(url, settings);
            Settings = RestService.For<ISettingsReadingApi>(url, settings);
        }
    }
}