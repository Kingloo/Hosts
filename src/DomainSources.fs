namespace Hosts

module DomainSources =

    open System
    open System.Collections.Generic
    open System.IO
    open System.Net.Http

    open Logger

    type DomainSource = {
            Name: string
            Url: Uri
            Format: string -> string
        }

    let domainSources = [
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

    let lineValidator (validatorFunction: string -> bool) (line: string option) : string option =
        match line with
            | Some s -> 
                match validatorFunction s with
                    | true -> Some(s)
                    | false -> None
            | None -> None

    // let lineValidator (validatorFunction: string -> bool option) (line: string option) : bool option =
    //     match line with
    //         | Some s -> s |> validatorFunction
    //         | None -> None

    let isValidUri (line: string) : bool =
        match Uri.TryCreate("http://" + line, UriKind.Absolute) with
            | true, _ -> true
            | false, _ -> false

    let validate (line: string) : bool =
        let result =
            Some(line)
                |> lineValidator (String.IsNullOrWhiteSpace >> not)
                |> lineValidator (fun line -> line <> "localhost") // we hard-omit localhost just in case
                |> lineValidator isValidUri
        match result with
            | Some _ -> true
            | None -> false

    let downloadSourceAsync (client: HttpClient) (source: DomainSource) : Async<string> =
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
            let mutable hasMoreLines = not (String.IsNullOrWhiteSpace(text))
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

    let getHostsForSource (client: HttpClient) (source: DomainSource) : Async<seq<string>> =
        async {
            let! text = downloadSourceAsync client source
            let! lines = readLinesAsync text
            let validLines =
                lines
                    |> Seq.map source.Format
                    |> Seq.filter validate
            printError (sprintf "found %i valid hosts from %s" (List.ofSeq validLines).Length source.Name)
            return validLines
        }

    let getHostsForAllSources (sources: seq<DomainSource>) : seq<string> =
        use client = new HttpClient()
        sources
            |> Seq.map (getHostsForSource client)
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.reduce Seq.append