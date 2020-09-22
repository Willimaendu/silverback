﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    public class StopConsumerErrorPolicy : ErrorPolicyBase
    {
        /// <inheritdoc cref="ErrorPolicyBase.BuildCore" />
        protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider) =>
            new StopConsumerErrorPolicyImplementation(
                MaxFailedAttemptsCount,
                ExcludedExceptions,
                IncludedExceptions,
                ApplyRule,
                MessageToPublishFactory,
                serviceProvider,
                serviceProvider
                    .GetRequiredService<ISilverbackIntegrationLogger<StopConsumerErrorPolicy>>());

        private class StopConsumerErrorPolicyImplementation : ErrorPolicyImplementation
        {
            public StopConsumerErrorPolicyImplementation(
                int? maxFailedAttempts,
                ICollection<Type> excludedExceptions,
                ICollection<Type> includedExceptions,
                Func<IRawInboundEnvelope, Exception, bool>? applyRule,
                Func<IRawInboundEnvelope, object>? messageToPublishFactory,
                IServiceProvider serviceProvider,
                ISilverbackIntegrationLogger<StopConsumerErrorPolicy> logger)
                : base(
                    maxFailedAttempts,
                    excludedExceptions,
                    includedExceptions,
                    applyRule,
                    messageToPublishFactory,
                    serviceProvider,
                    logger)
            {
            }

            protected override Task<bool> ApplyPolicy(ConsumerPipelineContext context, Exception exception)
            {
                // TODO: Log (consumer will be stopped)

                return Task.FromResult(false);
            }
        }
    }
}
