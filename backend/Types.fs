namespace GrepWithExtraSteps.Types

open System.Threading.Tasks

type IQueryService =
    abstract member StartQuery: unit -> Task
    abstract member CancelQuery: unit -> Task

