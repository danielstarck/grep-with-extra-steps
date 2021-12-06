namespace GrepWithExtraSteps

open System.Threading
open System.Text.RegularExpressions
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open FSharp.Control

type QueryJobService(directoryService: IDirectoryService, queryService: IQueryService, messageService: IMessageService) =
    let mutable ctsOption: CancellationTokenSource option = None

    member _.StartQueryJob(query: Query) : unit =
        let queryJobAsync =
            async {
                let chunks =
                    directoryService.GetDirectory(fun _ -> true) query.Directory
                    |> queryService.ExecuteQuery(fun line -> Regex.IsMatch(line, query.Text))

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
