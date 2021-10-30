namespace GrepWithExtraSteps

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.Cors.Infrastructure

type Startup(configuration: IConfiguration) =
    member _.Configuration = configuration

    member _.ConfigureServices(services: IServiceCollection) =
        services.AddCors() |> ignore
        services.AddControllers() |> ignore
        services.AddSignalR() |> ignore

        services.AddHostedService<SomeBackgroundService>()
        |> ignore

    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        app
            // .UseHttpsRedirection()
            // Also add...
            // "applicationUrl": "https://localhost:5001;http://localhost:5000",
            // ...to launchSettings.json
            .UseDefaultFiles()
            .UseStaticFiles()
            .UseCors(Action<_>(fun (options: CorsPolicyBuilder) -> options.AllowAnyOrigin() |> ignore))
            .UseRouting()
            .UseAuthorization()
            .UseEndpoints(fun endpoints ->
                endpoints.MapControllers() |> ignore
                endpoints.MapHub<SomeHub>("/someHub") |> ignore)
        |> ignore
