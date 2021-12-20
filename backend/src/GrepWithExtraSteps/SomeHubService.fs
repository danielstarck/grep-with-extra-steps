namespace GrepWithExtraSteps

open System
open System.IO
open System.Threading
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open GrepWithExtraSteps.Types
open Microsoft.Extensions.Logging
open FsToolkit.ErrorHandling

type QueryValidationError =
    | DirectoryPathError of DirectoryPathError
    | FilesIsNotValidRegex
    | TextIsNotValidRegex

type SomeHubService(logger: ILogger<SomeHubService>, queryJobService: IQueryJobService) =
    let mutable ctsOption: CancellationTokenSource option = None

    let getRegex error regex =
        try
            Ok <| System.Text.RegularExpressions.Regex regex
        with
        | _ -> Error error

    let getFileIsInScope (files: string) : Result<FileIsInScope, QueryValidationError> =
        if String.IsNullOrEmpty files then
            Ok <| fun _ -> true
        else
            getRegex FilesIsNotValidRegex files
            |> Result.map (fun regex -> Path.GetFileName >> regex.IsMatch)

    let getLineIsMatch (text: string) : Result<LineIsMatch, QueryValidationError> =
        result {
            let! regex = getRegex TextIsNotValidRegex text

            return regex.IsMatch
        }

    let validateQuery query =
        result {
            let! directoryPath =
                DirectoryPath.New Directory.Exists query.Directory
                |> Result.mapError DirectoryPathError

            let! fileIsInScope = getFileIsInScope query.Files
            let! lineIsMatch = getLineIsMatch query.Text

            return directoryPath, fileIsInScope, lineIsMatch
        }

    interface ISomeHubService with
        member _.StartQuery(query: Query) : unit =
            match validateQuery query with
            | Ok (directoryPath, fileIsInScope, lineIsMatch) ->
                do logger.LogInformation "Query validation succeeded"

                let queryJobAsync =
                    queryJobService.StartQueryJob directoryPath fileIsInScope lineIsMatch

                // TODO: dispose?
                let cts = new CancellationTokenSource()
                do ctsOption <- Some cts

                do Async.Start(queryJobAsync, cancellationToken = cts.Token)
            | Error error ->
                do logger.LogInformation $"Query validation failed with error %A{error}"

                // TODO: send error message to frontend
                ()

        member _.CancelQuery() : unit =
            match ctsOption with
            | Some cts ->
                do ctsOption <- None
                do cts.Cancel()
            | None -> ()
