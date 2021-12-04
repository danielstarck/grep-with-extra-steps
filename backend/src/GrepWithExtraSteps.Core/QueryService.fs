namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open System.Text.RegularExpressions
open System.Threading

type QueryService(fileSystemService: IFileSystemService, messageService: IMessageService) =
    let mutable ctsOption: CancellationTokenSource option = None

    let isMatch (queryText: string) line = Regex.IsMatch(line, queryText)

    let toResultChunk (queryText: string) filePath (lineNumber, line) : ResultChunk option =
        if line |> isMatch queryText then
            Some
                { FilePath = filePath
                  LineNumber = lineNumber
                  MatchingText = line }
        else
            None

    let searchFile (queryText: string) (filePath: string) : Async<ResultChunk list> =
        async {
            let! file = fileSystemService.ReadFile filePath

            return
                file.Lines
                |> Seq.zip (Seq.initInfinite (fun index -> index + 1))
                |> Seq.choose (toResultChunk queryText file.Path)
                |> Seq.toList
        }

    let rec searchDirectory (queryText: string) (directory: Directory) : Async<ResultChunk list seq> =
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
        member _.ExecuteQuery(query: Query) : unit =
            let executeQueryAsync =
                async {
                    let! directory = fileSystemService.GetDirectory query.Directory
                    let! nestedChunks = searchDirectory query.Text directory

                    for chunks in nestedChunks do
                        do! messageService.SendResultChunks chunks

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
