﻿using System;
using System.Collections.Generic;

namespace FWTL.Aggregate
{
    public class Account
    {
        public Guid Id { get; set; }

        public string ExternalId { get; set; }

        public Guid UserId { get; set; }

        public virtual User User { get; set; }

        public virtual ICollection<TelegramAccountJob> TelegramAccountJobs { get; set; } = new List<TelegramAccountJob>();
    }
}