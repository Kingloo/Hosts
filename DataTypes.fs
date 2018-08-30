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
        | Missing
        | Unknown

    type DnsServer = {
        Name: DnsServerType
        Format: string -> string
    }

    let dnsServerTypes : List<DnsServer> = [
        {
            Name = Bind
            Format = (fun x -> "zone \"" + x + "\" { type master; file \"/etc/bind/zones/db.poison\"; };")
        }
        {
            Name = Unbound
            Format = (fun x -> "local-zone: \"" + x + "\" inform_deny.")
        }
        {
            Name = Windows
            Format = (fun x -> blackHole + " " + x)
        }
        {
            Name = Unknown
            Format = (fun x -> "# domain name server type unknown")
        }
    ]

    type DomainSourceType = 
        | AbuseCH
        | MVPS
        | SANS
        | Firebog

    type DomainSource = {
        Name: DomainSourceType
        Url: Uri
        Format: string -> string
    }

    let domainSources : List<DomainSource> = [
        {
            Name = AbuseCH;
            Url = new Uri("https://ransomwaretracker.abuse.ch/downloads/RW_DOMBL.txt");
            Format = (fun raw -> if raw.StartsWith("#") then "" else raw)
        }
        {
            Name = SANS;
            Url = new Uri("https://isc.sans.edu/feeds/suspiciousdomains_Low.txt");
            Format = (fun raw -> if raw.StartsWith("#") || raw.StartsWith("site") then "" else raw)
        }
        {
            Name = SANS;
            Url = new Uri("https://isc.sans.edu/feeds/suspiciousdomains_Medium.txt");
            Format = (fun raw -> if raw.StartsWith("#") || raw.StartsWith("site") then "" else raw)
        }
        {
            Name = SANS;
            Url = new Uri("https://isc.sans.edu/feeds/suspiciousdomains_High.txt");
            Format = (fun raw -> if raw.StartsWith("#") || raw.StartsWith("site") then "" else raw)
        }
        {
            Name = MVPS;
            Url = new Uri("http://winhelp2002.mvps.org/hosts.txt");
            Format = (fun (raw: string) -> if raw.StartsWith("#") || String.IsNullOrWhiteSpace raw then "" else raw.Split(" ", StringSplitOptions.RemoveEmptyEntries).[1])
        }
        {
            Name = Firebog;
            Url = new Uri("https://v.firebog.net/hosts/Prigent-Ads.txt");
            Format = (fun raw -> raw)
        }
        {
            Name = Firebog;
            Url = new Uri("https://v.firebog.net/hosts/Easyprivacy.txt");
            Format = (fun raw -> raw)
        }
        {
            Name = Firebog;
            Url = new Uri("https://v.firebog.net/hosts/AdguardDNS.txt");
            Format = (fun raw -> raw)
        }
        {
            Name = Firebog;
            Url = new Uri("https://v.firebog.net/hosts/Airelle-trc.txt");
            Format = (fun raw -> raw)
        }
    ]