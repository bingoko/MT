﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.Core;
using MarginTrading.Core.Messages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.DataReader.Controllers
{
    [Authorize]
    [Route("api/accountAssets")]
    public class AccountAssetsController : Controller
    {
        private readonly IMarginTradingAccountAssetRepository _accountAssetRepository;

        public AccountAssetsController(IMarginTradingAccountAssetRepository accountAssetRepository)
        {
            _accountAssetRepository = accountAssetRepository;
        }

        [HttpGet]
        [Route("")]
        public async Task<IEnumerable<MarginTradingAccountAsset>> GetAll()
        {
            return (await _accountAssetRepository.GetAllAsync()).Select(MarginTradingAccountAsset.Create);
        }

        [HttpGet]
        [Route("byAsset/{tradingConditionId}/{baseAssetId}")]
        public async Task<IEnumerable<MarginTradingAccountAsset>> GetByAsset(string tradingConditionId, string baseAssetId)
        {
            return (await _accountAssetRepository.GetAllAsync(tradingConditionId, baseAssetId)).Select(MarginTradingAccountAsset.Create);
        }

        [HttpGet]
        [Route("byAssetPair/{tradingConditionId}/{baseAssetId}/{assetPairId}")]
        public async Task<MarginTradingAccountAsset> GetByAssetPairId(string tradingConditionId, string baseAssetId, string assetPairId)
        {
            var accountAsset = await _accountAssetRepository.GetAccountAsset(tradingConditionId, baseAssetId, assetPairId)
                   ?? throw new Exception(string.Format(MtMessages.AccountAssetForTradingConditionNotFound,
                       tradingConditionId, baseAssetId, assetPairId));
            return MarginTradingAccountAsset.Create(accountAsset);
        }
    }
}