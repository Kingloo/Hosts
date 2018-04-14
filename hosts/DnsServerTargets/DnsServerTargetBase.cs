using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace hosts.DnsServerTargets
{
    public abstract class DnsServerTargetBase
    {
        protected string defaultDirectory = Environment.CurrentDirectory;

        protected abstract StringBuilder Sb { get; }
        public abstract IEnumerable<Domain> Domains { get; set; }
        public abstract FileInfo File { get; set; }

        protected abstract string Format(Domain domain);

        public IEnumerable<string> Emit()
            => Domains.Any() ? Emit(Domains) : Enumerable.Empty<string>();

        public IEnumerable<string> Emit(IEnumerable<Domain> domains)
        {
            foreach (Domain each in domains)
            {
                yield return Format(each);
            }
        }

        public override string ToString() => GetType().Name;
    }
}
