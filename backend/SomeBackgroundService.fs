namespace GrepWithExtraSteps

open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type SomeBackgroundService(someHubContext: IHubContext<SomeHub>, logger: ILogger<SomeBackgroundService>) =
    inherit BackgroundService()

    override this.ExecuteAsync(stoppingToken: CancellationToken) : Task =
        let someAsync: Async<unit> =
            let mutable number = 1

            async {
                while not stoppingToken.IsCancellationRequested do
                    do!
                        someHubContext.Clients.All.SendAsync("ResultPart", $"An ever increasing number: %d{number}")
                        |> Async.AwaitTask

                    do number <- number + 1
                    do! Async.Sleep 5000
            }

        upcast Async.StartAsTask someAsync
