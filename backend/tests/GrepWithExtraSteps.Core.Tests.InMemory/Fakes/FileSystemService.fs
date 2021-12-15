namespace GrepWithExtraSteps.Core.Tests.InMemory.Fakes

open System.IO
open System.Text
open GrepWithExtraSteps.Core.Interfaces

type internal FileSystemService
    (
        directoriesByPath: Map<string, string seq>,
        filesByPath: Map<string, string seq>,
        fileContentsByPath: Map<string, string seq>
    ) =
    let linesToStreamReader lines =
        let bytes =
            lines
            |> String.concat "\n"
            |> Encoding.UTF8.GetBytes

        let stream = new MemoryStream(bytes)

        new StreamReader(stream)
        
    interface IFileSystemService with
        member this.GetDirectories path =
            match Map.tryFind path directoriesByPath with
            | Some directories -> directories
            | None -> Seq.empty

        member this.GetFiles path =
            match Map.tryFind path filesByPath with
            | Some files -> files
            | None -> Seq.empty

        member this.GetReader path =
            match Map.tryFind path fileContentsByPath with
            | Some lines -> linesToStreamReader lines
            | None -> linesToStreamReader Seq.empty
