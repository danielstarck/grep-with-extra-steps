namespace GrepWithExtraSteps

type ResultService() =

    member _.GetResults
        (sendResultChunk: string -> Async<unit>)
        (sendQueryFinished: unit -> Async<unit>)
        : Async<unit> =
        async {
            for result in Seq.init 10 (fun index -> $"Result #{index + 1}") do
                do! Async.Sleep 200
                do! sendResultChunk result

            do! sendQueryFinished ()
        }
