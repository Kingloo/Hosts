using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace hosts.DnsServerTargets
{
    /// <summary>
    /// Berkeley Internet Name Domain (BIND) DNS server.
    /// </summary>
    public class Bind : DnsServerTargetBase
    {
        private readonly string _blackHoleZoneFilePath = string.Empty;
        public string BlackHoleZoneFilePath => _blackHoleZoneFilePath;

        private readonly StringBuilder _sb = new StringBuilder();
        protected override StringBuilder Sb => _sb;

        private IEnumerable<Domain> _domains = Enumerable.Empty<Domain>();
        public override IEnumerable<Domain> Domains
        {
            get => _domains;
            set => _domains = value;
        }

        public Bind(string blackHoleZoneFilePath)
        {
            if (String.IsNullOrEmpty(blackHoleZoneFilePath))
            {
                _blackHoleZoneFilePath = "/etc/bind/db.poison";
            }
            else
            {
                _blackHoleZoneFilePath = blackHoleZoneFilePath;
            }
        }
        
        protected override string Format(Domain domain)
        {
            Sb.Clear();
            
            Sb.Append("zone \"");
            Sb.Append(domain.DomainName);
            Sb.Append("\" { type master; file \"");
            Sb.Append(BlackHoleZoneFilePath);
            Sb.Append("\"; };");

            // e.g.
            // zone "example.com" { type master; file "/etc/bind/db.poison"; };

            return Sb.ToString();
        }
    }
}
