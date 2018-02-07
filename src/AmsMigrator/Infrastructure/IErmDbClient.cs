﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AmsMigrator.Infrastructure
{
    public interface IErmDbClient
    {
        Task<int> BindMaterialToOrderAsync(List<OrderMaterialBindingData> orderBindindData);
        Task<List<long>> GetAdvertiserFirmIdsAsync(DateTime sinceDate);
        List<long> GetFirmIdsForGivenPositions(DateTime sinceDate, long[] nomenclatureIds);
    }
}