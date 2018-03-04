﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using MarginTrading.AzureRepositories.Entities;
using MarginTrading.Backend.Core;
using MarginTrading.Backend.Core.Repositories;

namespace MarginTrading.AzureRepositories
{
	public class OvernightSwapStateRepository : IOvernightSwapStateRepository
	{
		private readonly INoSQLTableStorage<OvernightSwapStateEntity> _tableStorage;
		
		public OvernightSwapStateRepository(INoSQLTableStorage<OvernightSwapStateEntity> tableStorage)
		{
			_tableStorage = tableStorage;
		}
		
		public async Task AddOrReplaceAsync(IOvernightSwapState obj)
		{
			await _tableStorage.InsertOrReplaceAsync(OvernightSwapStateEntity.Create(obj));
		}

		public async Task<IEnumerable<IOvernightSwapState>> GetAsync()
		{
			return await _tableStorage.GetDataAsync();
		}

		public async Task<IReadOnlyList<IOvernightSwapState>> GetAsync(string accountId, DateTime? @from, DateTime? to)
		{
			return (await _tableStorage.WhereAsync(accountId, from ?? DateTime.MinValue, to ?? DateTime.MaxValue, 
					ToIntervalOption.IncludeTo))
				.OrderByDescending(item => item.Timestamp).ToList();
		}
	}
}