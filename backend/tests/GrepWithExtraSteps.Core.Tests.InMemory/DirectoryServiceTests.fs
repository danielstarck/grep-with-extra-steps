namespace GrepWithExtraSteps.Core.Tests.InMemory

open FSharp.Control
open GrepWithExtraSteps.Core
open GrepWithExtraSteps.Core.Domain
open GrepWithExtraSteps.Core.Interfaces
open GrepWithExtraSteps.Core.Tests.InMemory
open Xunit

type DirectoryServiceTests() =
    let getDirectoryService directoriesByPath filesByPath linesByPath : IDirectoryService =
        let fileSystemService: IFileSystemService =
            upcast Fakes.FileSystemService(directoriesByPath, filesByPath, linesByPath)

        upcast DirectoryService(fileSystemService)

    let everyFileIsInScope: FileIsInScope = Predicate.alwaysTrue

    let root: DirectoryPath =
            Result.getUnsafe
            <| DirectoryPath.New(Predicate.alwaysTrue) "/"

    [<Fact>]
    member _.``GetDirectory returns empty when given empty FileSystemService``() =
        let directoryService =
            getDirectoryService Map.empty Map.empty Map.empty

        let directory =
            directoryService.GetDirectory everyFileIsInScope root

        do Assert.Empty directory.Directories
        do Assert.Empty directory.Files

    // TODO: add some property based analogue
    [<Fact>]
    member _.``GetDirectory returns expected directories, files and lines``() =
        let directoriesByPath =
            [ "/",
              seq {
                  yield "/directory1"
                  yield "/directory2"
              }
              "/directory2", seq { yield "/directory2/directory3" } ]
            |> Map.ofList

        let filesByPath =
            [ "/directory2/directory3",
              seq {
                  yield "/directory2/directory3/file1"
                  yield "/directory2/directory3/file2"
              } ]
            |> Map.ofList

        let linesByPath =
            [ "/directory2/directory3/file1",
              seq {
                  yield "line1"
                  yield "line2"
              }
              "/directory2/directory3/file2",
              seq {
                  yield "line3"
                  yield "line4"
                  yield "line5"
              } ]
            |> Map.ofList

        let directoryService =
            getDirectoryService directoriesByPath filesByPath linesByPath

        let directory =
            directoryService.GetDirectory everyFileIsInScope root

        let directoriesInRoot = directory.Directories |> Seq.toList

        do Assert.Equal(2, List.length directoriesInRoot)
        do Assert.Empty directory.Files

        let directory1 = directoriesInRoot |> List.item 0

        do Assert.Empty directory1.Directories
        do Assert.Empty directory1.Files

        let directory2 = directoriesInRoot |> List.item 1

        let directory3 = Assert.Single directory2.Directories
        do Assert.Empty directory2.Files

        let filesInDirectory3 = directory3.Files |> Seq.toList

        do Assert.Empty directory3.Directories
        do Assert.Equal(2, List.length filesInDirectory3)

        let file1 = filesInDirectory3 |> List.item 0

        do Assert.Equal("/directory2/directory3/file1", file1.Path)

        let linesInFile1 =
            file1.Lines |> AsyncSeq.toListSynchronously

        do Assert.Equal(2, List.length linesInFile1)
        do Assert.Equal("line1", linesInFile1 |> List.item 0)
        do Assert.Equal("line2", linesInFile1 |> List.item 1)

        let file2 = filesInDirectory3 |> List.item 1

        do Assert.Equal("/directory2/directory3/file2", file2.Path)

        let linesInFile2 =
            file2.Lines |> AsyncSeq.toListSynchronously

        do Assert.Equal(3, List.length linesInFile2)
        do Assert.Equal("line3", linesInFile2 |> List.item 0)
        do Assert.Equal("line4", linesInFile2 |> List.item 1)
        do Assert.Equal("line5", linesInFile2 |> List.item 2)
