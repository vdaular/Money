﻿using Microsoft.EntityFrameworkCore;
using Money.Data;
using Money.Events;
using Money.Services.Models.Queries;
using Neptuo;
using Neptuo.Activators;
using Neptuo.Events.Handlers;
using Neptuo.Models.Keys;
using Neptuo.Queries.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Money.Services.Models.Builders
{
    public class OutcomeBuilder : IEventHandler<OutcomeCreated>, 
        IEventHandler<OutcomeCategoryAdded>, 
        IQueryHandler<ListMonthWithOutcome, IEnumerable<MonthModel>>,
        IQueryHandler<ListMonthCategoryWithOutcome, IEnumerable<CategoryWithAmountModel>>,
        IQueryHandler<GetTotalMonthOutcome, Price>,
        IQueryHandler<GetCategoryName, string>,
        IQueryHandler<ListMonthOutcomeFromCategory, IEnumerable<OutcomeOverviewModel>>,
        IQueryHandler<ListYearOutcomeFromCategory, IEnumerable<OutcomeOverviewModel>>
    {
        private readonly IFactory<Price, decimal> priceFactory;

        public OutcomeBuilder(IFactory<Price, decimal> priceFactory)
        {
            Ensure.NotNull(priceFactory, "priceFactory");
            this.priceFactory = priceFactory;
        }

        public async Task<IEnumerable<MonthModel>> HandleAsync(ListMonthWithOutcome query)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                await Task.Delay(3000);

                var entities = await db.Outcomes
                    .OrderByDescending(o => o.When)
                    .Select(o => new { Year = o.When.Year, Month = o.When.Month })
                    .Distinct()
                    .ToListAsync();

                return entities
                    .Select(o => new MonthModel(o.Year, o.Month));
            }
        }

        public async Task<IEnumerable<CategoryWithAmountModel>> HandleAsync(ListMonthCategoryWithOutcome query)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                Dictionary<Guid, Price> totals = new Dictionary<Guid, Price>();

                List<OutcomeEntity> outcomes = await db.Outcomes
                    .Where(o => o.When.Month == query.Month.Month && o.When.Year == query.Month.Year)
                    .Include(o => o.Categories)
                    .ToListAsync();

                foreach (OutcomeEntity outcome in outcomes)
                {
                    foreach (OutcomeCategoryEntity category in outcome.Categories)
                    {
                        Price price;
                        if (totals.TryGetValue(category.CategoryId, out price))
                            price = price + new Price(outcome.Amount, outcome.Currency);
                        else
                            price = new Price(outcome.Amount, outcome.Currency);

                        totals[category.CategoryId] = price;
                    }
                }

                List<CategoryWithAmountModel> result = new List<CategoryWithAmountModel>();
                foreach (var item in totals)
                {
                    CategoryModel model = (await db.Categories.FindAsync(item.Key)).ToModel();
                    result.Add(new CategoryWithAmountModel(
                        model.Key, 
                        model.Name, 
                        model.Description, 
                        model.Color, 
                        item.Value
                    ));
                }

                return result.OrderBy(m => m.Name);
            }
        }

        public async Task<string> HandleAsync(GetCategoryName query)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                CategoryEntity category = await db.Categories.FindAsync(query.CategoryKey.AsGuidKey().Guid);
                if (category == null)
                    throw Ensure.Exception.ArgumentOutOfRange("categoryKey", "No such category with key '{0}'.", query.CategoryKey);

                return category.Name;
            }
        }

        public async Task<Price> HandleAsync(GetTotalMonthOutcome query)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                List<Price> outcomes = await db.Outcomes
                    .Where(o => o.When.Month == query.Month.Month && o.When.Year == query.Month.Year)
                    .Select(o => new Price(o.Amount, o.Currency))
                    .ToListAsync();

                Price price = priceFactory.Create(0);
                foreach (Price outcome in outcomes)
                    price += outcome;

                return price;
            }
        }

        public async Task<IEnumerable<OutcomeOverviewModel>> HandleAsync(ListYearOutcomeFromCategory query)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                IQueryable<OutcomeEntity> entities = db.Outcomes;
                if (!query.CategoryKey.IsEmpty)
                    entities = entities.Where(o => o.Categories.Select(c => c.CategoryId).Contains(query.CategoryKey.AsGuidKey().Guid));

                List<OutcomeOverviewModel> outcomes = await entities
                    .Where(o => o.When.Year == query.Year.Year)
                    .OrderBy(o => o.When)
                    .Select(o => o.ToOverviewModel())
                    .ToListAsync();

                return outcomes;
            }
        }

        public async Task<IEnumerable<OutcomeOverviewModel>> HandleAsync(ListMonthOutcomeFromCategory query)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                IQueryable<OutcomeEntity> entities = db.Outcomes;
                if (!query.CategoryKey.IsEmpty)
                    entities = entities.Where(o => o.Categories.Select(c => c.CategoryId).Contains(query.CategoryKey.AsGuidKey().Guid));

                List<OutcomeOverviewModel> outcomes = await entities
                    .Where(o => o.When.Month == query.Month.Month && o.When.Year == query.Month.Year)
                    .OrderBy(o => o.When)
                    .Select(o => o.ToOverviewModel())
                    .ToListAsync();

                return outcomes;
            }
        }

        public async Task HandleAsync(OutcomeCategoryAdded payload)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                OutcomeEntity entity = await db.Outcomes.FindAsync(payload.AggregateKey.AsGuidKey().Guid);
                if (entity != null)
                {
                    entity.Categories.Add(new OutcomeCategoryEntity()
                    {
                        OutcomeId = payload.AggregateKey.AsGuidKey().Guid,
                        CategoryId = payload.CategoryKey.AsGuidKey().Guid
                    });
                    await db.SaveChangesAsync();
                }
            }
        }

        public Task HandleAsync(OutcomeCreated payload)
        {
            using (ReadModelContext db = new ReadModelContext())
            {
                db.Outcomes.Add(new OutcomeEntity(new OutcomeModel(
                    payload.AggregateKey,
                    payload.Amount,
                    payload.When,
                    payload.Description,
                    new List<IKey>() { payload.CategoryKey }
                )));
                return db.SaveChangesAsync();
            }
        }
    }
}
