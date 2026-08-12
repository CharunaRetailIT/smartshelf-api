using System;
using System.Collections.Generic;
using System.Linq;

namespace TERMS_LOYALTY_API.Services.Providers
{
    public interface IEslProviderFactory
    {
        // Throws NotSupportedException if no provider is registered for the given brand code.
        IEslProvider GetProvider(string brandCode);
    }

    // Adding a new brand later = implement IEslProvider and add it to the list
    // passed into this factory's constructor (registered in Startup.cs) - no
    // other code needs to change.
    public class EslProviderFactory : IEslProviderFactory
    {
        private readonly Dictionary<string, IEslProvider> _providersByBrandCode;

        public EslProviderFactory(IEnumerable<IEslProvider> providers)
        {
            _providersByBrandCode = providers.ToDictionary(
                p => p.BrandCode,
                p => p,
                StringComparer.OrdinalIgnoreCase);
        }

        public IEslProvider GetProvider(string brandCode)
        {
            if (string.IsNullOrWhiteSpace(brandCode) || !_providersByBrandCode.TryGetValue(brandCode, out var provider))
            {
                throw new NotSupportedException($"No ESL provider registered for brand '{brandCode}'");
            }

            return provider;
        }
    }
}
