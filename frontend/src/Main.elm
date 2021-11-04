port module Main exposing (..)

import Browser
import Element
import Element.Background
import Element.Border
import Element.Input
import Html
import Json.Decode
import Json.Encode


main : Program () Model Msg
main =
    Browser.element
        { init = init
        , update = update
        , subscriptions = subscriptions
        , view = view
        }


type Status
    = Idle
    | ExecutingQuery


type alias Model =
    { status : Status
    , rows : List String
    }


init : () -> ( Model, Cmd Msg )
init _ =
    ( { status = Idle
      , rows = []
      }
    , Cmd.none
    )


type ServerMessage
    = ResultChunk String
    | QueryFinished
    | Unexpected Json.Encode.Value


type Msg
    = ServerMessage ServerMessage
    | StartQuery
    | CancelQuery
    | DirectoryChanged String
    | FilesChanged String
    | TextChanged String


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        ServerMessage message ->
            case message of
                ResultChunk chunk ->
                    case model.status of
                        ExecutingQuery ->
                            ( { model | rows = model.rows ++ [ chunk ] }, Cmd.none )

                        Idle ->
                            ( model, Cmd.none )

                QueryFinished ->
                    ( { model | status = Idle }, Cmd.none )

                Unexpected _ ->
                    ( model, Cmd.none )

        StartQuery ->
            ( { model | status = ExecutingQuery, rows = [] }, sendMessage "StartQuery" )

        CancelQuery ->
            case model.status of
                ExecutingQuery ->
                    ( { model | status = Idle }, sendMessage "CancelQuery" )

                Idle ->
                    ( model, Cmd.none )

        _ ->
            ( model, Cmd.none )


port sendMessage : String -> Cmd msg


port receiveMessage : (Json.Encode.Value -> msg) -> Sub msg


decodeServerMessage : Json.Encode.Value -> ServerMessage
decodeServerMessage json =
    let
        resultChunkDecoder =
            Json.Decode.string
                |> Json.Decode.field "chunk"
                |> Json.Decode.andThen
                    (\chunk -> Json.Decode.succeed <| ResultChunk chunk)

        serverMessageDecoder =
            Json.Decode.string
                |> Json.Decode.field "tag"
                |> Json.Decode.andThen
                    (\tag ->
                        case tag of
                            "ResultChunk" ->
                                resultChunkDecoder

                            "QueryFinished" ->
                                Json.Decode.succeed QueryFinished

                            _ ->
                                Json.Decode.fail <| "Unexpected tag: " ++ tag
                    )
    in
    case Json.Decode.decodeValue serverMessageDecoder json of
        Ok serverMessage ->
            serverMessage

        Err _ ->
            Unexpected json


subscriptions : Model -> Sub Msg
subscriptions _ =
    receiveMessage (decodeServerMessage >> ServerMessage)


view : Model -> Html.Html Msg
view model =
    let
        buttonAttributes =
            [ Element.Background.color <| Element.rgb255 182 182 253, Element.Border.width 1, Element.padding 2 ]

        directoryInput =
            Element.Input.text
                []
                { onChange = DirectoryChanged
                , text = ""
                , placeholder = Nothing
                , label = Element.Input.labelLeft [] <| Element.text "Directory"
                }

        filesInput =
            Element.Input.text
                []
                { onChange = FilesChanged
                , text = ""
                , placeholder = Nothing
                , label = Element.Input.labelLeft [] <| Element.text "Files"
                }

        textInput =
            Element.Input.text
                []
                { onChange = TextChanged
                , text = ""
                , placeholder = Nothing
                , label = Element.Input.labelLeft [] <| Element.text "Text"
                }

        queryInput =
            Element.column [] [ directoryInput, filesInput, textInput ]

        startButton =
            Element.Input.button buttonAttributes { onPress = Just StartQuery, label = Element.text "Start" }

        cancelButton =
            Element.Input.button buttonAttributes { onPress = Just CancelQuery, label = Element.text "Cancel" }

        control =
            Element.row [ Element.spacing 2 ] [ startButton, cancelButton ]

        statusText =
            case model.status of
                Idle ->
                    "Idle"

                ExecutingQuery ->
                    "Executing query"

        status =
            Element.el [ Element.padding 2 ] <| Element.text statusText

        result =
            model.rows
                |> List.map (\row -> Element.text row)
                |> Element.column []
    in
    [ queryInput, control, status, result ]
        |> Element.column [ Element.Background.color (Element.rgb255 210 210 210) ]
        |> Element.layout []
