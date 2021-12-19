namespace GrepWithExtraSteps

open System.Threading.Tasks
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Types
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Logging

type SomeHub(logger: ILogger<SomeHub>, someHubService: ISomeHubService) =
    inherit Hub()

    member _.StartQuery(query: Query) : Task =
        do logger.LogInformation $"StartQuery: %A{query}"

        do someHubService.StartQuery query

        Task.CompletedTask

    member _.CancelQuery() : Task =
        do logger.LogInformation "CancelQuery: no payload"

        do someHubService.CancelQuery()

        Task.CompletedTask
