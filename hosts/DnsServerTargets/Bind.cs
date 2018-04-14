using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace hosts.DnsServerTargets
{
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

        private FileInfo _file = null;
        public override FileInfo File
        {
            get => _file == null ? new FileInfo(Path.Combine(defaultDirectory, "bind.txt")) : _file;
            set => _file = value;
        }

        public Bind(string blackHoleZoneFilePath)
        {
            if (String.IsNullOrWhiteSpace(blackHoleZoneFilePath))
            {
                throw new ArgumentNullException(nameof(blackHoleZoneFilePath));
            }

            _blackHoleZoneFilePath = blackHoleZoneFilePath;
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
            // zone "example.com" { type master; file "/etc/bind/blackHoleZoneFile"; };

            return Sb.ToString();
        }
    }
}
