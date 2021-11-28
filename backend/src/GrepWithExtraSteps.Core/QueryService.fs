namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open System.Threading

type QueryService(fileSystemService: IFileSystemService, messageService: IMessageService) =
    let mutable ctsOption: CancellationTokenSource option = None

    interface IQueryService with
        member _.ExecuteQuery(query: Query) : unit =
            let someAsync =
                async {
                    for chunks in
                        Seq.init 10 (fun index ->
                            let number = index + 1

                            { FilePath = $"Path #%d{number}"
                              LineNumber = number
                              MatchingText = $"Matching text #%d{number}" })
                        |> Seq.chunkBySize 3
                        |> Seq.map Seq.toList do
                        do! Async.Sleep 500
                        do! messageService.SendResultChunks chunks

                    do! messageService.SendQueryFinished()
                }

            // TODO: dispose?
            let cts = new CancellationTokenSource()
            do ctsOption <- Some cts

            do Async.Start(someAsync, cancellationToken = cts.Token)

        member _.CancelQuery() : unit =
            match ctsOption with
            | Some cts ->
                do ctsOption <- None
                do cts.Cancel()
            | None -> ()
