[<RequireQualifiedAccess>]
module internal GrepWithExtraSteps.Core.Tests.InMemory.Filename

open System

let getFilename (path: string) =
    path.Split("/", StringSplitOptions.None)
    |> Array.last
