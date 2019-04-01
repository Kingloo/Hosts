namespace Hosts

module Program =

    open System
    open System.IO
    open System.Security
    
    open DnsServers
    open DomainSources

#if DEBUG
    let directory = Environment.CurrentDirectory
#else
    let directory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
#endif
    
    let addedHostsFilePath = Path.Combine (directory, "addedHosts.txt")
    let excludedHostsFilePath = Path.Combine (directory, "excludedHosts.txt")
    let customExtrasFilePath = Path.Combine (directory, "customExtras.txt")

    let loadInputLines (filePath: string) : seq<string> =
        try
            let lines = File.ReadAllLines filePath |> Array.where (fun line -> not (line.StartsWith("#")))
            eprintfn "loaded %i lines from %s" lines.Length filePath
            lines :> seq<string>
        with
            | :? FileNotFoundException -> eprintfn "%s was not found" filePath ; Seq.empty
            | :? DirectoryNotFoundException -> eprintfn "directory not found" ; Seq.empty
            | :? PathTooLongException -> eprintfn "%s exceeded path character limit" filePath ; Seq.empty
            | :? NotSupportedException -> eprintfn "%s is an unsupported format" filePath ; Seq.empty
            | :? SecurityException -> eprintfn "you do not have the required permissions for %s" filePath ; Seq.empty
            | :? IOException -> eprintfn "an i/o error occurred while opening %s" filePath ; Seq.empty

    let printOutputLines (lines: seq<string>) =
        lines
            |> Seq.iter (fun line -> printfn "%s" line)

    type ExitCodes =
        | Success = 0
        | ErrorServerTypeSwitchMissing = -1
        | ErrorServerTypeUnknown = -2

    [<EntryPoint>]
    let main args =
        match determineServerType args with
            | Unknown ->
                eprintfn "! server type unknown !"
                int ExitCodes.ErrorServerTypeUnknown
            | Missing ->
                eprintfn "! no \"-type\" switch !"
                int ExitCodes.ErrorServerTypeSwitchMissing
            | serverType ->
                let customExtras = loadInputLines customExtrasFilePath
                customExtras |> printOutputLines
                let addedHosts = loadInputLines addedHostsFilePath
                let excludedHosts = loadInputLines excludedHostsFilePath
                domainSources
                    |> getAllSourceHosts
                    |> Seq.append addedHosts
                    |> Seq.except excludedHosts
                    |> Seq.distinct
                    |> Seq.map (DnsServerFormatter serverType)
                    |> printOutputLines
                int ExitCodes.Success