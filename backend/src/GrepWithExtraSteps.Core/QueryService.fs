namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open System.Text.RegularExpressions
open System.Threading

type QueryService(fileSystemService: IFileSystemService, messageService: IMessageService) =
    let mutable ctsOption: CancellationTokenSource option = None

    let isMatch (queryText: string) line = Regex.IsMatch(line, queryText)

    let toMatchingLine (queryText: string) filePath (lineNumber, line) : MatchingLine option =
        if line |> isMatch queryText then
            Some
                { FilePath = filePath
                  LineNumber = lineNumber
                  MatchingText = line }
        else
            None

    let searchFile (queryText: string) (filePath: string) : Async<ResultChunk> =
        async {
            let! file = fileSystemService.ReadFile filePath

            return
                file.Lines
                |> Seq.zip (Seq.initInfinite (fun index -> index + 1))
                |> Seq.choose (toMatchingLine queryText file.Path)
                |> Seq.toList
        }

    let rec searchDirectory (queryText: string) (directory: Directory) : Async<ResultChunk seq> =
        async {
            let! filesFromThisDirectory =
                directory.Files
                |> Seq.map (searchFile queryText)
                |> Async.Parallel

            let! filesFromSubdirectories =
                directory.Directories
                |> Seq.map (searchDirectory queryText)
                |> Async.Parallel

            return
                filesFromSubdirectories
                |> Seq.concat
                |> Seq.append filesFromThisDirectory
        }

    interface IQueryService with
        member _.StartQuery(query: Query) : unit =
            let executeQueryAsync =
                async {
                    let! directory = fileSystemService.GetDirectory query.Directory
                    let! chunks = searchDirectory query.Text directory

                    for chunk in chunks do
                        do! messageService.SendResultChunk chunk

                    do! messageService.SendQueryFinished()
                }

            // TODO: dispose?
            let cts = new CancellationTokenSource()
            do ctsOption <- Some cts

            do Async.Start(executeQueryAsync, cancellationToken = cts.Token)

        member _.CancelQuery() : unit =
            match ctsOption with
            | Some cts ->
                do ctsOption <- None
                do cts.Cancel()
            | None -> ()
