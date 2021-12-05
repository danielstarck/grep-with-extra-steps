namespace GrepWithExtraSteps

open System.Threading
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces

type QueryJobService(queryService: IQueryService, messageService: IMessageService) =
    let mutable ctsOption: CancellationTokenSource option = None

    member _.StartQueryJob(query: Query): unit =
        let queryJobAsync =
            async {
                let! chunks = queryService.ExecuteQuery query

                for chunk in chunks do
                    do! messageService.SendResultChunk chunk

                do! messageService.SendQueryFinished()
            }

        // TODO: dispose?
        let cts = new CancellationTokenSource()
        do ctsOption <- Some cts

        do Async.Start(queryJobAsync, cancellationToken = cts.Token)

    member _.CancelQueryJob(): unit =
        match ctsOption with
        | Some cts ->
            do ctsOption <- None
            do cts.Cancel()
        | None -> ()
