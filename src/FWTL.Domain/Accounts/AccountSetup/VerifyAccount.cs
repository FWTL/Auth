﻿using System;
using System.Linq;
using System.Threading.Tasks;
using FWTL.Core.Aggregates;
using FWTL.Core.Commands;
using FWTL.Core.Services.Telegram;
using FWTL.Core.Services.Telegram.Dto;

namespace FWTL.Domain.Accounts.AccountSetup
{
    public class VerifyAccount
    {
        public class Request : IRequest
        {
            public Guid AccountId { get; set; }

            public string Code { get; set; }
        }

        public class Command : Request, ICommand
        {
            public Command()
            {
            }

            public Guid CorrelationId { get; set; }
        }

        public class Handler : ICommandHandler<Command>
        {
            private readonly ITelegramClient _telegramClient;
            private readonly IAggregateStore _aggregateStore;

            public Handler(ITelegramClient telegramClient, IAggregateStore aggregateStore)
            {
                _telegramClient = telegramClient;
                _aggregateStore = aggregateStore;
            }

            public async Task<IAggregateRoot> ExecuteAsync(Command command)
            {
                AccountAggregate account = await _aggregateStore.GetByIdAsync<AccountAggregate, Command>(command.AccountId, command);

                ResponseWrapper response = await _telegramClient.UserService.CompletePhoneLoginAsync(account.Id, command.Code);
                if (response.IsSuccess)
                {
                    account.Verify();
                    return account;
                }

                account.FailSetup(response.Errors.Select(e => e.Message));
                return account;
            }
        }
    }
}