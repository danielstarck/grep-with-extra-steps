namespace GrepWithExtraSteps

open GrepWithExtraSteps.Core.Interfaces
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Logging

type MessageService(logger: ILogger<MessageService>, queryHubContext: IHubContext<QueryHub>) =
    interface IMessageService with
        member _.SendResultChunk chunk =
            async {
                do logger.LogInformation($"Sent ResultChunk: %A{chunk}")

                do!
                    queryHubContext.Clients.All.SendAsync("ResultChunk", chunk)
                    |> Async.AwaitTask
            }

        member _.SendQueryFinished() =
            async {
                do logger.LogInformation "Sent QueryFinished"

                do!
                    queryHubContext.Clients.All.SendAsync("QueryFinished")
                    |> Async.AwaitTask
            }
