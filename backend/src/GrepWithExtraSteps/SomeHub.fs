namespace GrepWithExtraSteps

open System.Threading.Tasks
open GrepWithExtraSteps.Core.Domain
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Logging

type SomeHub(logger: ILogger<SomeHub>, queryJobService: QueryJobService) =
    inherit Hub()

    member _.StartQuery(query: Query) : Task =
        logger.LogInformation $"StartQuery: %A{query}"

        do queryJobService.StartQueryJob query

        Task.CompletedTask

    member _.CancelQuery() : Task =
        logger.LogInformation "CancelQuery: no payload"

        do queryJobService.CancelQueryJob()

        Task.CompletedTask
