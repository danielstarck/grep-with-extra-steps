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

    static member WithTestData: IFileSystemService =
        let directoriesByPath =
            [ "/",
              seq {
                  yield "/directory1"
                  yield "/directory2"
              }
              "/directory2", seq { yield "/directory2/directory3" } ]
            |> Map.ofList

        let filesByPath =
            [ "/directory2/directory3",
              seq {
                  yield "/directory2/directory3/file1"
                  yield "/directory2/directory3/file2"
              } ]
            |> Map.ofList

        let linesByPath =
            [ "/directory2/directory3/file1",
              seq {
                  yield "line1"
                  yield "line2"
              }
              "/directory2/directory3/file2",
              seq {
                  yield "line3"
                  yield "line4"
                  yield "line5"
              } ]
            |> Map.ofList

        upcast FileSystemService(directoriesByPath, filesByPath, linesByPath)
