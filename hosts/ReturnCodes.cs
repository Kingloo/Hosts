using System;

namespace hosts
{
    public partial class Program
    {
        private enum ReturnCodes
        {
            None = Int32.MinValue,
            Success = 0,
            BadServerType = -1,
            ServerTargetNull = -2
        }

        //private static Task WriteFilesAsync(DnsServerTargetBase[] serverTargets)
        //{
        //    if (serverTargets.Length == 0)
        //    {
        //        return Task.CompletedTask;
        //    }

        //    var tasks = new List<Task>();

        //    foreach (var each in serverTargets)
        //    {
        //        // don't add WriteFileAsync.ConfAwait
        //        // the message at the end of WriteFileAsync isn't printed to console
        //        Task task = Task.Run(() => WriteFileAsync(each)); // WORKS!

        //        tasks.Add(task);
        //    }

        //    return Task.WhenAll(tasks);
        //}

        //private static async Task WriteFileAsync(DnsServerTargetBase serverTarget)
        //{
        //    if (serverTarget == null) { throw new ArgumentNullException(nameof(serverTarget)); }

        //    try
        //    {
        //        await FileSystem.WriteLinesAsync(
        //            serverTarget.File,
        //            //serverTarget.Emit(),
        //            serverTarget.Domains.Select(domain => domain.DomainName),
        //            FileMode.Append)
        //                .ConfigureAwait(false);
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        Console.Out.WriteLine($"failure! unauthorized access when trying to create {serverTarget.File.FullName}");
        //    }

        //    Console.Out.WriteLine($"wrote file for {serverTarget.ToString()} to {serverTarget.File.FullName}");
        //}

        //private static string GetFromCmdLine(string[] args, string value, string defaultValue)
        //{
        //    string toRet = defaultValue;

        //    for (int i = 0; i < args.Length; i++)
        //    {
        //        if (args[i].Equals(value))
        //        {
        //            toRet = args[i + 1];
        //        }
        //    }



        //    return toRet;
        //}

        //var serverTargets = new DnsServerTargetBase[]
        //{
        //    new Bind(bindBlackHoleFilePath)
        //    {
        //        Domains = domains,
        //        File = new FileInfo(Path.Combine(workingDirectory, "bind.txt"))
        //    },
        //    new Unbound()
        //    {
        //        Domains = domains,
        //        File = new FileInfo(Path.Combine(workingDirectory, "unbound.txt"))
        //    }/*,
        //    new Windows()
        //    {
        //        Domains = domains,
        //        File = new FileInfo(Path.Combine(workingDirectory, "hosts"))
        //    }*/
        //};

        //await WriteFilesAsync(serverTargets).ConfigureAwait(false);
        //await WriteFileToStdOut(serverTargets[0]).ConfigureAwait(false);

        //string bindBlackHoleFilePath = GetFromCmdLine("-bindBlackHoleFile", "/etc/bind/db.poison") ?? "/etc/bind/"
    }
}
