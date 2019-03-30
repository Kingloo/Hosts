namespace Hosts

module Program =

    open System
    open System.IO
    open System.Net.Http
    open System.Security
    open DataTypes

    let determineServerType programArgs : DnsServerType =
        let typeIdx = Array.tryFindIndex (fun elem -> elem = "-type") programArgs
        match typeIdx with
            | Some idx ->
                try
                    match programArgs.[idx + 1] with // the domain type will be in the array position after "-type"
                        | "bind" -> Bind
                        | "unbound" -> Unbound
                        | "windows" -> Windows
                        | _ -> Unknown
                with
                    | :? IndexOutOfRangeException -> Unknown
            | None -> Missing

    let loadLinesFromFile filePath =
        try
            let lines = ((File.ReadAllLines filePath)
                |> Array.where (fun line -> not (line.StartsWith("#"))))
            eprintfn "loaded %i lines from %s" lines.Length filePath
            lines
        with
            | :? FileNotFoundException -> eprintfn "%s was not found" filePath ; Array.empty
            | :? DirectoryNotFoundException -> eprintfn "directory not found" ; Array.empty
            | :? PathTooLongException -> eprintfn "%s exceeded path character limit" filePath ; Array.empty
            | :? NotSupportedException -> eprintfn "%s is an unsupported format" filePath ; Array.empty
            | :? SecurityException -> eprintfn "you do not have the required permissions for %s" filePath ; Array.empty
            | :? IOException -> eprintfn "an i/o error occurred while opening %s" filePath ; Array.empty

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
            eprintfn "loaded %i lines from %s (%s)" lines.Count (domainSource.Name.ToString()) domainSource.Url.AbsoluteUri
            return lines :> seq<string>
        }        

    let downloadDomainSource (client: HttpClient) (domainSource: DomainSource) : Async<seq<string>> =
        async {
            try
                let! result = client.GetStringAsync(domainSource.Url) |> Async.AwaitTask
                return! getLinesFromWebString result domainSource
            with
                | ex ->
                    eprintfn "downloading %s failed: %s (%s)" domainSource.Url.AbsoluteUri (ex.GetType().Name) ex.Message
                    return Seq.empty
        }

    let getRemoteDomains domainSources : seq<string> =
        use client = new HttpClient()
        domainSources
            |> List.map (downloadDomainSource client)
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.reduce (fun acc item -> Seq.append acc item) // turns an array of seqs into one seq of everything        

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
                let customExtras = loadLinesFromFile customExtrasFilePath
                customExtras
                    |> Seq.iter (fun x -> printfn "%s" x)
                let addedHosts = loadLinesFromFile addedHostsFilePath
                let excludedHosts = loadLinesFromFile excludedHostsFilePath
                domainSources
                    |> getRemoteDomains
                    |> Seq.append addedHosts
                    |> Seq.except excludedHosts
                    |> Seq.distinct
                    |> Seq.map (DnsServerTypeFormatter serverType)
                    |> Seq.iter (fun x -> printfn "%s" x)
                int ExitCodes.Success