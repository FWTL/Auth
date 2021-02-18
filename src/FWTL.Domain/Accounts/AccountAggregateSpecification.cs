﻿using System.Linq;
using FluentValidation;
using FWTL.Core.Aggregates;
using FWTL.Core.Specification;
using FWTL.Core.Validation;
using FWTL.Database.Access;
using FWTL.Events;
using Microsoft.EntityFrameworkCore;

namespace FWTL.Domain.Accounts
{
    public class AccountAggregateSpecification : AppAbstractValidation<AccountAggregate>,
        ISpecificationFor<AccountAggregate, AccountCreated>
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly IDatabaseContext _dbContext;

        public AccountAggregateSpecification(IAggregateStore aggregateStore, IDatabaseContext dbContext)
        {
            _aggregateStore = aggregateStore;
            _dbContext = dbContext;
        }

        public void MustBeUnique()
        {
            RuleFor(x => x).CustomAsync(async (aggregate, context, token) =>
            {
                bool doesExist = await _dbContext.Accounts.AsQueryable()
                    .Where(account => account.OwnerId == aggregate.OwnerId)
                    .Where(account => account.ExternalAccountId == aggregate.ExternalAccountId)
                    .Where(account => account.Id != aggregate.Id).AnyAsync(cancellationToken: token);

                if (doesExist)
                {
                    context.AddFailure(nameof(AccountAggregate), "Account already exists");
                }
            });
        }

        public IValidator<AccountAggregate> Apply(AccountCreated @event)
        {
            MustBeUnique();
            return this;
        }
    }
}