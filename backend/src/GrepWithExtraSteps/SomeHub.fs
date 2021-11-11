namespace GrepWithExtraSteps

open System.Threading.Tasks
open GrepWithExtraSteps.Types
open Microsoft.AspNetCore.SignalR
open Microsoft.Extensions.Logging

type SomeHub(logger: ILogger<SomeHub>, queryService: IQueryService, hubContext: IHubContext<SomeHub>) =
    inherit Hub()

    member _.StartQuery() : Task =
        logger.LogInformation "StartQuery: no payload"
    
        queryService.StartQuery ()

    member _.CancelQuery() : Task =
        logger.LogInformation "CancelQuery: no payload"
        
        queryService.CancelQuery ()
