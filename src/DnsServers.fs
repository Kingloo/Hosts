namespace Hosts

module DnsServers =

    open System

    let blackHoleIp = "0.0.0.0"

    type DnsServer =
        | Bind
        | Unbound
        | Windows
        | Unknown
        | Missing

    let DnsServerFormatter (serverType: DnsServer) : (string -> string) =
        match serverType with
            | Bind -> fun raw -> sprintf "zone \"%s\" { type master; file \"/etc/bind/zones/db.poison\"; };" raw
            | Unbound -> fun raw -> sprintf "local-zone: \"%s\" inform_deny." raw
            | Windows -> fun raw -> sprintf "%s %s" blackHoleIp raw
            | Unknown -> fun raw -> sprintf "DNS server type unknown: %s" raw
            | Missing -> fun _ -> sprintf "server type was missing"

    let determineServerType (args: string[]) : DnsServer =
        match Array.tryFindIndex (fun elem -> elem = "-type") args with
            | Some idx ->
                try
                    match args.[idx + 1] with // the domain type will be in the array position after "-type", e.g. "-type bind"
                        | "bind" -> Bind
                        | "unbound" -> Unbound
                        | "windows" -> Windows
                        | _ -> Unknown
                with
                    | :? IndexOutOfRangeException -> Unknown
            | None -> Missing