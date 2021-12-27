namespace GrepWithExtraSteps.Core.Tests.InMemory

open GrepWithExtraSteps.Base
open GrepWithExtraSteps.Core
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open GrepWithExtraSteps.Core.Tests.InMemory
open Xunit

type QueryJobServiceTests() =
    let assertLastMessageIsQueryFinished messageSink =
        do Assert.NotEmpty messageSink
        do Assert.Equal(Fakes.QueryFinished, Seq.last messageSink)

    let assertOnlyMessageIsQueryFinished messageSink =
        do Assert.Single messageSink |> ignore
        do assertLastMessageIsQueryFinished messageSink

    let assertContainsMatchingLines
        (matchingLines: MatchingLine list)
        (messageSink: System.Collections.Generic.List<Fakes.Message>)
        =
        let sortedExpected = matchingLines |> List.sort

        let sortedActual =
            messageSink
            |> Seq.choose
                (function
                | Fakes.ResultChunk chunk -> Some chunk
                | Fakes.QueryFinished -> None)
            |> Seq.concat
            |> Seq.sort
            |> Seq.toList

        do Assert.Equal<MatchingLine list>(sortedExpected, sortedActual)

    [<Fact>]
    member _.``Empty file system returns no results``() =
        let fileSystemService: IFileSystemService =
            upcast Fakes.FileSystemService(Map.empty, Map.empty, Map.empty)

        let directoryService: IDirectoryService =
            upcast DirectoryService(fileSystemService)

        let messageSink = System.Collections.Generic.List()
        let messageService = Fakes.MessageService messageSink

        let queryJobService: IQueryJobService =
            upcast QueryJobService(directoryService, messageService)

        let directoryPath =
            DirectoryPath.New Predicate.alwaysTrue "somePath"
            |> Result.getUnsafe

        let fileIsInScope = Predicate.alwaysTrue
        let lineIsMatch = Predicate.alwaysTrue

        do
            queryJobService.StartQueryJob directoryPath fileIsInScope lineIsMatch
            |> Async.RunSynchronously

        do assertOnlyMessageIsQueryFinished messageSink

    [<Fact>]
    member _.``Query job sends expected messages``() =
        let directoryService: IDirectoryService =
            upcast DirectoryService Fakes.FileSystemService.WithTestData

        let messageSink = System.Collections.Generic.List()
        let messageService = Fakes.MessageService messageSink

        let queryJobService: IQueryJobService =
            upcast QueryJobService(directoryService, messageService)

        let directoryPath =
            DirectoryPath.New Predicate.alwaysTrue "/"
            |> Result.getUnsafe

        let fileIsInScope = Predicate.alwaysTrue
        let lineIsMatch = Predicate.alwaysTrue

        do
            queryJobService.StartQueryJob directoryPath fileIsInScope lineIsMatch
            |> Async.RunSynchronously

        let expectedMatchingLines =
            [ { FilePath = "/directory2/directory3/file1"
                LineNumber = 1
                MatchingText = "line1" }
              { FilePath = "/directory2/directory3/file1"
                LineNumber = 2
                MatchingText = "line2" }
              { FilePath = "/directory2/directory3/file2"
                LineNumber = 1
                MatchingText = "line3" }
              { FilePath = "/directory2/directory3/file2"
                LineNumber = 2
                MatchingText = "line4" }
              { FilePath = "/directory2/directory3/file2"
                LineNumber = 3
                MatchingText = "line5" } ]

        do
            messageSink
            |> assertContainsMatchingLines expectedMatchingLines

        do assertLastMessageIsQueryFinished messageSink

// TODO: property - last message sent is always QueryFinished
