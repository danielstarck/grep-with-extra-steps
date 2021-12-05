namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open FSharp.Control

type internal QueryService() =
    let toMatchingLine lineIsMatch filePath (lineNumber, line) : MatchingLine option =
        if line |> lineIsMatch then
            Some
                { FilePath = filePath
                  LineNumber = lineNumber
                  MatchingText = line }
        else
            None

    let toResultChunk (lineIsMatch: string -> bool) (file: File) : Async<ResultChunk> =
        file.Lines
        |> AsyncSeq.zip (AsyncSeq.initInfinite ((+) 1L >> int))
        |> AsyncSeq.choose (toMatchingLine lineIsMatch file.Path)
        |> AsyncSeq.toListAsync
                
    let rec searchDirectory (lineIsMatch: string -> bool) (directory: Directory) : AsyncSeq<ResultChunk> =
        let resultChunksFromThisDirectory =
            directory.Files
            |> AsyncSeq.ofSeq
            |> AsyncSeq.mapAsync (toResultChunk lineIsMatch)

        let resultChunksFromSubdirectories =
            directory.Directories
            |> AsyncSeq.ofSeq
            |> AsyncSeq.collect (searchDirectory lineIsMatch)

        resultChunksFromSubdirectories
        |> AsyncSeq.append resultChunksFromThisDirectory

    // TODO: A result chunk must never be empty
    interface IQueryService with
        member _.ExecuteQuery (directory: Directory) (lineIsMatch: string -> bool) : AsyncSeq<ResultChunk> =
            searchDirectory lineIsMatch directory
