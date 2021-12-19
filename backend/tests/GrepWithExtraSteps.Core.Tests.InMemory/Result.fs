[<RequireQualifiedAccess>]
module internal GrepWithExtraSteps.Core.Tests.InMemory.Result

let rec getUnsafe =
    function
    | Ok ok -> ok
    | Error error -> failwith $"%s{nameof getUnsafe} failed because the result was of type Error: %A{error}."
