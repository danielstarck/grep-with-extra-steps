namespace GrepWithExtraSteps

open System.Threading.Tasks
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Logging

type SomeHub(logger: ILogger<SomeHub>, queryService: IQueryService) =
    inherit Hub()

    member _.StartQuery() : Task =
        logger.LogInformation "StartQuery: no payload"

        let query =
            { Directory = ""
              Files = ""
              Text = "" }

        do queryService.ExecuteQuery query

        Task.CompletedTask

    member _.CancelQuery() : Task =
        logger.LogInformation "CancelQuery: no payload"

        do queryService.CancelQuery()

        Task.CompletedTask
