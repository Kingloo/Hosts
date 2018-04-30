using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using hosts.Common;
using hosts.DnsServerTargets;
using hosts.HostSources;

namespace hosts
{
    public static class Program
    {
        private static DirectoryInfo workingDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

        public static async Task<int> Main(string[] args)
        {
            DnsServerType serverType = DnsServerTargetBase.ParseServerType(GetFromCmdLine("-type"));

            if (serverType == DnsServerType.None)
            {
                return (int)ReturnCodes.BadServerType;
            }

            List<Domain> domains = await GetDomainsAsync().ConfigureAwait(false);

            DnsServerTargetBase serverTarget = null;

            switch (serverType)
            {
                case DnsServerType.Bind:
                    string blackHoleZoneFile = GetFromCmdLine("-bindBlackHoleZoneFile") ?? "/etc/bind/db.poison";
                    serverTarget = new Bind(blackHoleZoneFile) { Domains = domains };
                    break;
                case DnsServerType.Unbound:
                    serverTarget = new Unbound { Domains = domains };
                    break;
                case DnsServerType.Windows:
                    serverTarget = new Windows { Domains = domains };
                    break;
                default:
                    return (int)ReturnCodes.ServerTargetNull;
            }

            await WriteServerTargetAsync(serverTarget, Console.OpenStandardOutput());

#if DEBUG
            Console.ReadKey();
#endif

            return (int)ReturnCodes.Success;
        }

        private static string GetFromCmdLine(string value)
        {
            string[] args = Environment.GetCommandLineArgs();
            
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                {
                    if (i < args.GetUpperBound(0)) // in case there isn't actually another arg
                    {
                        return args[i + 1];
                    }
                }
            }

            return null;
        }

        private static async Task<List<Domain>> GetDomainsAsync()
        {
            var tasks = new List<Task<IEnumerable<Domain>>>
            {
                Task.Run(() => LoadDomainsAsync(new FileInfo(Path.Combine(workingDirectory.FullName, "addedHosts.txt")))),
                Task.Run(DownloadDomainsAsync),
                Task.Run(() => LoadDomainsAsync(new FileInfo(Path.Combine(workingDirectory.FullName, "excludedHosts.txt"))))
            };

            await Task.WhenAll().ConfigureAwait(false);

            return tasks[0].Result
               .Union(tasks[1].Result)
               .Except(tasks[2].Result)
               .ToList();
        }

        private static async Task<IEnumerable<Domain>> LoadDomainsAsync(FileInfo file)
        {
            if (!file.Exists) { return Enumerable.Empty<Domain>(); }

            string[] lines = await FileSystem.GetLinesAsync(file).ConfigureAwait(false);

            var domains = new List<Domain>();

            foreach (string line in lines)
            {
                if (Domain.TryCreate(line, out Domain domain))
                {
                    domains.Add(domain);
                }
            }

            return domains;
        }

        private static async Task<IEnumerable<Domain>> DownloadDomainsAsync()
        {
            var downloadTasks = new List<Task<Domain[]>>();

            foreach (HostSourceBase each in HostSourceBase.AllSources())
            {
                Task<Domain[]> task = Task.Run(each.GetDomainsAsync);

                downloadTasks.Add(task);
            }

            await Task.WhenAll(downloadTasks).ConfigureAwait(false);

            return (from task in downloadTasks
                    let domainArray = task.Result
                    from domain in domainArray
                    select domain)
                        .Distinct();
        }

        private static async Task WriteServerTargetAsync(DnsServerTargetBase serverTarget, Stream stream)
        {
            using (StreamWriter sw = new StreamWriter(stream))
            {
#if DEBUG
                foreach (string line in serverTarget.Emit().Take(20))
#else
                foreach (string line in serverTarget.Emit())
#endif
                {
                    await sw.WriteLineAsync(line).ConfigureAwait(false);
                }
            }
        }
    }
}
