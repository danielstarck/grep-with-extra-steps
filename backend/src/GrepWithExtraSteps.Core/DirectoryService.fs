namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open FSharp.Control

// TODO: investigate Directory.EnumerateFileSystemEntries
type internal DirectoryService(fileSystemService: IFileSystemService) =
    let readLines path : AsyncSeq<string> =
        asyncSeq {
            use reader = fileSystemService.GetReader path

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
        member this.GetDirectory fileIsInScope directoryPath =
            getDirectory fileIsInScope (DirectoryPath.Value directoryPath)
