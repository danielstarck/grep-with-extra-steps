namespace GrepWithExtraSteps

open System.Threading.Tasks
open Microsoft.AspNetCore.SignalR

type SomeHub() =
    inherit Hub()

    member this.SendMessage(name: string) : Task =
        this.Clients.All.SendAsync("TheMessageName", name)
