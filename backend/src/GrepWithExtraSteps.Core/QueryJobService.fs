namespace GrepWithExtraSteps.Core

open System.Threading
open GrepWithExtraSteps.Core
open GrepWithExtraSteps.Core.Interfaces
open FSharp.Control

type internal QueryJobService(directoryService: IDirectoryService, messageService: IMessageService) =
    let mutable ctsOption: CancellationTokenSource option = None

    interface IQueryJobService with
        member _.StartQueryJob directoryPath fileIsInScope lineIsMatch : unit =
            let queryJobAsync =
                async {
                    let chunks =
                        directoryService.GetDirectory fileIsInScope directoryPath
                        |> QueryExecution.searchDirectory lineIsMatch

                    do!
                        chunks
                        |> AsyncSeq.iterAsync messageService.SendResultChunk

                    do! messageService.SendQueryFinished()
                }

            // TODO: dispose?
            let cts = new CancellationTokenSource()
            do ctsOption <- Some cts

            do Async.Start(queryJobAsync, cancellationToken = cts.Token)

        member _.CancelQueryJob() : unit =
            match ctsOption with
            | Some cts ->
                do ctsOption <- None
                do cts.Cancel()
            | None -> ()
