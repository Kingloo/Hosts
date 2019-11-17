namespace Hosts

module Program =

    open System
    open System.IO
    
    open Logger
    open DnsServers
    open HostNameSources

#if DEBUG
    let directory = Environment.CurrentDirectory
#else
    let directory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
#endif
    
    let additionalHostsFilePath = Path.Combine (directory, "addedHosts.txt")
    let excludedHostsFilePath = Path.Combine (directory, "excludedHosts.txt")

    let loadLinesFromFilePath (filePath: string) : string list =
        try
            let lines =
                File.ReadAllLines filePath
                    |> Array.where (fun line -> not (line.StartsWith("#", StringComparison.OrdinalIgnoreCase)))
                    |> Array.where (fun line -> line |> (String.IsNullOrWhiteSpace >> not))
            printError (sprintf "loaded %i lines from %s" lines.Length filePath)
            lines |> List.ofArray
        with
            | ex ->
                printError (sprintf "loading from file failed: %s: %s" (ex.GetType().FullName) ex.Message)
                List.empty

    type ExitCodes =
        | Success = 0
        | ServerTypeSwitchMissing = -1
        | ServerTypeUnknown = -2

    [<EntryPoint>]
    let main args =
        match determineServerType args with
            | Unknown ->
                printError "! server type unknown !"
                int ExitCodes.ServerTypeUnknown
            | Missing ->
                printError "! no \"-type\" switch !"
                int ExitCodes.ServerTypeSwitchMissing
            | serverType ->
                let additionalHostNames = loadLinesFromFilePath additionalHostsFilePath
                let excludedHostNames = loadLinesFromFilePath excludedHostsFilePath
                hostNameSources
                    |> getHostNamesFromAllSources
                    |> List.append additionalHostNames
                    |> List.except excludedHostNames
                    |> List.distinct
                    |> List.map (DnsServerFormatter serverType)
                    |> printLines Console.Out
                int ExitCodes.Success