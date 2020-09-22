// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     The context that is passed along the consumer behaviors pipeline.
    /// </summary>
    public class ConsumerPipelineContext
    {
        private IConsumerTransactionManager? _transactionManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerPipelineContext" /> class.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message being processed.
        /// </param>
        /// <param name="consumer">
        ///     The <see cref="IConsumer" /> that triggered this pipeline.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public ConsumerPipelineContext(
            IRawInboundEnvelope envelope,
            IConsumer consumer,
            IServiceProvider serviceProvider)
        {
            Envelope = Check.NotNull(envelope, nameof(envelope));
            Consumer = Check.NotNull(consumer, nameof(consumer));
            ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerPipelineContext" /> class.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message being processed.
        /// </param>
        /// <param name="context">
        ///     The <see cref="ConsumerPipelineContext" /> that was being used until this point in the pipeline. The
        ///     <see cref="IConsumer" />, <see cref="IServiceProvider" /> and
        ///     <see cref="IConsumerTransactionManager" /> will be reused.
        /// </param>
        public ConsumerPipelineContext(IRawInboundEnvelope envelope, ConsumerPipelineContext context)
        {
            Envelope = Check.NotNull(envelope, nameof(envelope));

            Check.NotNull(context, nameof(context));

            Consumer = context.Consumer;
            ServiceProvider = context.ServiceProvider;
            _transactionManager = context.TransactionManager;
        }

        /// <summary>
        ///     Gets the <see cref="IConsumer" /> that triggered this pipeline.
        /// </summary>
        public IConsumer Consumer { get; }

        /// <summary>
        ///     Gets the <see cref="IConsumerTransactionManager" /> that is handling the current pipeline transaction.
        /// </summary>
        public IConsumerTransactionManager TransactionManager
        {
            get
            {
                if (_transactionManager == null)
                    throw new InvalidOperationException("The transaction manager is not initialized.");

                return _transactionManager;
            }

            internal set
            {
                if (_transactionManager != null)
                    throw new InvalidOperationException("The transaction manager is already initialized.");

                _transactionManager = value;
            }
        }

        /// <summary>
        ///     Gets or sets the envelopes containing the messages being processed.
        /// </summary>
        public IRawInboundEnvelope Envelope { get; set; }

        /// <summary>
        ///     Gets or sets the <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }
    }
}
