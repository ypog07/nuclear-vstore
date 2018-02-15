using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using AmsMigrator.Models;

using Microsoft.EntityFrameworkCore;

using Serilog;

namespace AmsMigrator.Infrastructure
{
    public class ErmDbClient : IErmDbClient
    {
        private readonly IDbContextFactory _contextFactory;
        private readonly ImportOptions _options;
        private readonly ILogger _logger = Log.Logger;

        public ErmDbClient(IDbContextFactory contextFactory, ImportOptions options)
        {
            _contextFactory = contextFactory;
            _options = options;
        }

        public async Task<List<long>> GetAdvertiserFirmIdsAsync(DateTime sinceDate)
        {
            using (var context = _contextFactory.GetNewContext())
            {
                var firmsQuery = from o in context.Orders
                                 join op in context.OrderPositions on o.Id equals op.OrderId
                                 where !o.IsDeleted
                                       && o.IsActive
                                       && o.EndDistributionDateFact >= sinceDate
                                       && !op.IsDeleted
                                       && op.IsActive
                                 select new { o.FirmId, ProjectId = o.DestOrganizationUnit.DgppId };

                if (_options.ProjectId != null)
                {
                    firmsQuery = firmsQuery.Where(x => x.ProjectId == _options.ProjectId);
                }

                return await firmsQuery.Select(x => x.FirmId).Distinct().ToListAsync();
            }
        }

        public List<long> GetFirmIdsForGivenPositions(DateTime sinceDate, long[] nomenclatureIds)
        {
            using (var context = _contextFactory.GetNewContext())
            {
                var linkedOrders = (from o in context.Orders
                                    join op in context.OrderPositions on o.Id equals op.OrderId
                                    join pp in context.PricePositions on op.PricePositionId equals pp.Id
                                    join p in context.Positions on pp.PositionId equals p.Id
                                    where !o.IsDeleted && o.IsActive
                                          && o.EndDistributionDateFact >= sinceDate
                                          && !op.IsDeleted && op.IsActive && nomenclatureIds.Contains(p.Id)
                                    select o.FirmId)
                                            .Union(from o in context.Orders
                                                   join op in context.OrderPositions on o.Id equals op.OrderId
                                                   join pp in context.PricePositions on op.PricePositionId equals pp.Id
                                                   join p in context.Positions on pp.PositionId equals p.Id
                                                   join pc in context.PositionChildren on p.Id equals pc.MasterPositionId into g
                                                   from pcg in g.DefaultIfEmpty()
                                                   where !o.IsDeleted && o.IsActive
                                                         && o.EndDistributionDateFact >= sinceDate
                                                         && !op.IsDeleted && op.IsActive && nomenclatureIds.Contains(pcg.ChildPositionId)
                                                   select o.FirmId);

                return linkedOrders.Distinct().ToList();
            }
        }

        public async Task<int> BindMaterialToOrderAsync(IEnumerable<MaterialCreationResult> orderBindindData)
        {
            int ordersCount = 0;

            using (var context = _contextFactory.GetNewContext())
            using (var repo = new SimpleRepository<OrderPositionAdvertisement>(context))
            {
                var strategy = context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {

                    using (var tran = await context.Database.BeginTransactionAsync())
                    {
                        try
                        {
                            foreach (var item in orderBindindData)
                            {
                                var orderPositionAdvertisements = await (from opa in context.OrderPositionAdvertisement
                                                                         join op in context.OrderPositions on opa.OrderPositionId equals op.Id
                                                                         join or in context.Orders on op.OrderId equals or.Id
                                                                         where or.FirmId == item.FirmId && !or.IsDeleted
                                                                                                        && or.IsActive && !op.IsDeleted &&
                                                                                                        op.IsActive && item
                                                                                                                       .BindedNomenclatures
                                                                                                                       .Contains(opa.PositionId)
                                                                         select new OrderPositionAdvertisementOrder { OPA = opa, Order = or })
                                                                      .ToListAsync();

                                _logger.Information("[BINDED_ORDERS] {orderCount} orders found for material {material} firm {firm}",
                                                    orderPositionAdvertisements.Count,
                                                    item.MaterialId,
                                                    item.FirmId);

                                if (!orderPositionAdvertisements.Any()) continue;
                                ordersCount += orderPositionAdvertisements.Count;

                                foreach (var opa in orderPositionAdvertisements)
                                {
                                    opa.OPA.AdvertisementId = item.MaterialId;
                                    opa.OPA.ModifiedOn = DateTime.UtcNow;
                                    opa.OPA.ModifiedBy = _options.ErmUserId;

                                    repo.Edit(opa.OPA);

                                    _logger.Information(
                                        "[MATERIAL_ORDER_BINDING] Material {materialId} firm {firmId} has been successfully binded to order {orderId}",
                                        item.MaterialId,
                                        item.FirmId,
                                        opa.Order.Id);

                                    await context.Database.ExecuteSqlCommandAsync("exec Adm.PushOrder2Primary {0}", opa.Order.Id);

                                    _logger.Information("[SP_EXEC] Stored procedure called for order: {orderId}", opa.Order.Id);
                                }

                            }

                            await repo.CommitChangesAsync();

                            tran.Commit();
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "[BINDING_FAILED] Unable to bind materials to orders: {materialIds}",
                                          string.Join(", ", orderBindindData.Select(x => $"({x.FirmId} - {x.MaterialId})")));
                            tran.Rollback();
                            throw;
                        }
                    }
                });
            }
            return ordersCount;
        }

        class OrderPositionAdvertisementOrder
        {
            public OrderPositionAdvertisement OPA { get; set; }
            public Order Order { get; set; }
        }
    }
}
