[<RequireQualifiedAccess>]
module internal GrepWithExtraSteps.Core.QueryExecution

open GrepWithExtraSteps.Core.Domain
open FSharp.Control

let private toMatchingLine filePath (lineNumber, line) : MatchingLine =
    { FilePath = filePath
      LineNumber = lineNumber
      MatchingText = line }

let private toResultChunk (lineIsMatch: string -> bool) (file: File) : Async<ResultChunk> =
    file.Lines
    |> AsyncSeq.zip (AsyncSeq.initInfinite ((+) 1L >> int))
    |> AsyncSeq.filter (snd >> lineIsMatch)
    |> AsyncSeq.map (toMatchingLine file.Path)
    |> AsyncSeq.toListAsync
            
// TODO: A result chunk must never be empty
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
