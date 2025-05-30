﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.WebJobs.Extensions.AzureCosmosDb.Mongo.Tests
{
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly LoggerFilterOptions _filter;
        private readonly Action<LogMessage> _logAction;
        private readonly Regex userCategoryRegex = new Regex(@"^Function\.\w+\.User$");
        private readonly Dictionary<string, TestLogger> _loggerCache = new Dictionary<string, TestLogger>();

        public TestLoggerProvider(Action<LogMessage> logAction = null)
        {
            _filter = new LoggerFilterOptions();
            _logAction = logAction;
        }

        public IList<TestLogger> CreatedLoggers => _loggerCache.Values.ToList();

        public ILogger CreateLogger(string categoryName)
        {
            if (!_loggerCache.TryGetValue(categoryName, out TestLogger logger))
            {
                logger = new TestLogger(categoryName, _logAction);
                _loggerCache.Add(categoryName, logger);
            }

            return logger;
        }

        public IEnumerable<LogMessage> GetAllLogMessages() => CreatedLoggers.SelectMany(l => l.GetLogMessages()).OrderBy(p => p.Timestamp);

        public IEnumerable<LogMessage> GetAllUserLogMessages()
        {
            return GetAllLogMessages().Where(p => userCategoryRegex.IsMatch(p.Category));
        }

        public string GetLogString() => string.Join(Environment.NewLine, GetAllLogMessages());

        public void ClearAllLogMessages()
        {
            foreach (TestLogger logger in CreatedLoggers)
            {
                logger.ClearLogMessages();
            }
        }

        public void Dispose()
        {
        }
    }
}
