﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using MarginTrading.Backend.Attributes;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Mappers;
using MarginTrading.Backend.Core.MatchingEngines;
using MarginTrading.Backend.Core.Settings;
using MarginTrading.Backend.Models;
using MarginTrading.Backend.Services;
using MarginTrading.Backend.Services.MatchingEngines;
using MarginTrading.Common.Middleware;
using MarginTrading.Common.Services;
using MarginTrading.Common.Services.Settings;
using MarginTrading.Contract.BackendContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.Backend.Controllers
{
    [Authorize]
    [Route("api/backoffice")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class BackOfficeController : Controller
    {
        private readonly IAssetPairsCache _assetPairsCache;
        private readonly IAccountsCacheService _accountsCacheService;
        private readonly AccountManager _accountManager;
        private readonly MatchingEngineRoutesManager _routesManager;
        private readonly IOrderReader _ordersReader;
        private readonly MarginSettings _marginSettings;
        private readonly IMarginTradingSettingsService _marginTradingSettingsService;
        private readonly IMatchingEngineRepository _meRepository;

        public BackOfficeController(
            
            IAssetPairsCache assetPairsCache,
            IAccountsCacheService accountsCacheService,
            AccountManager accountManager,
            MatchingEngineRoutesManager routesManager,
            IOrderReader ordersReader,
            MarginSettings marginSettings,
            IMarginTradingSettingsService marginTradingSettingsService,
            IMatchingEngineRepository meRepository)
        {
            _assetPairsCache = assetPairsCache;
            _accountsCacheService = accountsCacheService;

            _accountManager = accountManager;
            _routesManager = routesManager;
            _ordersReader = ordersReader;
            _marginSettings = marginSettings;
            _marginTradingSettingsService = marginTradingSettingsService;
            _meRepository = meRepository;
        }


        #region Monitoring

        /// <summary>
        /// Returns summary asset info
        /// </summary>
        /// <remarks>
        /// VolumeLong is a sum of long positions volume
        ///
        /// VolumeShort is a sum of short positions volume
        ///
        /// PnL is a sum of all positions PnL
        ///
        /// Header "api-key" is required
        /// </remarks>
        /// <response code="200">Returns summary info by assets</response>
        [HttpGet]
        [Route("assetsInfo")]
        [ProducesResponseType(typeof(List<SummaryAssetInfo>), 200)]
        public List<SummaryAssetInfo> GetAssetsInfo()
        {
            var result = new List<SummaryAssetInfo>();
            var orders = _ordersReader.GetAll().ToList();

            foreach (var order in orders)
            {
                var assetInfo = result.FirstOrDefault(item => item.AssetPairId == order.Instrument);

                if (assetInfo == null)
                {
                    result.Add(new SummaryAssetInfo
                    {
                        AssetPairId = order.Instrument,
                        PnL = order.GetFpl(),
                        VolumeLong = order.GetOrderType() == OrderDirection.Buy ? order.GetMatchedVolume() : 0,
                        VolumeShort = order.GetOrderType() == OrderDirection.Sell ? order.GetMatchedVolume() : 0
                    });
                }
                else
                {
                    assetInfo.PnL += order.GetFpl();

                    if (order.GetOrderType() == OrderDirection.Buy)
                    {
                        assetInfo.VolumeLong += order.GetMatchedVolume();
                    }
                    else
                    {
                        assetInfo.VolumeShort += order.GetMatchedVolume();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Returns list of opened positions
        /// </summary>
        /// <remarks>
        /// Returns list of opened positions with matched volume greater or equal provided "volume" parameter
        ///
        /// Header "api-key" is required
        /// </remarks>
        /// <response code="200">Returns opened positions</response>
        [HttpGet]
        [Route("positionsByVolume")]
        [ProducesResponseType(typeof(List<OrderContract>), 200)]
        public List<OrderContract> GetPositionsByVolume([FromQuery]decimal volume)
        {
            var result = new List<OrderContract>();
            var orders = _ordersReader.GetActive();

            foreach (var order in orders)
            {
                if (order.GetMatchedVolume() >= volume)
                {
                    result.Add(order.ToBaseContract());
                }
            }

            return result;
        }

        /// <summary>
        /// Returns list of pending orders
        /// </summary>
        /// <remarks>
        /// Returns list of pending orders with volume greater or equal provided "volume" parameter
        ///
        /// Header "api-key" is required
        /// </remarks>
        /// <response code="200">Returns pending orders</response>
        [HttpGet]
        [Route("pendingOrdersByVolume")]
        [ProducesResponseType(typeof(List<OrderContract>), 200)]
        public List<OrderContract> GetPendingOrdersByVolume([FromQuery]decimal volume)
        {
            var result = new List<OrderContract>();
            var orders = _ordersReader.GetPending();

            foreach (var order in orders)
            {
                if (Math.Abs(order.Volume) >= volume)
                {
                    result.Add(order.ToBaseContract());
                }
            }

            return result;
        }

        #endregion


        #region Dictionaries

        [HttpGet]
        [Route("instruments/getall")]
        [ProducesResponseType(typeof(List<AssetPair>), 200)]
        public IActionResult GetAllInstruments()
        {
            var instruments = _assetPairsCache.GetAll();
            return Ok(instruments);
        }

        [HttpGet]
        [Route("matchingengines")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public IActionResult GetAllMatchingEngines()
        {
            var matchingEngines = _meRepository.GetMatchingEngines().Select(me => me.Id);
            return Ok(matchingEngines);
        }

        [HttpGet]
        [Route("orderTypes/getall")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public IActionResult GetAllOrderTypes()
        {
            var orderTypes = Enum.GetNames(typeof(OrderDirection));
            return Ok(orderTypes);
        }

        #endregion


        #region Accounts

        [HttpGet]
        [Route("marginTradingAccounts/getall/{clientId}")]
        [ProducesResponseType(typeof(List<MarginTradingAccount>), 200)]
        public IActionResult GetAllMarginTradingAccounts(string clientId)
        {
            var accounts = _accountsCacheService.GetAll(clientId);
            return Ok(accounts);
        }

        [HttpPost]
        [Route("marginTradingAccounts/delete/{clientId}/{accountId}")]
        public async Task<IActionResult> DeleteMarginTradingAccount(string clientId, string accountId)
        {
            await _accountManager.DeleteAccountAsync(clientId, accountId);
            return Ok();
        }

        [HttpPost]
        [Route("marginTradingAccounts/init")]
        public async Task<InitAccountsResponse> InitMarginTradingAccounts([FromBody]InitAccountsRequest request)
        {
            var accounts = _accountsCacheService.GetAll(request.ClientId);

            if (accounts.Any())
            {
                return new InitAccountsResponse { Status = CreateAccountStatus.Available };
            }

            if (string.IsNullOrEmpty(request.TradingConditionsId))
            {
                return new InitAccountsResponse
                {
                    Status = CreateAccountStatus.Error,
                    Message = "Can't create accounts - no trading condition passed"
                };
            }

            await _accountManager.CreateDefaultAccounts(request.ClientId, request.TradingConditionsId);

            return new InitAccountsResponse { Status = CreateAccountStatus.Created};
        }

        [HttpPost]
        [Route("marginTradingAccounts/add")]
        public async Task<IActionResult> AddMarginTradingAccount([FromBody]MarginTradingAccount account)
        {
            await _accountManager.AddAccountAsync(account.ClientId, account.BaseAssetId, account.TradingConditionId);
            return Ok();
        }

        #endregion


        #region Matching engine routes

        [HttpGet]
        [Route("routes")]
        [ProducesResponseType(typeof(List<MatchingEngineRoute>), 200)]
        public IActionResult GetAllRoutes()
        {
            var routes = _routesManager.GetRoutes();
            return Ok(routes);
        }

        [HttpGet]
        [Route("routes/{id}")]
        [ProducesResponseType(typeof(MatchingEngineRoute), 200)]
        public IActionResult GetRoute(string id)
        {
            var route = _routesManager.GetRouteById(id);
            return Ok(route);
        }

        [HttpPost]
        [Route("routes")]
        public async Task<IActionResult> AddRoute([FromBody]NewMatchingEngineRouteRequest request)
        {
            var newRoute = DomainObjectsFactory.CreateRoute(request);
            await _routesManager.AddOrReplaceRouteAsync(newRoute);
            return Ok(newRoute);
        }

        [HttpPut]
        [Route("routes/{id}")]
        public async Task<IActionResult> EditRoute(string id, [FromBody]NewMatchingEngineRouteRequest request)
        {
            var existingRoute = _routesManager.GetRouteById(id);
            if (existingRoute != null)
            {
                var route = DomainObjectsFactory.CreateRoute(request, id);
                await _routesManager.AddOrReplaceRouteAsync(route);
                return Ok(_routesManager);
            }
            else
                throw new Exception("MatchingEngine Route not found");
        }

        [HttpDelete]
        [Route("routes/{id}")]
        public async Task<IActionResult> DeleteRoute(string id)
        {
            await _routesManager.DeleteRouteAsync(id);
            return Ok();
        }

        #endregion


        #region Settings

        [HttpPost]
        [Route("settings/enabled/{clientId}")]
        [SkipMarginTradingEnabledCheck]
        public async Task<IActionResult> SetMarginTradingIsEnabled(string clientId, [FromBody]bool enabled)
        {
            await _marginTradingSettingsService.SetMarginTradingEnabled(clientId, _marginSettings.IsLive, enabled);
            return Ok();
        }

        #endregion
        
    }
}
