namespace GrepWithExtraSteps

open System.Threading
open System.Threading.Tasks
open GrepWithExtraSteps.Types
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Logging

type QueryService (logger: ILogger<QueryService>, resultService: ResultService, hubContext: IHubContext<SomeHub>) =
    let mutable ctsOption: CancellationTokenSource option = None
    
    interface IQueryService with
        member _.StartQuery (): Task =
            logger.LogInformation "StartQuery: no payload"

            let sendResultChunk (result: string) =
                do logger.LogInformation $"Sent ResultChunk: %s{result}"

                hubContext.Clients.All.SendAsync("ResultChunk", result)
                |> Async.AwaitTask

            let sendQueryFinished () =
                do logger.LogInformation "Sent QueryFinished"

                hubContext.Clients.All.SendAsync("QueryFinished")
                |> Async.AwaitTask
            
            // TODO: dispose?
            let cts = new CancellationTokenSource()
            do ctsOption <- Some cts
            
            do
                resultService.GetResults sendResultChunk sendQueryFinished
                |> fun async -> Async.Start(async, cancellationToken = cts.Token)
            
            Task.CompletedTask

        member _.CancelQuery(): Task =
            async {
                match ctsOption with
                | Some cts ->
                    do ctsOption <- None
                    do cts.Cancel()
                | None -> ()
            }
            |> Async.StartAsTask
            :> Task
