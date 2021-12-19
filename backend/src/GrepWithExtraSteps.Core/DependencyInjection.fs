module GrepWithExtraSteps.Core.DependencyInjection

open GrepWithExtraSteps.Core.Interfaces
open Microsoft.Extensions.DependencyInjection

let addCoreServices (services: IServiceCollection) =
    services
        .AddSingleton<IQueryJobService, QueryJobService>()
        .AddSingleton<IDirectoryService, DirectoryService>()
        .AddSingleton<IFileSystemService, FileSystemService>()
