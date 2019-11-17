namespace Hosts

module HostNameSources =

    open System
    open System.Collections.Generic
    open System.IO
    open System.Net.Http

    open Logger

    type HostNameSource = {
            Name: string
            Url: Uri
            Format: string -> string
        }

    let hostNameSources = [
        {
            Name = "AbuseCH";
            Url = new Uri("https://ransomwaretracker.abuse.ch/downloads/RW_DOMBL.txt");
            Format = (fun raw -> if raw.StartsWith("#") then "" else raw)
        };
        {
            Name = "SANS Suspicious Low";
            Url = new Uri("https://isc.sans.edu/feeds/suspiciousdomains_Low.txt");
            Format = (fun raw -> if raw.StartsWith("#") || raw.StartsWith("site") then "" else raw)
        };
        {
            Name = "SANS Suspicious Medium";
            Url = new Uri("https://isc.sans.edu/feeds/suspiciousdomains_Medium.txt");
            Format = (fun raw -> if raw.StartsWith("#") || raw.StartsWith("site") then "" else raw)
        };
        {
            Name = "SANS Suspicious High";
            Url = new Uri("https://isc.sans.edu/feeds/suspiciousdomains_High.txt");
            Format = (fun raw -> if raw.StartsWith("#") || raw.StartsWith("site") then "" else raw)
        };
        {
            Name = "MVPS";
            Url = new Uri("http://winhelp2002.mvps.org/hosts.txt");
            Format = (fun raw -> if raw.StartsWith("#") || String.IsNullOrWhiteSpace raw then "" else raw.Split(" ", StringSplitOptions.RemoveEmptyEntries).[1])
        };
        {
            Name = "Firebog AdGuard DNS";
            Url = new Uri("https://v.firebog.net/hosts/AdguardDNS.txt");
            Format = (fun raw -> raw)
        };
        {
            Name = "Firebog Prigent Ads";
            Url = new Uri("https://v.firebog.net/hosts/Prigent-Ads.txt");
            Format = (fun raw -> raw)
        };
        {
            Name = "Firebog Prigent Malware";
            Url = new Uri("https://v.firebog.net/hosts/Prigent-Malware.txt");
            Format = (fun raw -> raw)
        };
        {
            Name = "Firebog Prigent Phishing";
            Url = new Uri("https://v.firebog.net/hosts/Prigent-Phishing.txt");
            Format = (fun raw -> raw)
        };
    ]

    let isNotNullOrWhiteSpace (line: string) : bool = not (String.IsNullOrWhiteSpace line)
    let isNotComment (line: string) : bool = not (line.StartsWith("#", StringComparison.OrdinalIgnoreCase))
    let isValidUri (line: string) : bool = fst (Uri.TryCreate("http://" + line, UriKind.Absolute))
    let isNotLocalhost (line: string) : bool = line <> "localhost" // we hard-omit localhost just in case

    let lineValidators =
        [
            isNotNullOrWhiteSpace;
            isNotComment;
            isValidUri;
            isNotLocalhost;
        ]

    let validateLine (validators: (string -> bool) list) (line: string) : bool =
        validators
            |> List.map (fun validator -> validator(line))
            |> List.forall ((=) true)

    let downloadSourceAsync (client: HttpClient) (source: HostNameSource) : Async<string> =
        async {
            try
                return! client.GetStringAsync(source.Url) |> Async.AwaitTask
            with
                | ex ->
                    printError (sprintf "downloading %s (%s) failed: %s" source.Name source.Url.AbsoluteUri ex.Message)
                    return String.Empty
        }

    let readLinesAsync (text: string) : Async<seq<string>> =
        async {
            use reader = new StringReader(text)
            let list = new List<string>()
            let mutable hasMoreLines = not (String.IsNullOrWhiteSpace text)
            try
                while hasMoreLines do
                    let! line = reader.ReadLineAsync() |> Async.AwaitTask
                    if isNull line then
                        hasMoreLines <- false
                    else
                        list.Add line
            with
                | :? ArgumentOutOfRangeException -> ()
            return list :> seq<string>
        }

    let getHostNamesFromSource (client: HttpClient) (source: HostNameSource) : Async<string list> =
        async {
            let! text = downloadSourceAsync client source
            let! lines = readLinesAsync text
            let validLines =
                lines
                    |> Seq.map source.Format // must format before we filter out
                    |> Seq.filter (validateLine lineValidators)
            printError (sprintf "found %i valid hosts from %s" (List.ofSeq validLines).Length source.Name)
            return validLines |> List.ofSeq
        }

    let getHostNamesFromAllSources (sources: HostNameSource list) : string list =
        use client = new HttpClient()
        sources
            |> Seq.map (getHostNamesFromSource client)
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.reduce List.append