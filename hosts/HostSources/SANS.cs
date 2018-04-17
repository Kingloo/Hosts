using System;

namespace hosts.HostSources
{
    public class SANS : HostSourceBase
    {
        public SANS(Uri uri)
            : base(uri)
        { }

        protected override bool TryCreate(string line, out Domain domain)
        {
            if (line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                domain = null;
                return false;
            }

            if (line.StartsWith("site", StringComparison.OrdinalIgnoreCase))
            {
                domain = null;
                return false;
            }

            if (!Domain.TryCreate(line, out Domain d))
            {
                domain = null;
                return false;
            }

            domain = d;
            return true;
        }
    }
}
