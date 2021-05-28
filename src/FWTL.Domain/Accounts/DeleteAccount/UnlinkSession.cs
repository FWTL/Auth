﻿using System;
using System.Threading.Tasks;
using FWTL.Core.Aggregates;
using FWTL.Core.Commands;
using FWTL.Core.Services.Telegram;
using FWTL.Core.Services.Telegram.Dto;

namespace FWTL.Domain.Accounts.AccountSetup
{
    public class UnlinkSession
    {
        public class Command : ICommand
        {
            public Guid AccountId { get; set; }
            public Guid CorrelationId { get; set; }
        }

        public class Handler : ICommandHandler<Command>
        {
            private readonly IAggregateStore _aggregateStore;
            private readonly ITelegramClient _telegramClient;

            public Handler(IAggregateStore aggregateStore, ITelegramClient telegramClient)
            {
                _aggregateStore = aggregateStore;
                _telegramClient = telegramClient;
            }

            public async Task<IAggregateRoot> ExecuteAsync(Command command)
            {
                var account = await _aggregateStore.GetByIdAsync<AccountAggregate>(command.AccountId,true);

                ResponseWrapper response = await _telegramClient.SystemService.UnlinkSession(account.Id.ToString());

                if (response.IsSuccess || response.NotFound)
                {
                    account.UnlinkSession();
                    return account;
                }

                return account;
            }
        }
    }
}