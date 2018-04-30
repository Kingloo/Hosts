using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace hosts.DnsServerTargets
{
    public abstract class DnsServerTargetBase
    {
        protected abstract StringBuilder Sb { get; }
        public abstract IEnumerable<Domain> Domains { get; set; }

        protected abstract string Format(Domain domain);

        public IEnumerable<string> Emit()
        {
            if (!Domains.Any())
            {
                yield break;
            }

            foreach (Domain each in Domains)
            {
                yield return Format(each);
            }
        }

        public static DnsServerType ParseServerType(string value)
        {
            return Enum.TryParse(
                typeof(DnsServerType),
                value,
                ignoreCase: true,
                out object serverType)
                    ? (DnsServerType)serverType
                    : DnsServerType.None;
        }

        public override string ToString() => GetType().Name;
    }
}
