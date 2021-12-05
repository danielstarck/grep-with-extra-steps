namespace GrepWithExtraSteps

open GrepWithExtraSteps.Core.Interfaces
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Logging

type MessageService(logger: ILogger<MessageService>, hubContext: IHubContext<SomeHub>) =
    interface IMessageService with
        member _.SendResultChunk chunk =
            async {
                do logger.LogInformation($"Sent ResultChunk: %A{chunk}")

                do!
                    hubContext.Clients.All.SendAsync("ResultChunk", chunk)
                    |> Async.AwaitTask
            }

        member _.SendQueryFinished() =
            async {
                do logger.LogInformation "Sent QueryFinished"

                do!
                    hubContext.Clients.All.SendAsync("QueryFinished")
                    |> Async.AwaitTask
            }
