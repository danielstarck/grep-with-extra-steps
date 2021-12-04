namespace GrepWithExtraSteps

open GrepWithExtraSteps.Core.Interfaces
open GrepWithExtraSteps.Core.Domain

type FileSystemService() =
    let someLines =
        seq {
            "The first line."
            "The second line."
            "The third line."
        }

    let someDirectory =
        { Directories =
            [ { Directories = List.empty
                Files =
                  [ "/sub1/file4.txt"
                    "/sub1/file5.txt"
                    "/sub1/file6.txt" ] } ]
          Files =
            [ "/file1.txt"
              "/file2.txt"
              "/file3.txt" ] }

    interface IFileSystemService with
        member _.ReadFile file =
            async { return { Path = file; Lines = someLines } }

        member _.GetDirectory directory = async { return someDirectory }
