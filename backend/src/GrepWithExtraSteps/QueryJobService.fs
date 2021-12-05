namespace GrepWithExtraSteps

open System.Threading
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open FSharp.Control

type QueryJobService(directoryService: IDirectoryService, queryService: IQueryService, messageService: IMessageService) =
    let mutable ctsOption: CancellationTokenSource option = None

    member _.StartQueryJob(query: Query) : unit =
        let queryJobAsync =
            async {
                let directory =
                    directoryService.GetDirectory(fun _ -> true) query.Directory

                let chunks =
                    queryService.ExecuteQuery directory (fun _ -> true)

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
