namespace GrepWithExtraSteps.Core

open System.IO
open FSharp.Control

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

    type File = { Path: string; Lines: string AsyncSeq }

    type Directory =
        { Directories: Directory seq
          Files: File seq }

module Interfaces =
    open Domain

    type IFileSystemService =
        abstract member GetDirectories : path: string -> string seq
        abstract member GetFiles : path: string -> string seq
        abstract member GetReader : path: string -> StreamReader

    type IMessageService =
        abstract member SendResultChunk : ResultChunk -> Async<unit>
        abstract member SendQueryFinished : unit -> Async<unit>

    type IQueryService =
        abstract ExecuteQuery : Directory -> lineIsMatch: (string -> bool) -> AsyncSeq<ResultChunk>

    type IDirectoryService =
        abstract GetDirectory : fileIsInScope: (string -> bool) -> path: string -> Directory
