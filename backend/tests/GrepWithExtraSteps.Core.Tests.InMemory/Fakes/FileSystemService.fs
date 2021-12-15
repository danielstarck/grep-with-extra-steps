namespace GrepWithExtraSteps.Core.Tests.InMemory.Fakes

open System.IO
open System.Text
open GrepWithExtraSteps.Core.Interfaces

type internal FileSystemService
    (
        directoriesByPath: Map<string, string seq>,
        filesByPath: Map<string, string seq>,
        linesByPath: Map<string, string seq>
    ) =
    let getValueOrEmptySeq key =
        Map.tryFind key >> Option.defaultValue Seq.empty

    interface IFileSystemService with
        member this.GetDirectories path =
            getValueOrEmptySeq path directoriesByPath

        member this.GetFiles path = getValueOrEmptySeq path filesByPath

        member this.GetReader path =
            let bytes =
                getValueOrEmptySeq path linesByPath
                |> String.concat "\n"
                |> Encoding.UTF8.GetBytes

            let stream = new MemoryStream(bytes)

            new StreamReader(stream)
