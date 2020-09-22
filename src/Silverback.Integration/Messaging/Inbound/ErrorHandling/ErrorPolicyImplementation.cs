﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    /// <inheritdoc cref="IErrorPolicy" />
    public abstract class ErrorPolicyImplementation : IErrorPolicyImplementation
    {
        private readonly int? _maxFailedAttempts;

        private readonly ICollection<Type> _excludedExceptions;

        private readonly ICollection<Type> _includedExceptions;

        private readonly Func<IRawInboundEnvelope, Exception, bool>? _applyRule;

        private readonly Func<IRawInboundEnvelope, object>? _messageToPublishFactory;

        private readonly IServiceProvider _serviceProvider;

        private readonly ISilverbackIntegrationLogger<ErrorPolicyBase> _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorPolicyImplementation" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        protected ErrorPolicyImplementation(
            int? maxFailedAttempts,
            ICollection<Type> excludedExceptions,
            ICollection<Type> includedExceptions,
            Func<IRawInboundEnvelope, Exception, bool>? applyRule,
            Func<IRawInboundEnvelope, object>? messageToPublishFactory,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<ErrorPolicyBase> logger)
        {
            _maxFailedAttempts = maxFailedAttempts;
            _excludedExceptions = Check.NotNull(excludedExceptions, nameof(excludedExceptions));
            _includedExceptions = Check.NotNull(includedExceptions, nameof(includedExceptions));
            _applyRule = applyRule;
            _messageToPublishFactory = messageToPublishFactory;

            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));
        }

        /// <inheritdoc cref="IErrorPolicyImplementation.CanHandle" />
        public virtual bool CanHandle(ConsumerPipelineContext context, Exception exception)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(exception, nameof(exception));

            var failedAttempts = context.Envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts);

            if (_maxFailedAttempts != null && failedAttempts > _maxFailedAttempts)
            {
                var traceString = $"The policy '{GetType().Name}' will be skipped because the current failed " +
                                  $"attempts ({failedAttempts}) exceeds the configured maximum attempts " +
                                  $"({_maxFailedAttempts}).";

                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.PolicyMaxFailedAttemptsExceeded,
                    traceString,
                    context.Envelope);

                return false;
            }

            if (_includedExceptions.Any() && _includedExceptions.All(type => !type.IsInstanceOfType(exception)))
            {
                var traceString = $"The policy '{GetType().Name}' will be skipped because the " +
                                  $"{exception.GetType().Name} is not in the list of handled exceptions.";

                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.PolicyExceptionNotIncluded,
                    traceString,
                    context.Envelope);

                return false;
            }

            if (_excludedExceptions.Any(type => type.IsInstanceOfType(exception)))
            {
                var traceString = $"The policy '{GetType().Name}' will be skipped because the " +
                                  $"{exception.GetType().Name} is in the list of excluded exceptions.";

                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.PolicyExceptionExcluded,
                    traceString,
                    context.Envelope);

                return false;
            }

            if (_applyRule != null && !_applyRule.Invoke(context.Envelope, exception))
            {
                var traceString = $"The policy '{GetType().Name}' will be skipped because the apply rule has been " +
                                  "evaluated and returned false.";

                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.PolicyApplyRuleReturnedFalse,
                    traceString,
                    context.Envelope);

                return false;
            }

            return true;
        }

        /// <inheritdoc cref="IErrorPolicyImplementation.HandleError" />
        public async Task<bool> HandleError(ConsumerPipelineContext context, Exception exception)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(exception, nameof(exception));

            var result = await ApplyPolicy(context, exception).ConfigureAwait(false);

            if (_messageToPublishFactory != null)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    await scope.ServiceProvider.GetRequiredService<IPublisher>()
                        .PublishAsync(_messageToPublishFactory.Invoke(context.Envelope))
                        .ConfigureAwait(false);
                }
            }

            return result;
        }

        /// <summary>
        ///     Executes the current policy.
        /// </summary>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> related to the message that failed to be processed.
        /// </param>
        /// <param name="exception">
        ///     The exception that was thrown during the processing.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the action
        ///     that the consumer should perform (e.g. skip the message or stop consuming).
        /// </returns>
        protected abstract Task<bool> ApplyPolicy(ConsumerPipelineContext context, Exception exception);
    }
}
