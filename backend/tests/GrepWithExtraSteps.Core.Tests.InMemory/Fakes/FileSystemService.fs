namespace GrepWithExtraSteps.Core.Tests.InMemory.Fakes

open System.IO
open GrepWithExtraSteps.Core.Interfaces

type internal FileSystemService
    (
        directoriesByPath: Map<string, string seq>,
        filesByPath: Map<string, string seq>,
        readersByPath: Map<string, StreamReader>
    ) =
    interface IFileSystemService with
        member this.GetDirectories path = Map.find path directoriesByPath

        member this.GetFiles path = Map.find path filesByPath

        member this.GetReader path = Map.find path readersByPath
