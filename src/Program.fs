namespace Hosts

module Program =

    open System
    open System.IO
    open System.Security
    
    open Logger
    open DnsServers
    open DomainSources

#if DEBUG
    let directory = Environment.CurrentDirectory
#else
    let directory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
#endif
    
    let addedHostsFilePath = Path.Combine (directory, "addedHosts.txt")
    let excludedHostsFilePath = Path.Combine (directory, "excludedHosts.txt")

    let loadInputLines (filePath: string) : seq<string> =
        try
            let lines = File.ReadAllLines filePath |> Array.where (fun line -> not (line.StartsWith("#")))
            printError (sprintf "loaded %i lines from %s" lines.Length filePath)
            lines :> seq<string>
        with
            | :? FileNotFoundException -> printError (sprintf "%s was not found" filePath ) ; Seq.empty
            | :? DirectoryNotFoundException -> printError "directory not found" ; Seq.empty
            | :? PathTooLongException -> printError (sprintf "%s exceeded path character limit" filePath ) ; Seq.empty
            | :? NotSupportedException -> printError (sprintf "%s is an unsupported format" filePath ) ; Seq.empty
            | :? SecurityException -> printError (sprintf "you do not have the required permissions for %s" filePath ) ; Seq.empty
            | :? IOException -> printError (sprintf "an i/o error occurred while opening %s" filePath ) ; Seq.empty

    type ExitCodes =
        | Success = 0
        | ErrorServerTypeSwitchMissing = -1
        | ErrorServerTypeUnknown = -2

    [<EntryPoint>]
    let main args =
        match determineServerType args with
            | Unknown ->
                printError "! server type unknown !"
                int ExitCodes.ErrorServerTypeUnknown
            | Missing ->
                printError "! no \"-type\" switch !"
                int ExitCodes.ErrorServerTypeSwitchMissing
            | serverType ->
                let addedHosts = loadInputLines addedHostsFilePath
                let excludedHosts = loadInputLines excludedHostsFilePath
                domainSources
                    |> getAllSourceHosts
                    |> Seq.append addedHosts
                    |> Seq.except excludedHosts
                    |> Seq.distinct
                    |> Seq.map (DnsServerFormatter serverType)
                    |> printLines Console.Out
                int ExitCodes.Success