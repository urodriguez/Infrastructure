﻿using Logging.Domain;
using Shared.Infrastructure.CrossCuttingV3.Authentication;

namespace Logging.Application.Dtos
{
    public class LogDtoPost
    {
        public Credential Credential { get; set; }
        public string Application { get; set; }
        public string Project { get; set; }
        public string CorrelationId { get; set; }
        public string Text { get; set; }
        public LogType Type { get; set; }
        public string Environment { get; set; }
    }
}