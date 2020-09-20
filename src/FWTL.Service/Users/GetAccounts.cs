﻿using FWTL.Common.Extensions;
using FWTL.Core.Database;
using FWTL.Core.Queries;
using FWTL.Core.Services;
using FWTL.TelegramClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FWTL.Domain.Users
{
    public class GetAccounts
    {
        public class Query : IQuery
        {
            public Query()
            {
            }

            public Query(ICurrentUserService currentUserService)
            {
                UserId = currentUserService.CurrentUserId;
            }

            public Guid UserId { get; set; }
        }

        public class Result
        {
            public string Number { get; set; }

            public string FirstName { get; set; }

            public string LastName { get; set; }

            public string UserName { get; set; }

            public bool IsLogged { get; set; }
        }

        public class Handler : IQueryHandler<Query, IReadOnlyList<Result>>
        {
            private readonly ITelegramClient _telegramClient;
            private readonly IAuthDatabaseContext _dbAuthDatabaseContext;

            public Handler(ITelegramClient telegramClient, IAuthDatabaseContext dbAuthDatabaseContext)
            {
                _telegramClient = telegramClient;
                _dbAuthDatabaseContext = dbAuthDatabaseContext;
            }

            public async Task<IReadOnlyList<Result>> HandleAsync(Query query)
            {
                var accounts = _dbAuthDatabaseContext.TelegramAccount
                    .Where(ta => ta.UserId == query.UserId)
                    .Select(ta => ta.AccountId).ToList();

                var telegramAccounts = new List<Result>();
                foreach (string account in accounts)
                {
                    string sessionName = query.UserId.ToSession(account);
                    var result = await _telegramClient.UserService.GetSelfAsync(sessionName);

                    if (result.IsNotNull())
                    {
                        telegramAccounts.Add(new Result()
                        {
                            FirstName = result.Firstname,
                            LastName = result.Lastname,
                            Number = account,
                            UserName = result.Username,
                            IsLogged = true,
                        });
                    }
                    else
                    {
                        telegramAccounts.Add(new Result()
                        {
                            Number = account,
                            IsLogged = false,
                        });
                    }
                }

                return telegramAccounts;
            }
        }
    }
}