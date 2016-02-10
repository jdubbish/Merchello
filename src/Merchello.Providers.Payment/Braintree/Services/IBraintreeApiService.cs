﻿namespace Merchello.Providers.Payment.Braintree.Services
{
    using Merchello.Providers.Payment.Braintree.Models;

    /// <summary>
    /// Defines the <see cref="BraintreeApiService"/>.
    /// </summary>
    public interface IBraintreeApiService
    {
        /// <summary>
        /// Gets the <see cref="BraintreeProviderSettings"/>.
        /// </summary>
        BraintreeProviderSettings BraintreeProviderSettings { get; }

        /// <summary>
        /// Gets the <see cref="IBraintreeCustomerApiService"/>.
        /// </summary>
        IBraintreeCustomerApiService Customer { get; }

        /// <summary>
        /// Gets the <see cref="IBraintreePaymentMethodApiService"/>.
        /// </summary>
        IBraintreePaymentMethodApiService PaymentMethod { get; }

        /// <summary>
        /// Gets the <see cref="IBraintreeSubscriptionApiService"/>.
        /// </summary>
        IBraintreeSubscriptionApiService Subscription { get; }

        /// <summary>
        /// Gets the <see cref="IBraintreeTransactionApiService"/>.
        /// </summary>
        IBraintreeTransactionApiService Transaction { get; }

        /// <summary>
        /// Gets the <see cref="IBraintreeWebhooksApiService"/>
        /// </summary>
        IBraintreeWebhooksApiService Webhook { get; }
    }
}