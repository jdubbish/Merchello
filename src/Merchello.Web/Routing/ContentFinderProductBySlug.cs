﻿namespace Merchello.Web.Routing
{
    using System.Linq;

    using Merchello.Core;
    using Merchello.Core.Configuration;
    using Merchello.Web.Models.VirtualContent;

    using Umbraco.Core;
    using Umbraco.Web.Routing;

    /// <summary>
    /// Responsible for finding ProductContent by it's slug.
    /// </summary>
    public class ContentFinderProductBySlug : IContentFinder
    {
        /// <summary>
        /// The <see cref="MerchelloHelper"/>
        /// </summary>
        private static readonly MerchelloHelper Merchello = new MerchelloHelper(MerchelloContext.Current.Services);

        /// <summary>
        /// The factory.
        /// </summary>
        private static readonly ProductContentFactory Factory = new ProductContentFactory();

        /// <summary>
        /// Tries to find a <see cref="IProductContent"/> by it's unique slug.
        /// </summary>
        /// <param name="contentRequest">
        /// The content request.
        /// </param>
        /// <returns>
        /// A value indicating whether or not the product was found by slug.
        /// </returns>        
        public bool TryFindContent(PublishedContentRequest contentRequest)
        {
            if (contentRequest.Uri.AbsolutePath == "/") return false;

            var slug = PrepareSlug(contentRequest);

            if (slug.IsNullOrWhiteSpace()) return false;

            // This may have a db fallback so we want this content finder to happen after Umbraco's content finders.
            var display = Merchello.Query.Product.GetBySlug(slug);

            if (display == null) return false;

            // products marked as not available cannot be rendered
            if (!display.Available) return false;

            // ensure their is a "renderable" detached content 
            var cultureName = contentRequest.Culture.Name;
            if (display.DetachedContents.FirstOrDefault(x => x.CultureName == cultureName && x.CanBeRendered) == null) return false;
           
            contentRequest.PublishedContent = Factory.BuildContent(display, contentRequest.Culture.Name);
            return true;
        }

        /// <summary>
        /// The prepare slug.
        /// </summary>
        /// <param name="contentRequest">
        /// The content request.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string PrepareSlug(PublishedContentRequest contentRequest)
        {
            var slug = contentRequest.Uri.AbsolutePath.EnsureNotStartsOrEndsWith('/');

            // Check if there were any overrides to the slug in the merchello.config
            var prefix = MerchelloConfiguration.Current.GetProductSlugCulturePrefix(contentRequest.Culture.Name);

            if (prefix.IsNullOrWhiteSpace()) return slug;

            // enforce the prefix is present in the slug
            return !slug.StartsWith(prefix) ? 
                string.Empty : 
                slug.Replace(prefix, string.Empty).EnsureNotStartsOrEndsWith('/');
        }
    }
}