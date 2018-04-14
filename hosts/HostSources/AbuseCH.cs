using System;

namespace hosts.HostSources
{
    public class AbuseCH : HostSourceBase
    {
        public AbuseCH()
            : base(new Uri("https://ransomwaretracker.abuse.ch/downloads/RW_DOMBL.txt"))
        { }

        protected override bool TryCreate(string line, out Domain domain)
        {
            if (line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                domain = null;
                return false;
            }
            
            if (!Domain.TryCreate(line, out Domain d))
            {
                domain = d;
                return false;
            }

            domain = d;
            return true;
        }
    }
}
