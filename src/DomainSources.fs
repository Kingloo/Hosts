namespace Hosts

module DomainSources =

    open System
    open System.Collections.Generic
    open System.IO
    open System.Net.Http
    open System.Threading.Tasks

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

    // effectively introducing the C# await keyword as a function - bad idea?
    let await (task: Task<'a>) : Async<'a> =
        task |> Async.AwaitTask

    let downloadStringAsync (client: HttpClient) (url: Uri) : Async<string> =
        async {
            try
                return! await (client.GetStringAsync url)
            with
                | ex ->
                    printError (sprintf "downloading %s failed: %s" url.AbsoluteUri ex.Message)
                    return String.Empty
        }

    let formatLinesAsync (text: string) (source: DomainSource) : Async<seq<string>> =
        async {
            let lines = new List<string>()
            use reader = new StringReader(text)
            let mutable hasMoreLines = not (String.IsNullOrWhiteSpace(text))
            while hasMoreLines do
                let! line = await (reader.ReadLineAsync())
                if not (isNull line) then
                    let formatted = source.Format line
                    if not (formatted.Equals("localhost")) then // we hard-omit localhost from every source just in case
                        match Uri.TryCreate("http://" + formatted, UriKind.Absolute) with
                            | true, _ -> lines.Add formatted
                            | false, _ -> ()
                else
                    hasMoreLines <- false
            printError (sprintf "loaded %i lines from %s (%s)" lines.Count source.Name source.Url.AbsoluteUri )
            return lines :> seq<string>
        }

    let getSourceHosts (client: HttpClient) (source: DomainSource) : Async<seq<string>> =
        async {
            let! text = downloadStringAsync client source.Url
            return! formatLinesAsync text source
        }
    
    let getAllSourceHosts (sources: seq<DomainSource>) : seq<string> =
        use client = new HttpClient()
        sources
            |> Seq.map (getSourceHosts client)
            |> Async.Parallel
            |> Async.RunSynchronously
            |> Array.reduce Seq.append // turns an array of seqs into a single seq of everything