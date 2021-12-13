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

module private Generators =
    let sizedFileGenerator size =
        gen {
            let! lines =
                Arb.Default.String()
                |> Arb.filter (fun string -> string <> null)
                |> Arb.toGen
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

type Arbitraries() =
    static member Directory() : Arbitrary<Directory> =
        Generators.directoryGenerator |> Arb.fromGen

type QueryExecutionTests(testOutputHelper: ITestOutputHelper) =

    let everyLineIsMatch: LineIsMatch = fun _ -> true

    let noLineIsMatch: LineIsMatch = fun _ -> false

    let emptyDirectory =
        { Directories = Seq.empty
          Files = Seq.empty }

    let someDirectory =
        { Directories = Seq.empty
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
    member _.``searchDirectory does not return empty result chunks - property``(directory: Directory) =
        let resultChunks =
            QueryExecution.searchDirectory everyLineIsMatch directory
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
