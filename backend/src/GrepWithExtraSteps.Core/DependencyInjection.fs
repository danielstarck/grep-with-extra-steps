module GrepWithExtraSteps.Core.DependencyInjection

open Microsoft.Extensions.DependencyInjection
open GrepWithExtraSteps.Core.Interfaces

let addQueryService (services: IServiceCollection) =
    services.AddSingleton<IQueryService, QueryService>()
