module QueryExecutionTests

open FSharp.Control
open GrepWithExtraSteps.Core
open GrepWithExtraSteps.Core.Domain
open Xunit

let everyLineIsMatch: LineIsMatch = fun _ -> true

let emptyDirectory =
    { Directories = Seq.empty
      Files = Seq.empty }

[<Fact>]
let ``searchDirectory returns an empty seq when given an empty directory`` () =
    let resultChunks =
        QueryExecution.searchDirectory everyLineIsMatch emptyDirectory
        |> AsyncSeq.toListSynchronously

    Assert.Empty(resultChunks)
