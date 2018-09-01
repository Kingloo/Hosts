namespace Hosts

module DataTypes =

    open System
    open System.IO

    type ExitCodes =
        | Success = 0
        | ErrorServerTypeSwitchMissing = -1
        | ErrorServerTypeUnknown = -2

    let blackHole = "0.0.0.0"
    
#if DEBUG
    let directory = Environment.CurrentDirectory
#else
    let directory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location)
#endif
    
    let addedHostsFilePath = Path.Combine (directory, "addedHosts.txt")
    let excludedHostsFilePath = Path.Combine (directory, "excludedHosts.txt")
    let customExtrasFilePath = Path.Combine (directory, "customExtras.txt")

    type DnsServerType =
        | Bind
        | Unbound
        | Windows
        | Unknown
        | Missing

    let DnsServerTypeFormatter (serverType: DnsServerType) : (string -> string) =
        match serverType with
            | Bind -> fun raw -> sprintf "zone \"%s\" { type master; file \"/etc/bind/zones/db.poison\"; };" raw
            | Unbound -> fun raw -> sprintf "local-zone: \"%s\" inform_deny." raw
            | Windows -> fun raw -> sprintf "%s %s" blackHole raw
            | Unknown -> fun raw -> sprintf "DNS server type unknown: %s" raw
            | Missing -> fun _ -> sprintf "server type was missing"

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
            Format = (fun (raw: string) -> if raw.StartsWith("#") || String.IsNullOrWhiteSpace raw then "" else raw.Split(" ", StringSplitOptions.RemoveEmptyEntries).[1])
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