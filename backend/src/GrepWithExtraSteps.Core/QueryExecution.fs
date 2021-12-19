[<RequireQualifiedAccess>]
module internal GrepWithExtraSteps.Core.QueryExecution

open GrepWithExtraSteps.Core.Domain
open FSharp.Control

let private toMatchingLine filePath (lineNumber, line) : MatchingLine =
    { FilePath = filePath
      LineNumber = lineNumber
      MatchingText = line }

let private toResultChunk (lineIsMatch: LineIsMatch) (file: File) : Async<ResultChunk> =
    file.Lines
    |> AsyncSeq.zip (AsyncSeq.initInfinite ((+) 1L >> int))
    |> AsyncSeq.filter (snd >> lineIsMatch)
    |> AsyncSeq.map (toMatchingLine file.Path)
    |> AsyncSeq.toListAsync

let rec searchDirectory (lineIsMatch: LineIsMatch) (directory: Directory) : AsyncSeq<ResultChunk> =
    let resultChunksFromThisDirectory =
        directory.Files
        |> AsyncSeq.ofSeq
        |> AsyncSeq.mapAsync (toResultChunk lineIsMatch)
        |> AsyncSeq.filter (not << List.isEmpty)

    let resultChunksFromSubdirectories =
        directory.Directories
        |> AsyncSeq.ofSeq
        |> AsyncSeq.collect (searchDirectory lineIsMatch)

    resultChunksFromSubdirectories
    |> AsyncSeq.append resultChunksFromThisDirectory
