namespace GrepWithExtraSteps.Core.Tests.InMemory

open System
open FSharp.Control
open GrepWithExtraSteps.Core
open GrepWithExtraSteps.Core.Domain
open Xunit
open FsCheck
open FsCheck.Xunit
open Xunit.Abstractions

module private Debugging =
    let rec getDirectoryCount (directory: Directory) : int =
        let children = directory.Directories |> Seq.toList

        match children with
        | [] -> 1
        | _ -> 1 + (children |> List.sumBy getDirectoryCount)

    let rec getFileCount (directory: Directory) : int =
        let children = directory.Directories |> Seq.toList
        let filesCount = directory.Files |> Seq.length

        match children with
        | [] -> filesCount
        | _ -> filesCount + (children |> List.sumBy getFileCount)

    let rec getLineCount (directory: Directory) : int =
        let children = directory.Directories |> Seq.toList

        let lineCount =
            directory.Files
            |> AsyncSeq.ofSeq
            |> AsyncSeq.collect (fun file -> file.Lines)
            |> AsyncSeq.toListSynchronously
            |> List.length
            |> int

        match children with
        | [] -> lineCount
        | _ -> lineCount + (children |> List.sumBy getLineCount)

[<AutoOpen>]
module private TestValues =
    let everyLineIsMatch: LineIsMatch = fun _ -> true

    let noLineIsMatch: LineIsMatch = fun _ -> false

    let emptyDirectory =
        { Directories = Seq.empty
          Files = Seq.empty }

    let someDirectory =
        { Directories =
              Seq.singleton
                  { Directories = Seq.empty
                    Files =
                        seq {
                            { Path = "/directory/file"
                              Lines =
                                  asyncSeq {
                                      yield "line 4"
                                      yield "line 5"
                                  } }
                        } }
          Files =
              seq {
                  { Path = "/file"
                    Lines =
                        asyncSeq {
                            yield "line 1"
                            yield "line 2"
                            yield "line 3"
                        } }
              } }


module private Generators =
    let nonNullStringGenerator =
        Arb.Default.String()
        |> Arb.filter (fun string -> string <> null)
        |> Arb.toGen

    let sizedFileGenerator size =
        gen {
            let! lines =
                nonNullStringGenerator
                |> Gen.listOfLength size
                |> Gen.map AsyncSeq.ofSeq

            return { Path = "somePath"; Lines = lines }
        }

    let directoryGenerator =
        let rec sizedDirectoryGenerator (size: int) : Gen<Directory> =
            gen {
                let descendantCount = Math.Max(0, size - 1)

                let! childCount =
                    Gen.choose (1, 10)
                    |> Gen.map (fun oneToTen -> Math.Min(oneToTen, descendantCount))

                let nonChildDescendantCount = descendantCount - childCount

                let! directories =
                    if childCount > 0 then
                        let descendantsPerChild = nonChildDescendantCount / childCount

                        sizedDirectoryGenerator descendantsPerChild
                        |> Gen.listOfLength childCount
                    else
                        gen { return List.empty }

                let! fileCount = Gen.choose (0, Math.Max(size, 3))

                let! lineCount =
                    Gen.choose (1, 10)
                    |> Gen.map (fun oneToTen -> Math.Min(oneToTen, size))

                let! files =
                    sizedFileGenerator lineCount
                    |> Gen.listOfLength fileCount

                return
                    { Directories = directories |> List.toSeq
                      Files = files }
            }

        Gen.sized sizedDirectoryGenerator

    let lineIsMatchGenerator =
        let randomLineIsMatchGenerator =
            gen {
                let! monkey = nonNullStringGenerator

                return (=) monkey
            }

        [ everyLineIsMatch; noLineIsMatch ]
        |> List.map Gen.constant
        |> List.append [ randomLineIsMatchGenerator ]
        |> Gen.oneof

type Arbitraries() =
    static member Directory() : Arbitrary<Directory> =
        Generators.directoryGenerator |> Arb.fromGen

    static member LineIsMatch() : Arbitrary<LineIsMatch> =
        Generators.lineIsMatchGenerator |> Arb.fromGen

type QueryExecutionTests(testOutputHelper: ITestOutputHelper) =
    [<Fact>]
    member _.``searchDirectory returns an empty seq when given an empty directory``() =
        let resultChunks =
            QueryExecution.searchDirectory everyLineIsMatch emptyDirectory
            |> AsyncSeq.toListSynchronously

        do Assert.Empty resultChunks

    [<Fact>]
    member _.``searchDirectory does not return empty result chunks - fact``() =
        let resultChunks =
            QueryExecution.searchDirectory noLineIsMatch someDirectory
            |> AsyncSeq.toListSynchronously

        do resultChunks |> Seq.iter Assert.NotEmpty

    [<Property(Arbitrary = [| typeof<Arbitraries> |])>]
    member _.``searchDirectory does not return empty result chunks - property``
        (directory: Directory)
        (lineIsMatch: LineIsMatch)
        =
        let resultChunks =
            QueryExecution.searchDirectory lineIsMatch directory
            |> AsyncSeq.toListSynchronously

        resultChunks |> List.forall (not << List.isEmpty)

    [<Fact>]
    member _.``searchDirectory returns an empty seq when given noLineIsMatch - fact``() =
        let resultChunks =
            QueryExecution.searchDirectory noLineIsMatch someDirectory
            |> AsyncSeq.toListSynchronously

        do Assert.Empty resultChunks

    [<Property(Arbitrary = [| typeof<Arbitraries> |])>]
    member _.``searchDirectory returns an empty seq when given noLineIsMatch - property``(directory: Directory) =
        let resultChunks =
            QueryExecution.searchDirectory noLineIsMatch directory
            |> AsyncSeq.toListSynchronously

        resultChunks |> List.isEmpty

    [<Fact>]
    member _.``searchDirectory finds matching lines``() =
        let lineIsMatch line =
            List.contains line [ "line 1"; "line 2"; "line 5" ]

        let matchingLines =
            QueryExecution.searchDirectory lineIsMatch someDirectory
            |> AsyncSeq.toListSynchronously
            |> List.concat

        let matchingLine1 =
            { FilePath = "/file"
              LineNumber = 1
              MatchingText = "line 1" }

        let matchingLine2 =
            { FilePath = "/file"
              LineNumber = 2
              MatchingText = "line 2" }

        let matchingLine3 =
            { FilePath = "/directory/file"
              LineNumber = 2
              MatchingText = "line 5" }

        do Assert.Equal(3, matchingLines.Length)
        do Assert.Contains(matchingLine1, matchingLines)
        do Assert.Contains(matchingLine2, matchingLines)
        do Assert.Contains(matchingLine3, matchingLines)
