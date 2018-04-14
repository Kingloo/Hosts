using System;

namespace hosts.HostSources
{
    public class MVPS : HostSourceBase
    {
        public MVPS()
            : base(new Uri("http://winhelp2002.mvps.org/hosts.txt"))
        { }
        
        protected override bool TryCreate(string line, out Domain domain)
        {
            if (!line.StartsWith("0.0.0.0", StringComparison.OrdinalIgnoreCase))
            {
                domain = null;
                return false;
            }

            // --[0]-- ----[1]---- ----[2]----
            // 0.0.0.0 example.com # a comment
            string domainName = line
                .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                [1];
            
            if (!Domain.TryCreate(domainName, out Domain d))
            {
                domain = null;
                return false;
            }
            
            domain = d;
            return true;
        }
    }
}
