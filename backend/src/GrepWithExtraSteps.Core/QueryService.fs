namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open System.Text.RegularExpressions

type QueryService(fileSystemService: IFileSystemService) =
    let isMatch (queryText: string) line = Regex.IsMatch(line, queryText)

    let toMatchingLine (queryText: string) filePath (lineNumber, line) : MatchingLine option =
        if line |> isMatch queryText then
            Some
                { FilePath = filePath
                  LineNumber = lineNumber
                  MatchingText = line }
        else
            None

    let searchFile (queryText: string) (filePath: string) : Async<ResultChunk> =
        async {
            let! file = fileSystemService.ReadFile filePath

            return
                file.Lines
                |> Seq.zip (Seq.initInfinite (fun index -> index + 1))
                |> Seq.choose (toMatchingLine queryText file.Path)
                |> Seq.toList
        }

    let rec searchDirectory (queryText: string) (directory: Directory) : Async<ResultChunk seq> =
        async {
            let! filesFromThisDirectory =
                directory.Files
                |> Seq.map (searchFile queryText)
                |> Async.Parallel

            let! filesFromSubdirectories =
                directory.Directories
                |> Seq.map (searchDirectory queryText)
                |> Async.Parallel

            return
                filesFromSubdirectories
                |> Seq.concat
                |> Seq.append filesFromThisDirectory
        }

    interface IQueryService with
        member _.ExecuteQuery(query: Query) : Async<ResultChunk seq> =
            async {
                let! directory = fileSystemService.GetDirectory query.Directory

                return! searchDirectory query.Text directory
            }
