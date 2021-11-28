namespace GrepWithExtraSteps

open GrepWithExtraSteps.Core.DependencyInjection
open GrepWithExtraSteps.Core.Interfaces
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

type Startup(configuration: IConfiguration) =
    member _.Configuration = configuration

    member _.ConfigureServices(services: IServiceCollection) =
        services.AddControllers() |> ignore
        services.AddSignalR() |> ignore

        (services
            .AddSingleton<IMessageService, MessageService>()
            .AddSingleton<IFileSystemService, FileSystemService>())
        |> addQueryService
        |> ignore

    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        app
            .UseDefaultFiles()
            .UseStaticFiles()
            .UseRouting()
            .UseAuthorization()
            .UseEndpoints(fun endpoints ->
                endpoints.MapControllers() |> ignore
                endpoints.MapHub<SomeHub>("/someHub") |> ignore)
        |> ignore
