﻿using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Shared.Infrastructure.CrossCutting.Authentication;

namespace Shared.Infrastructure.CrossCutting.AppSettings
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly IConfiguration _configuration;

        public AppSettingsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string AuditingConnectionString
        {
            get
            {
                switch (Environment.Name)
                {
                    case "DEV": return   "Server=localhost;Database=UciRod.Infrastructure.Auditing;User ID=ucirod-infrastructure-user;Password=uc1r0d-1nfr45tructur3-user;Trusted_Connection=True;MultipleActiveResultSets=true";
                    case "TEST": return  "Server=localhost;Database=UciRod.Infrastructure.Auditing-Test;User ID=ucirod-infrastructure-user;Password=uc1r0d-1nfr45tructur3-user;Trusted_Connection=True;MultipleActiveResultSets=true";
                    case "STAGE": return "Server=localhost;Database=UciRod.Infrastructure.Auditing-Stage;User ID=ucirod-infrastructure-user;Password=uc1r0d-1nfr45tructur3-user;Trusted_Connection=True;MultipleActiveResultSets=true";
                    case "PROD": return  "Server=localhost;Database=UciRod.Infrastructure.Auditing;User ID=ucirod-infrastructure-user;Password=uc1r0d-1nfr45tructur3-user;Trusted_Connection=True;MultipleActiveResultSets=true";

                    default: throw new ArgumentOutOfRangeException($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name} | Invalid Environment");
                }
            }
        }

        public Credential Credential => new Credential { Id = "Insfrastructure", SecretKey = "1nfr4structur3_1nfr4structur3" };

        public InsfrastructureEnvironment Environment => new InsfrastructureEnvironment
        {
            Name = _configuration.GetValue<string>("Environment")
        };

        public string FileSystemLogsDirectory => $"{InsfrastructureDirectory}\\FileSystemLogs";


        public string HangfireLoggingConnectionString
        {
            get
            {
                switch (Environment.Name)
                {
                    case "DEV": return   "Server=localhost;Database=UciRod.Infrastructure.Logging.Hangfire;Integrated Security=SSPI;";
                    case "TEST": return  "Server=localhost;Database=UciRod.Infrastructure.Logging.Hangfire-Test;Integrated Security=SSPI;";
                    case "STAGE": return "Server=localhost;Database=UciRod.Infrastructure.Logging.Hangfire-Stage;Integrated Security=SSPI;";
                    case "PROD": return  "Server=localhost;Database=UciRod.Infrastructure.Logging.Hangfire;Integrated Security=SSPI;";

                    default: throw new ArgumentOutOfRangeException($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name} | Invalid Environment");
                }
            }
        }

        public string InsfrastructureDirectory
        {
            get
            {
                switch (Environment.Name)
                {
                    case "DEV": return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory.Substring(0, AppContext.BaseDirectory.IndexOf("bin")), @".."));
                    case "TEST":
                    case "STAGE":
                    case "PROD": return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @".."));

                    default: throw new ArgumentOutOfRangeException($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name} | Invalid Environment");
                }
            }
        }

        public string LoggingConnectionString
        {
            get
            {
                switch (Environment.Name)
                {
                    case "DEV": return   "Server=localhost;Database=UciRod.Infrastructure.Logging;User ID=ucirod-infrastructure-user;Password=uc1r0d-1nfr45tructur3-user;Trusted_Connection=True;MultipleActiveResultSets=true";
                    case "TEST": return  "Server=localhost;Database=UciRod.Infrastructure.Logging-Test;User ID=ucirod-infrastructure-user;Password=uc1r0d-1nfr45tructur3-user;Trusted_Connection=True;MultipleActiveResultSets=true";
                    case "STAGE": return "Server=localhost;Database=UciRod.Infrastructure.Logging-Stage;User ID=ucirod-infrastructure-user;Password=uc1r0d-1nfr45tructur3-user;Trusted_Connection=True;MultipleActiveResultSets=true";
                    case "PROD": return  "Server=localhost;Database=UciRod.Infrastructure.Logging;User ID=ucirod-infrastructure-user;Password=uc1r0d-1nfr45tructur3-user;Trusted_Connection=True;MultipleActiveResultSets=true";

                    default: throw new ArgumentOutOfRangeException($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name} | Invalid Environment");
                }
            }
        }

        public string LoggingApiUrlV1
        {
            get
            {
                const string project = "logging";

                switch (Environment.Name)
                {
                    case "DEV":   return $"http://www.ucirod.infrastructure-test.com:40000/{project}/api/v1.0";
                    case "TEST":  return $"http://www.ucirod.infrastructure-test.com:40000/{project}/api/v1.0";
                    case "STAGE": return $"http://www.ucirod.infrastructure-stage.com:40000/{project}/api/v1.0";
                    case "PROD":  return $"http://www.ucirod.infrastructure.com:40000/{project}/api/v1.0";

                    default: throw new ArgumentOutOfRangeException($"{GetType().Name}.{MethodBase.GetCurrentMethod().Name} | Invalid Environment");
                }
            }
        }

        public string LoggingApiUrlV2 => LoggingApiUrlV1.Replace("v1.0", "v2.0");
    }
}