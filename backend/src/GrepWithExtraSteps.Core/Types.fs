namespace GrepWithExtraSteps.Core

open System
open System.IO
open FSharp.Control
open FsToolkit.ErrorHandling

module Domain =
    type DirectoryPathError =
        | PathIsNullOrEmptyString
        | DirectoryDoesNotExist

    type DirectoryPath =
        private
        | DirectoryPath of string
        static member New (directoryExists: string -> bool) path : Result<DirectoryPath, DirectoryPathError> =
            result {
                do!
                    String.IsNullOrEmpty path
                    |> Result.requireFalse PathIsNullOrEmptyString

                do!
                    directoryExists path
                    |> Result.requireTrue DirectoryDoesNotExist

                return DirectoryPath path
            }

        static member Value(DirectoryPath path) = path

    type FileIsInScope = string -> bool

    type LineIsMatch = string -> bool

    type MatchingLine =
        { FilePath: string
          LineNumber: int
          MatchingText: string }

    type ResultChunk = MatchingLine list

    type Query =
        { Directory: string
          Files: string
          Text: string }

    type File =
        { Path: string
          Lines: string AsyncSeq }

    type Directory =
        { Directories: Directory seq
          Files: File seq }

module Interfaces =
    open Domain

    type internal IDirectoryService =
        abstract GetDirectory : FileIsInScope -> DirectoryPath -> Directory

    type internal IFileSystemService =
        abstract member GetDirectories : path: string -> string seq
        abstract member GetFiles : path: string -> string seq
        abstract member GetReader : path: string -> StreamReader

    type IQueryJobService =
        abstract member StartQueryJob : DirectoryPath -> FileIsInScope -> LineIsMatch -> unit
        abstract member CancelQueryJob : unit -> unit

    type IMessageService =
        abstract member SendResultChunk : ResultChunk -> Async<unit>
        abstract member SendQueryFinished : unit -> Async<unit>
