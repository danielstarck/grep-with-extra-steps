namespace GrepWithExtraSteps.Core.Tests.InMemory.Fakes

open GrepWithExtraSteps.Core.Interfaces
open GrepWithExtraSteps.Core.Tests.InMemory

type internal PathService() =
    interface IPathService with
        member this.GetFilename path = Filename.getFilename path
