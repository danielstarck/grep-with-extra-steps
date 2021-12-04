namespace GrepWithExtraSteps

open System.Threading.Tasks
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Logging

type SomeHub(logger: ILogger<SomeHub>, queryService: IQueryService) =
    inherit Hub()

    member _.StartQuery(query: Query) : Task =
        logger.LogInformation $"StartQuery: %A{query}"

        do queryService.ExecuteQuery query

        Task.CompletedTask

    member _.CancelQuery() : Task =
        logger.LogInformation "CancelQuery: no payload"

        do queryService.CancelQuery()

        Task.CompletedTask
