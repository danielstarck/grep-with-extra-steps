namespace GrepWithExtraSteps.Controllers

open Microsoft.AspNetCore.Mvc
open Microsoft.Extensions.Logging

[<ApiController>]
[<Route("helloworld")>]
type WeatherForecastController(logger: ILogger<WeatherForecastController>) =
    inherit ControllerBase()

    [<HttpGet>]
    member _.Get() = "Hello world!"
