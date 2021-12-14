namespace GrepWithExtraSteps.Core.Tests.InMemory.Fakes

open System
open GrepWithExtraSteps.Core.Interfaces

type internal PathService() =
    interface IPathService with
        member this.GetFilename path =
            path.Split("/", StringSplitOptions.None)
            |> Array.last
