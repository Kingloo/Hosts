using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using hosts.Common;
using hosts.DnsServerTargets;
using hosts.HostSources;

namespace hosts
{
    public partial class Program
    {
        private static DirectoryInfo workingDirectory = new DirectoryInfo(Environment.CurrentDirectory);

        public static async Task<int> Main(string[] args)
        {
            DnsServerType serverType = GetServerType(GetFromCmdLine("-type"));

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

            await WriteServerTargetAsync(serverTarget).ConfigureAwait(false);

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
                    return args[i + 1];
                }
            }

            return null;
        }

        private static DnsServerType GetServerType(string value)
        {
            return Enum.TryParse(
                typeof(DnsServerType),
                value,
                ignoreCase: true,
                out object serverType)
                    ? (DnsServerType)serverType
                    : DnsServerType.None;
        }

        private static async Task<List<Domain>> GetDomainsAsync()
        {
            var tasks = new List<Task<IEnumerable<Domain>>>
            {
                Task.Run(() => LoadDomainsAsync(new FileInfo(Path.Combine(workingDirectory.FullName, "addedHosts.txt")))),
                Task.Run(DownloadDomainsAsync),
                Task.Run(() => LoadDomainsAsync(new FileInfo(Path.Combine(workingDirectory.FullName, "excludedHosts.txt"))))
            };

            await Task.WhenAll();

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

        private static async Task WriteServerTargetAsync(DnsServerTargetBase serverTarget)
        {
            using (TextWriter tw = Console.Out)
            {
#if DEBUG
                foreach (string line in serverTarget.Emit().Take(20))
#else
                foreach (string line in serverTarget.Emit())
#endif
                {
                    await tw.WriteLineAsync(line).ConfigureAwait(false);
                }
            }
        }
    }
}
