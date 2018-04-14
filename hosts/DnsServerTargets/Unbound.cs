using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace hosts.DnsServerTargets
{
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

        private FileInfo _file = null;
        public override FileInfo File
        {
            get => _file == null ? new FileInfo(Path.Combine(defaultDirectory, "unbound.txt")) : _file;
            set => _file = value;
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
