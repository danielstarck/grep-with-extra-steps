namespace GrepWithExtraSteps

open GrepWithExtraSteps.Core.Interfaces

type FileSystemService() =
    interface IFileSystemService with
        member _.ReadFile file = Seq.empty
