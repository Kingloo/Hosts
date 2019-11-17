namespace Hosts

module Logger = 

    open System
    open System.IO

    let printLine (writer: TextWriter) (line: string) = writer.WriteLine line
    let printLines (writer: TextWriter) (lines: seq<string>) = lines |> Seq.iter (printLine writer)
    let printError (line: string) = printLine Console.Error line