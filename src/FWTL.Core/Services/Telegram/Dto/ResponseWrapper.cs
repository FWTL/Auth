﻿using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FWTL.Core.Services.Telegram.Dto
{
    public class ResponseWrapper<TResponse> : ResponseWrapper
    {
        public TResponse Response { get; set; }
    }

    public class ResponseWrapper
    {
        [JsonPropertyName("success")]
        public bool IsSuccess { get; set; }

        public bool NotFound { get; set; }

        public IEnumerable<Error> Errors { get; set; } = new List<Error>();
    }
}