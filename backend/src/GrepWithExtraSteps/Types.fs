namespace GrepWithExtraSteps.Types

open GrepWithExtraSteps.Core.Domain

type IQueryHubService =
    abstract member StartQuery : Query -> unit

    abstract member CancelQuery : unit -> unit
