﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FWTL.Core.Queries
{
    public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery
    {
        Task<TResult> HandleAsync(TQuery query);
    }
}