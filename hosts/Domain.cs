using System;
using System.Net;

namespace hosts
{
    public class Domain : IEquatable<Domain>
    {
        private static IPAddress defaultBlackHole = IPAddress.Parse("0.0.0.0");

        private readonly string _domainName = string.Empty;
        public string DomainName => _domainName;

        private readonly IPAddress _blackHole = IPAddress.None;
        public IPAddress BlackHole => _blackHole;

        private Domain(string domainName)
            : this(domainName, defaultBlackHole)
        { }

        private Domain(string domainName, IPAddress blackHole)
        {
            if (String.IsNullOrWhiteSpace(domainName))
            {
                throw new ArgumentNullException(nameof(domainName));
            }

            _domainName = domainName;
            _blackHole = blackHole;
        }

        public static bool TryCreate(string domainName, out Domain domain)
        {
            if (!Uri.TryCreate($"https://{domainName}", UriKind.Absolute, out Uri uri))
            {
                domain = null;
                return false;
            }

            domain = new Domain(uri.DnsSafeHost);
            return true;
        }
        
        public bool Equals(Domain other)
        {
            if (other == null) { return false; }

            return DomainName.Equals(other.DomainName);
        }

        public override bool Equals(object obj) => Equals((Domain)obj);

        public override int GetHashCode() => DomainName.GetHashCode();
    }
}
