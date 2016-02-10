﻿namespace Merchello.Providers.Payment.Braintree.Provider
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Merchello.Core.Gateways;
    using Merchello.Core.Gateways.Payment;
    using Merchello.Core.Models;
    using Merchello.Core.Services;
    using Merchello.Plugin.Payments.Braintree;
    using Merchello.Providers.Payment.Braintree.Services;

    using Umbraco.Core.Logging;

    /// <summary>
    /// The BrainTree Payment Gateway Provider.
    /// </summary>
    [GatewayProviderActivation("D143E0F6-98BB-4E0A-8B8C-CE9AD91B0969", "BrainTree Payment Provider", "BrainTree Payment Provider")]
    [GatewayProviderEditor("BrainTree Configuration", "~/App_Plugins/MerchelloPaymentProviders/views/dialogs/braintree.providersettings.html")]
    public class BraintreePaymentGatewayProvider : PaymentGatewayProviderBase, IBraintreePaymentGatewayProvider
    {
        #region AvailableResources

        /// <summary>
        /// The available resources.
        /// </summary>
        internal static readonly IEnumerable<IGatewayResource> AvailableResources = new List<IGatewayResource>
        {
            new GatewayResource(Constants.PaymentCodes.Transaction, "Standard Transaction"),
            new GatewayResource(Constants.PaymentCodes.VaultTransaction, "Vault Transaction"),
            new GatewayResource(Constants.PaymentCodes.RecordSubscriptionTransaction, "Record of Subscription Transaction")
        };


        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BraintreePaymentGatewayProvider"/> class.
        /// </summary>
        /// <param name="gatewayProviderService">
        /// The gateway provider service.
        /// </param>
        /// <param name="gatewayProviderSettings">
        /// The gateway provider settings.
        /// </param>
        /// <param name="runtimeCacheProvider">
        /// The runtime cache provider.
        /// </param>
        public BraintreePaymentGatewayProvider(IGatewayProviderService gatewayProviderService, IGatewayProviderSettings gatewayProviderSettings, Umbraco.Core.Cache.IRuntimeCacheProvider runtimeCacheProvider)
            : base(gatewayProviderService, gatewayProviderSettings, runtimeCacheProvider)
        {
        }


        /// <summary>
        /// Returns a list of unassigned <see cref="IGatewayResource"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="IEnumerable{IGatewayResource}"/>.
        /// </returns>
        public override IEnumerable<IGatewayResource> ListResourcesOffered()
        {
            return AvailableResources.Where(x => this.PaymentMethods.All(y => y.PaymentCode != x.ServiceCode));
        }

        /// <summary>
        /// Creates a <see cref="IPaymentGatewayMethod"/> for this provider.
        /// </summary>
        /// <param name="gatewayResource">
        /// The gateway resource.
        /// </param>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="description">
        /// The description.
        /// </param>
        /// <returns>
        /// The <see cref="IPaymentGatewayMethod"/>.
        /// </returns>
        public override IPaymentGatewayMethod CreatePaymentMethod(IGatewayResource gatewayResource, string name, string description)
        {
            var available = this.ListResourcesOffered().FirstOrDefault(x => x.ServiceCode == gatewayResource.ServiceCode);

            if (available == null)
            {
                var error = new InvalidOperationException("The GatewayResource has already been assigned.");

                LogHelper.Error<BraintreePaymentGatewayProvider>("GatewayResource has alread been assigned", error);

                throw error;
            }

            var attempt = this.GatewayProviderService.CreatePaymentMethodWithKey(this.GatewayProviderSettings.Key, name, description, available.ServiceCode);

            if (attempt.Success)
            {
                this.PaymentMethods = null;

                switch (available.ServiceCode)
                {
                    case Constants.PaymentCodes.VaultTransaction:
                        return new BraintreeVaultTransactionPaymentGatewayMethod(this.GatewayProviderService, attempt.Result, this.GetBraintreeApiService());
                    
                    case Constants.PaymentCodes.RecordSubscriptionTransaction:
                        return new BraintreeSubscriptionRecordPaymentMethod(this.GatewayProviderService, attempt.Result, this.GetBraintreeApiService());

                    default:
                        return new BraintreeStandardTransactionPaymentGatewayMethod(this.GatewayProviderService, attempt.Result, this.GetBraintreeApiService());
                }   
            }

            LogHelper.Error<BraintreePaymentGatewayProvider>(string.Format("Failed to create a payment method name: {0}, description {1}, paymentCode {2}", name, description, available.ServiceCode), attempt.Exception);

            throw attempt.Exception;
        }

        /// <summary>
        /// Gets a <see cref="IBraintreeStandardTransactionPaymentGatewayMethod"/> by it's unique key.
        /// </summary>
        /// <param name="paymentMethodKey">
        /// The payment method key.
        /// </param>
        /// <returns>
        /// The <see cref="IPaymentGatewayMethod"/>.
        /// </returns>        
        public override IPaymentGatewayMethod GetPaymentGatewayMethodByKey(Guid paymentMethodKey)
        {
            var paymentMethod = this.PaymentMethods.FirstOrDefault(x => x.Key == paymentMethodKey);

            if (paymentMethod != null)
            {
                switch (paymentMethod.PaymentCode)
                {
                    case Constants.PaymentCodes.VaultTransaction:
                        return new BraintreeVaultTransactionPaymentGatewayMethod(this.GatewayProviderService, paymentMethod, this.GetBraintreeApiService());

                    case Constants.PaymentCodes.RecordSubscriptionTransaction:
                        return new BraintreeSubscriptionRecordPaymentMethod(this.GatewayProviderService, paymentMethod, this.GetBraintreeApiService());

                    default:
                        return new BraintreeStandardTransactionPaymentGatewayMethod(this.GatewayProviderService, paymentMethod, this.GetBraintreeApiService());
                }
            }

            var error = new NullReferenceException("Failed to find BraintreePaymentGatewayMethod with key specified");
            LogHelper.Error<BraintreePaymentGatewayProvider>("Failed to find BraintreePaymentGatewayMethod with key specified", error);
            throw error;            
        }

        /// <summary>
        /// Gets a <see cref="IPaymentGatewayMethod"/> by it's payment code
        /// </summary>
        /// <param name="paymentCode">The payment code of the <see cref="IPaymentGatewayMethod"/></param>
        /// <returns>A <see cref="IPaymentGatewayMethod"/></returns>
        public override IPaymentGatewayMethod GetPaymentGatewayMethodByPaymentCode(string paymentCode)
        {
            var paymentMethod = this.PaymentMethods.FirstOrDefault(x => x.PaymentCode == paymentCode);

            if (paymentMethod != null)
            {
                switch (paymentMethod.PaymentCode)
                {
                    case Constants.PaymentCodes.VaultTransaction:
                        return new BraintreeVaultTransactionPaymentGatewayMethod(this.GatewayProviderService, paymentMethod, this.GetBraintreeApiService());

                    case Constants.PaymentCodes.RecordSubscriptionTransaction:
                        return new BraintreeSubscriptionRecordPaymentMethod(this.GatewayProviderService, paymentMethod, this.GetBraintreeApiService());

                    default:
                        return new BraintreeStandardTransactionPaymentGatewayMethod(this.GatewayProviderService, paymentMethod, this.GetBraintreeApiService());
                }
            }

            var error = new NullReferenceException("Failed to find BraintreePaymentGatewayMethod with key specified");
            LogHelper.Error<BraintreePaymentGatewayProvider>("Failed to find BraintreePaymentGatewayMethod with key specified", error);
            throw error;  
        }

        private IBraintreeApiService GetBraintreeApiService()
        {
            return new BraintreeApiService(this.GatewayProviderSettings.ExtendedData.GetBrainTreeProviderSettings());
        }
    }
}