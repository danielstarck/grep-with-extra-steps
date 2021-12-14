namespace GrepWithExtraSteps.Core.Tests.InMemory.Fakes

open GrepWithExtraSteps.Core
open GrepWithExtraSteps.Core.Interfaces

type internal Message =
    | ResultChunk of Domain.ResultChunk
    | QueryFinished

type internal MessageService(messageSink: System.Collections.Generic.List<Message>) =
    interface IMessageService with
        member this.SendResultChunk chunk =
            async { do messageSink.Add <| ResultChunk chunk }

        member this.SendQueryFinished() =
            async { do messageSink.Add QueryFinished }
