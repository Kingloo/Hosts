using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace hosts.DnsServerTargets
{
    /// <summary>
    /// Windows-format HOSTS file.
    /// </summary>
    public class Windows : DnsServerTargetBase
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
            get => _file == null ? new FileInfo(Path.Combine(defaultDirectory, "hosts")) : _file;
            set => _file = value;
        }

        protected override string Format(Domain domain)
        {
            Sb.Clear();

            Sb.Append(domain.BlackHole);
            Sb.Append(" ");
            Sb.Append(domain.DomainName);

            // e.g.
            // 123.123.123.123 example.com

            return Sb.ToString();
        }
    }
}
