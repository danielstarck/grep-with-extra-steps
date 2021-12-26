namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core
open GrepWithExtraSteps.Core.Interfaces
open FSharp.Control

type internal QueryJobService(directoryService: IDirectoryService, messageService: IMessageService) =
    interface IQueryJobService with
        member _.StartQueryJob directoryPath fileIsInScope lineIsMatch : Async<unit> =
            async {
                let chunks =
                    directoryService.GetDirectory fileIsInScope directoryPath
                    |> QueryExecution.searchDirectory lineIsMatch

                do!
                    chunks
                    |> AsyncSeq.iterAsync messageService.SendResultChunk

                do! messageService.SendQueryFinished()
            }
