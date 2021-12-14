namespace GrepWithExtraSteps.Core

open System.Threading
open System.Text.RegularExpressions
open GrepWithExtraSteps.Core
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open FSharp.Control

type internal QueryJobService(directoryService: IDirectoryService, messageService: IMessageService) =
    let mutable ctsOption: CancellationTokenSource option = None

    interface IQueryJobService with
        member _.StartQueryJob(query: Query) : unit =
            let queryJobAsync =
                async {
                    let fileIsInScope (path: string) =
                        let filename = System.IO.Path.GetFileName(path)
                        
                        Regex.IsMatch(filename, query.Files)
                    
                    let chunks =
                        directoryService.GetDirectory fileIsInScope query.Directory
                        |> QueryExecution.searchDirectory (fun line -> Regex.IsMatch(line, query.Text))

                    do!
                        chunks
                        |> AsyncSeq.iterAsync messageService.SendResultChunk

                    do! messageService.SendQueryFinished()
                }

            // TODO: dispose?
            let cts = new CancellationTokenSource()
            do ctsOption <- Some cts

            do Async.Start(queryJobAsync, cancellationToken = cts.Token)

        member _.CancelQueryJob() : unit =
            match ctsOption with
            | Some cts ->
                do ctsOption <- None
                do cts.Cancel()
            | None -> ()
