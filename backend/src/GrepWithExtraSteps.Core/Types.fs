namespace GrepWithExtraSteps.Core

module Domain =
    type MatchingLine =
        { FilePath: string
          LineNumber: int
          MatchingText: string }
          
    type ResultChunk = MatchingLine list

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
        { Directories: Directory list
          Files: string list }

module Interfaces =
    open Domain

    type IFileSystemService =
        abstract member ReadFile : string -> Async<File>
        abstract member GetDirectory : string -> Async<Directory>

    type IMessageService =
        abstract member SendResultChunk : ResultChunk -> Async<unit>
        abstract member SendQueryFinished : unit -> Async<unit>

    type IQueryService =
        abstract ExecuteQuery : Query -> Async<ResultChunk seq>
