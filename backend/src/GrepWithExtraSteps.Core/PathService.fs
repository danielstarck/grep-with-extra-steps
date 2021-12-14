namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core.Interfaces

type PathService() =
    interface IPathService with
        member this.GetFilename path = System.IO.Path.GetFileName path
