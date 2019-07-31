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

    let loadLinesFromFilePath (filePath: string) : seq<string> =
        try
            let lines =
                File.ReadAllLines filePath
                    |> Array.where (fun line -> not (line.StartsWith("#", StringComparison.OrdinalIgnoreCase)))
                    |> Array.where (fun line -> not (String.IsNullOrWhiteSpace(line)))
            printError (sprintf "loaded %i lines from %s" lines.Length filePath)
            lines :> seq<string>
        with
            | ex ->
                printError (sprintf "loading from file (%s) failed: %s: %s" filePath (ex.GetType().FullName) ex.Message)
                Seq.empty

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
                    |> Seq.append additionalHostNames
                    |> Seq.except excludedHostNames
                    |> Seq.distinct
                    |> Seq.map (DnsServerFormatter serverType)
                    |> printLines Console.Out
                int ExitCodes.Success