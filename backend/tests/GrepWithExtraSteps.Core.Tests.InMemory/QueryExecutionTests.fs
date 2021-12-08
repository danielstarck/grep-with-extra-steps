namespace GrepWithExtraSteps.Core.Tests.InMemory

open FSharp.Control
open FsCheck
open GrepWithExtraSteps.Core
open GrepWithExtraSteps.Core.Domain
open Xunit

type QueryExecutionTests() =

    let everyLineIsMatch: LineIsMatch = fun _ -> true

    let noLineIsMatch: LineIsMatch = fun _ -> false

    let emptyDirectory =
        { Directories = Seq.empty
          Files = Seq.empty }
    
    let someDirectory =
        { Directories = Seq.empty
          Files =
              seq {
                  { Path = "/file"
                    Lines =
                        asyncSeq {
                            yield "line 1"
                            yield "line 2"
                            yield "line 3"
                        }}
              }}

    [<Fact>]
    member _.``searchDirectory returns an empty seq when given an empty directory``() =
        let resultChunks =
            QueryExecution.searchDirectory everyLineIsMatch emptyDirectory
            |> AsyncSeq.toListSynchronously

        do Assert.Empty(resultChunks)

    [<Fact>]
    member _.``searchDirectory does not return empty result chunks``() =
        let resultChunks =
            QueryExecution.searchDirectory noLineIsMatch someDirectory
            |> AsyncSeq.toListSynchronously

        do resultChunks |> Seq.iter Assert.NotEmpty

// TODO: figure out how to generate Directory

// ``searchDirectory returns an empty seq when given noLineIsMatch`` (for arbitrary directory)
// ``searchDirectory does not return empty result chunks`` (for arbitrary directory)
