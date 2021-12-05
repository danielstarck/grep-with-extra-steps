namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open FSharp.Control

// TODO: investigate Directory.EnumerateFileSystemEntries
type DirectoryService(fileSystemService: IFileSystemService) =
    let readLines path : AsyncSeq<string> =
        use reader = fileSystemService.GetReader path

        asyncSeq {
            while not reader.EndOfStream do
                let! line = reader.ReadLineAsync() |> Async.AwaitTask

                yield line
        }

    let getFiles fileIsInScope path =
        fileSystemService.GetFiles path
        |> Seq.filter fileIsInScope
        |> Seq.map (fun path -> { Path = path; Lines = readLines path })

    let rec getDirectory fileIsInScope path =
        { Directories =
              fileSystemService.GetDirectories path
              |> Seq.map (getDirectory fileIsInScope)
          Files = getFiles fileIsInScope path }

    interface IDirectoryService with
        member this.GetDirectory fileIsInScope path =
            getDirectory fileIsInScope path
