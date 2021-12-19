namespace GrepWithExtraSteps.Core

open System.IO
open FSharp.Control

module Domain =
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

    type FileIsInScope = string -> bool

module Interfaces =
    open Domain

    type internal IDirectoryService =
        abstract GetDirectory : fileIsInScope: FileIsInScope -> path: string -> Directory

    type internal IFileSystemService =
        abstract member GetDirectories : path: string -> string seq
        abstract member GetFiles : path: string -> string seq
        abstract member GetReader : path: string -> StreamReader

    type internal IPathService =
        abstract member GetFilename : path: string -> string

    type IQueryJobService =
        abstract member StartQueryJob : Query -> unit
        abstract member CancelQueryJob : unit -> unit

    type IMessageService =
        abstract member SendResultChunk : ResultChunk -> Async<unit>
        abstract member SendQueryFinished : unit -> Async<unit>
