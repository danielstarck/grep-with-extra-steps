module GrepWithExtraSteps.Core.DependencyInjection

open Microsoft.Extensions.DependencyInjection

let addQueryService (services: IServiceCollection) =
    services
