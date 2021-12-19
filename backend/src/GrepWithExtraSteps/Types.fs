namespace GrepWithExtraSteps.Types

open GrepWithExtraSteps.Core.Domain

type ISomeHubService =
    abstract member StartQuery : Query -> unit

    abstract member CancelQuery : unit -> unit
