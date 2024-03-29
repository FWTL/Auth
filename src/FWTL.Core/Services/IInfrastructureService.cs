﻿using System;
using System.Threading.Tasks;

namespace FWTL.Core.Services
{
    public interface IInfrastructureService
    {
        public Task<Result> GenerateTelegramApi(Guid accountId);

        public Task<Result> TearDownTelegramApi(Guid accountId);
    }
}