namespace GrepWithExtraSteps

open System
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
            |> Result.map (fun regex -> System.IO.Path.GetFileName >> regex.IsMatch)

    let getLineIsMatch (text: string) : Result<LineIsMatch, QueryValidationError> =
        result {
            let! regex = getRegex TextIsNotValidRegex text

            return regex.IsMatch
        }

    let validateQuery query =
        result {
            let! directoryPath =
                // TODO: fun _ -> true
                DirectoryPath.New(fun _ -> true) query.Directory
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

                do queryJobService.StartQueryJob directoryPath fileIsInScope lineIsMatch
            | Error error ->
                do logger.LogInformation $"Query validation failed with error %A{error}"

                // TODO: send error message to frontend
                ()

        member _.CancelQuery() : unit = do queryJobService.CancelQueryJob()
