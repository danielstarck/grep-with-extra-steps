namespace GrepWithExtraSteps.Core

open GrepWithExtraSteps.Core.Interfaces
open System.IO

type internal FileSystemService() =
    interface IFileSystemService with
        member this.GetDirectories path = Directory.EnumerateDirectories path

        member this.GetFiles path = Directory.EnumerateFiles path

        member this.GetReader path = File.OpenText path
