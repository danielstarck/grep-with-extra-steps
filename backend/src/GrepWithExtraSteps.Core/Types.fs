namespace GrepWithExtraSteps.Core

module Domain =
    type ResultChunk =
        { FilePath: string
          LineNumber: int
          MatchingText: string }

    type Query =
        { Directory: string
          Files: string
          Text: string }

    // TODO: private constructor, module with same name, all that good stuff
    type ValidQuery = private ValidQuery of Query

    type ValidationError = | DirectoryDoesNotExist

    type ValidateQuery = Query -> Result<ValidQuery, ValidationError>

    module ValidQuery =
        let validate query =
            if true then
                Ok <| ValidQuery query
            else
                Error <| DirectoryDoesNotExist

        let value (ValidQuery query) = query

    type File = { Path: string; Lines: string seq }

    type Directory =
        | Directories of Directory list
        | Files of File list

module Interfaces =
    open Domain

    type IFileSystemService =
        abstract member ReadFile : string -> string seq
    // abstract member GetDirectory

    type IMessageService =
        abstract member SendResultChunks : ResultChunk list -> Async<unit>
        abstract member SendQueryFinished : unit -> Async<unit>

    type IQueryService =
        abstract ExecuteQuery : Query -> unit
        abstract CancelQuery : unit -> unit
