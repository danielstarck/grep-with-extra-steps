namespace GrepWithExtraSteps

open GrepWithExtraSteps.Types

type ResultService() =

    member _.GetResults
        (sendResultChunks: ResultChunk list -> Async<unit>)
        (sendQueryFinished: unit -> Async<unit>)
        : Async<unit> =
        async {
            for chunks in
                Seq.init 10 (fun index ->
                    let number = index + 1

                    { FilePath = $"Path #%d{number}"
                      LineNumber = number
                      MatchingText = $"Matching text #%d{number}" })
                |> Seq.chunkBySize 3
                |> Seq.map Seq.toList do
                do! Async.Sleep 500
                do! sendResultChunks chunks

            do! sendQueryFinished ()
        }
