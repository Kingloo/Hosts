using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace hosts.DnsServerTargets
{
    /// <summary>
    /// Unbound DNS server.
    /// </summary>
    public class Unbound : DnsServerTargetBase
    {
        private readonly StringBuilder _sb = new StringBuilder();
        protected override StringBuilder Sb => _sb;

        private IEnumerable<Domain> _domains = Enumerable.Empty<Domain>();
        public override IEnumerable<Domain> Domains
        {
            get => _domains;
            set => _domains = value;
        }

        protected override string Format(Domain domain)
        {
            Sb.Clear();

            Sb.Append("local-zone: \"");
            Sb.Append(domain.DomainName);
            Sb.Append("\" inform_deny.");

            // e.g.
            // local-zone: "example.com" inform_deny.

            return Sb.ToString();
        }
    }
}
