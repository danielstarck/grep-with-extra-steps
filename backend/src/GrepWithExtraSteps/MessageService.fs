namespace GrepWithExtraSteps

open GrepWithExtraSteps.Core.Interfaces
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Logging

type MessageService(logger: ILogger<MessageService>, hubContext: IHubContext<SomeHub>) =
    interface IMessageService with
        member _.SendResultChunks chunks =
            async {
                do logger.LogInformation($"Sent ResultChunks: %A{chunks}")

                do!
                    hubContext.Clients.All.SendAsync("ResultChunks", chunks)
                    |> Async.AwaitTask
            }

        member _.SendQueryFinished() =
            async {
                do logger.LogInformation "Sent QueryFinished"

                do!
                    hubContext.Clients.All.SendAsync("QueryFinished")
                    |> Async.AwaitTask
            }
