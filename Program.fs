namespace Hosts

module Program =

    open System
    open System.IO
    open System.Net.Http
    open System.Security
    open DataTypes

    let writeMessage (message: string) =
        use writer = Console.Error
        writer.WriteLine(message)

    let determineServerType args : DnsServerType =
        let typeIdx = Array.tryFindIndex (fun elem -> elem = "-type") args
        match typeIdx with
            | Some idx ->
                try
                    match args.[idx + 1] with // the domain type will be in the array position after "-type"
                        | "bind" -> Bind
                        | "unbound" -> Unbound
                        | "windows" -> Windows
                        | _ -> Unknown
                with
                    | :? IndexOutOfRangeException -> Unknown
            | None -> Missing

    let getLinesFromWebString (webString: string) (domainSource: DomainSource): Async<seq<string>> =
        async {
            let lines = new System.Collections.Generic.List<string>()
            use reader = new StringReader(webString)
            let mutable hasMoreLines = true
            while hasMoreLines do
                let! line = reader.ReadLineAsync() |> Async.AwaitTask
                if line <> null then
                    let formattedLine = domainSource.Format line
                    match Uri.TryCreate("http://" + formattedLine, UriKind.Absolute) with
                        | true, _ -> lines.Add formattedLine
                        | false, _ -> ()
                else
                    hasMoreLines <- false
            writeMessage ("loaded " + lines.Count.ToString() + " lines from " + domainSource.Name.ToString() + " (" + domainSource.Url.AbsoluteUri + ")")
            return lines :> seq<string>
        }        

    let downloadDomainSource (client: HttpClient) (domainSource: DomainSource) : Async<seq<string>> =
        async {
            try
                let! result = client.GetStringAsync(domainSource.Url) |> Async.AwaitTask
                return! getLinesFromWebString result domainSource
            with
                | ex ->
                    writeMessage ("downloading (" + domainSource.Url.AbsoluteUri + ") failed: " + ex.GetType().Name + " (" + ex.Message + ")")
                    return Seq.empty
        }

    let getRemoteDomains domainSources : seq<string> =
        use client = new HttpClient()
        domainSources
            |> List.map (downloadDomainSource (client))
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.reduce (fun acc item -> Seq.append acc item) // turns an array of seqs into one seq of everything

    let printLines (lines: seq<string>) =
        use writer = Console.Out
        lines
            |> Seq.iter (fun line -> writer.WriteLine(line))

    let printDomains (serverType: DnsServerType) (domains: seq<string>) =
        domains
        |> Seq.map (fun item ->
            let dns = List.find (fun (y: DnsServer) -> y.Name = serverType) dnsServerTypes
            dns.Format item)
        |> printLines

    let loadLinesFromFile filePath =
        try
            let lines = File.ReadAllLines filePath
            writeMessage ("loaded " + lines.Length.ToString() + " lines from " + filePath)
            lines
        with
            | :? FileNotFoundException -> writeMessage (filePath + " was not found"); Array.empty
            | :? DirectoryNotFoundException -> writeMessage "directory not found"; Array.empty
            | :? PathTooLongException -> writeMessage (filePath + " exceeded path character limit"); Array.empty
            | :? NotSupportedException -> writeMessage (filePath + " is an unsupported format"); Array.empty
            | :? SecurityException -> writeMessage ("you do not have the required permissions for " + filePath); Array.empty
            | :? IOException -> writeMessage ("an i/o error occurred while opening " + filePath); Array.empty

    [<EntryPoint>]
    let main args =
        match determineServerType args with
            | Unknown ->
                writeMessage "! server type unknown !"
                int ExitCodes.ErrorServerTypeUnknown
            | Missing ->
                writeMessage "! no \"-type\" switch !"
                int ExitCodes.ErrorServerTypeSwitchMissing
            | serverType ->
                let customExtras = loadLinesFromFile customExtrasFilePath
                let addedHosts = loadLinesFromFile addedHostsFilePath
                let excludedHosts = loadLinesFromFile excludedHostsFilePath
                printLines customExtras
                domainSources
                    |> getRemoteDomains
                    |> Seq.append addedHosts
                    |> Seq.except excludedHosts
                    |> Seq.distinct
                    |> printDomains serverType
                int ExitCodes.Success