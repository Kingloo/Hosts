using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using hosts.DnsServerTargets;
using hosts.HostSources;
using hosts.Common;

namespace hosts
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            string workingDirectory = GetFromCmdLine(args, "-outputDir", Environment.CurrentDirectory);
            string bindBlackHoleFilePath = GetFromCmdLine(args, "-bindBlackHoleFile", "/etc/bind/db.poison");
            
            IEnumerable<Domain> additions = await LoadAdditionsAsync(workingDirectory).ConfigureAwait(false);
            IEnumerable<Domain> downloaded = await DownloadAndCreateDomainsAsync().ConfigureAwait(false);
            IEnumerable<Domain> exclusions = await LoadExclusionsAsync(workingDirectory).ConfigureAwait(false);

            List<Domain> domains = additions
                .Union(downloaded)
                .Except(exclusions)
                .ToList();

            Console.WriteLine($"{domains.Count} domains to block");

            var serverTargets = new DnsServerTargetBase[]
            {
                new Bind(bindBlackHoleFilePath)
                {
                    Domains = domains,
                    File = new FileInfo(Path.Combine(workingDirectory, "bind.txt"))
                },
                new Unbound()
                {
                    Domains = domains,
                    File = new FileInfo(Path.Combine(workingDirectory, "unbound.txt"))
                }/*,
                new Windows()
                {
                    Domains = domains,
                    File = new FileInfo(Path.Combine(workingDirectory, "hosts"))
                }*/
            };

            await WriteFilesAsync(serverTargets).ConfigureAwait(false);

#if DEBUG
            Console.ReadKey();
#endif

            return 0;
        }

        private static string GetFromCmdLine(string[] args, string value, string defaultValue)
        {
            string toRet = defaultValue;
            
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(value))
                {
                    toRet = args[i + 1];
                }
            }

            Console.WriteLine($"{value}: {toRet}");

            return toRet;
        }

        private static async Task<IEnumerable<Domain>> LoadAdditionsAsync(string workingDirectory)
        {
            FileInfo addedHosts = new FileInfo(Path.Combine(workingDirectory, "addedHosts.txt"));

            Console.Write("loading custom additions... ");

            if (!addedHosts.Exists)
            {
                Console.WriteLine("file not found");

                return Enumerable.Empty<Domain>();
            }

            string[] lines = await FileSystem.GetLinesAsync(addedHosts).ConfigureAwait(false);

            var domains = new List<Domain>();

            foreach (string line in lines)
            {
                if (Domain.TryCreate(line, out Domain domain))
                {
                    domains.Add(domain);
                }
            }

            Console.WriteLine($"success - {domains.Count} domain(s) added");

            return domains;
        }

        private static async Task<IEnumerable<Domain>> DownloadAndCreateDomainsAsync()
        {
            var downloadTasks = new List<Task<Domain[]>>();

            Console.Write("downloading... ");

            foreach (HostSourceBase each in HostSourceBase.AllSources())
            {
                Task<Domain[]> task = Task.Run(each.GetDomainsAsync);

                downloadTasks.Add(task);
            }

            await Task.WhenAll(downloadTasks).ConfigureAwait(false);

            Console.WriteLine("finished!");

            return (from task in downloadTasks
                    let domainArray = task.Result
                    from domain in domainArray
                    select domain)
                    .Distinct();
        }

        private static async Task<IEnumerable<Domain>> LoadExclusionsAsync(string workingDirectory)
        {
            FileInfo excludedHosts = new FileInfo(Path.Combine(workingDirectory, "excludedHosts.txt"));

            Console.Write("loading exclusions... ");

            if (!excludedHosts.Exists)
            {
                Console.WriteLine("file not found");

                return Enumerable.Empty<Domain>();
            }

            string[] lines = await FileSystem.GetLinesAsync(excludedHosts).ConfigureAwait(false);

            var domains = new List<Domain>();

            foreach (string line in lines)
            {
                if (Domain.TryCreate(line, out Domain domain))
                {
                    domains.Add(domain);
                }
            }

            Console.WriteLine($"success - {domains.Count} domain(s) excluded");

            return domains;
        }

        private static Task WriteFilesAsync(DnsServerTargetBase[] serverTargets)
        {
            if (serverTargets.Length == 0)
            {
                return Task.CompletedTask;
            }

            var tasks = new List<Task>();

            foreach (var each in serverTargets)
            {
                // don't add WriteFileAsync.ConfAwait
                // the message at the end of WriteFileAsync isn't printed to console
                Task task = Task.Run(() => WriteFileAsync(each)); // WORKS!

                tasks.Add(task);
            }

            return Task.WhenAll(tasks);
        }

        private static async Task WriteFileAsync(DnsServerTargetBase serverTarget)
        {
            if (serverTarget == null) { throw new ArgumentNullException(nameof(serverTarget)); }

            try
            {
                await FileSystem.WriteLinesAsync(
                    serverTarget.File,
                    serverTarget.Emit().ToList(),
                    FileMode.Append)
                        .ConfigureAwait(false);
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"failure! unauthorized access when trying to create {serverTarget.File.FullName}");
            }

            Console.WriteLine($"wrote file for {serverTarget.ToString()} to {serverTarget.File.FullName}");
        }
    }
}
