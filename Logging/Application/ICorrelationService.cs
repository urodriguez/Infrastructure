﻿using Logging.Application.Dtos;
using Shared.Infrastructure.CrossCutting.Authentication;

namespace Logging.Application
{
    public interface ICorrelationService
    {
        CorrelationDto Create(Credential credential, bool validateCredential = true);
    }
}