port module Main exposing (Model(..), Msg(..), init, main, subscriptions, update, view)

import Browser
import Html exposing (Html, pre, text)
import Http


main =
    Browser.element
        { init = init
        , update = update
        , subscriptions = subscriptions
        , view = view
        }


type Model
    = Loading
    | Success String


init : () -> ( Model, Cmd Msg )
init _ =
    ( Loading, Cmd.none )


type Msg
    = SignalRMessage String


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        SignalRMessage string ->
            ( Success ("Received message from SignalR: " ++ string), Cmd.none )


port updates : (String -> msg) -> Sub msg


subscriptions : Model -> Sub Msg
subscriptions model =
    updates SignalRMessage


view : Model -> Html Msg
view model =
    case model of
        Loading ->
            text "Loading..."

        Success fullText ->
            pre [] [ text fullText ]
