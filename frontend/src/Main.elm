port module Main exposing (..)

import Browser
import Browser.Events
import Element exposing (Element)
import Element.Background
import Element.Border
import Element.Events
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
    , lines : List String
    , resizing : Maybe ResizeOperation
    , fileColumnWidth : Int
    }


type alias ResizeOperation =
    { initialSize : Int
    , initialMouseX : Maybe Int
    }


init : () -> ( Model, Cmd Msg )
init _ =
    ( { status = Idle
      , lines = []
      , resizing = Nothing
      , fileColumnWidth = 200
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
    | MouseDownMsg -- TODO: include payload to identify element
    | MouseUpMsg -- TODO: rename to GlobalMouseUpMsg
    | MouseMoveMsg Int Int


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        ServerMessage message ->
            case message of
                ResultChunk chunk ->
                    case model.status of
                        ExecutingQuery ->
                            ( { model | lines = model.lines ++ [ chunk ] }, Cmd.none )

                        _ ->
                            ( model, Cmd.none )

                QueryFinished ->
                    ( { model | status = Idle }, Cmd.none )

                Unexpected _ ->
                    ( model, Cmd.none )

        StartQuery ->
            ( { model | status = ExecutingQuery, lines = [] }, sendMessage "StartQuery" )

        CancelQuery ->
            case model.status of
                ExecutingQuery ->
                    ( { model | status = Idle }, sendMessage "CancelQuery" )

                _ ->
                    ( model, Cmd.none )

        MouseDownMsg ->
            ( { model | resizing = Just { initialSize = model.fileColumnWidth, initialMouseX = Nothing } }, Cmd.none )

        MouseUpMsg ->
            ( { model | resizing = Nothing }, Cmd.none )

        MouseMoveMsg x _ ->
            case model.resizing of
                Just resizing ->
                    -- TODO: somehow disable mouse y movement when resizing
                    case resizing.initialMouseX of
                        Just initialMouseX ->
                            let
                                deltaX =
                                    x - initialMouseX

                                newSize =
                                    max 10 (resizing.initialSize + deltaX)
                            in
                            ( { model | fileColumnWidth = newSize }, Cmd.none )

                        Nothing ->
                            ( { model | resizing = Just { resizing | initialMouseX = Just x } }, Cmd.none )

                Nothing ->
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
subscriptions model =
    let
        serverMessageSubscription =
            Just <| receiveMessage (decodeServerMessage >> ServerMessage)

        mouseMoveSubscription =
            model.resizing
                |> Maybe.map
                    (\_ ->
                        Browser.Events.onMouseMove <|
                            Json.Decode.map2 MouseMoveMsg
                                (Json.Decode.field "pageX" Json.Decode.int)
                                (Json.Decode.field "pageY" Json.Decode.int)
                    )
    in
    [ serverMessageSubscription
    , mouseMoveSubscription
    ]
        |> List.concatMap
            (\maybeSub ->
                case maybeSub of
                    Just sub ->
                        [ sub ]

                    Nothing ->
                        []
            )
        |> Sub.batch


directoryInput : Element Msg
directoryInput =
    Element.Input.text
        []
        { onChange = DirectoryChanged
        , text = ""
        , placeholder = Nothing
        , label = Element.Input.labelLeft [] <| Element.text "Directory"
        }


filesInput : Element Msg
filesInput =
    Element.Input.text
        []
        { onChange = FilesChanged
        , text = ""
        , placeholder = Nothing
        , label = Element.Input.labelLeft [] <| Element.text "Files"
        }


textInput : Element Msg
textInput =
    Element.Input.text
        []
        { onChange = TextChanged
        , text = ""
        , placeholder = Nothing
        , label = Element.Input.labelLeft [] <| Element.text "Text"
        }


queryInput : Element.Element Msg
queryInput =
    Element.column [] [ directoryInput, filesInput, textInput ]


buttonAttributes : List (Element.Attr () msg)
buttonAttributes =
    [ Element.Background.color <| Element.rgb255 182 182 253, Element.Border.width 1, Element.padding 2 ]


startButton : Element Msg
startButton =
    Element.Input.button buttonAttributes { onPress = Just StartQuery, label = Element.text "Start" }


cancelButton : Element Msg
cancelButton =
    Element.Input.button buttonAttributes { onPress = Just CancelQuery, label = Element.text "Cancel" }


control : Element Msg
control =
    Element.row [ Element.spacing 2 ] [ startButton, cancelButton ]


getStatus : Model -> Element.Element msg
getStatus model =
    let
        statusText =
            case model.status of
                Idle ->
                    "Idle"

                ExecutingQuery ->
                    "Executing query"
    in
    Element.el [ Element.padding 2 ] <| Element.text <| statusText


getFileColumnHeader : Model -> Element Msg
getFileColumnHeader model =
    getColumnHeaderWithBorder (Just MouseDownMsg) model.fileColumnWidth "File"


lineColumnHeader : Element Msg
lineColumnHeader =
    getColumnHeaderWithBorder Nothing lineColumnWidth "Line"


getColumnHeaderWithBorder : Maybe Msg -> Int -> String -> Element Msg
getColumnHeaderWithBorder maybeMouseDownMsg width title =
    Element.row [] [ getColumnHeader width title, getColumnBorder maybeMouseDownMsg ]


getColumnHeader : Int -> String -> Element Msg
getColumnHeader width title =
    Element.text title
        |> Element.el
            [ Element.Background.color <| Element.rgb255 120 120 253
            , Element.width <| Element.px width
            , Element.clip
            ]


getColumnBorder : Maybe Msg -> Element Msg
getColumnBorder maybeMouseDownMsg =
    let
        baseAttributes =
            [ Element.Background.color <| Element.rgb255 0 0 0
            , Element.width <| Element.px 5
            , Element.height Element.fill
            ]

        attributes =
            case maybeMouseDownMsg of
                Just msg ->
                    Element.Events.onMouseDown msg :: baseAttributes

                Nothing ->
                    baseAttributes
    in
    Element.el attributes Element.none


columnBorder : Element Msg
columnBorder =
    getColumnBorder Nothing


getColumnHeaders : Model -> Element Msg
getColumnHeaders model =
    [ getFileColumnHeader model, lineColumnHeader ]
        |> Element.row []


lineToRowElement : Model -> String -> Element.Element Msg
lineToRowElement model line =
    [ line |> getFileCell model, line |> getLineCell ]
        |> Element.row []


getFileCell : Model -> String -> Element Msg
getFileCell model line =
    getCellWithBorder model.fileColumnWidth line


lineColumnWidth : Int
lineColumnWidth =
    300


getLineCell : String -> Element.Element Msg
getLineCell line =
    getCellWithBorder lineColumnWidth line


getCellWithBorder : Int -> String -> Element.Element Msg
getCellWithBorder width string =
    Element.row [] [ getCell width string, columnBorder ]


getCell : Int -> String -> Element.Element msg
getCell width string =
    Element.text string
        |> Element.el
            [ Element.Background.color <| Element.rgb255 120 253 120
            , Element.width <| Element.px width
            , Element.clip
            ]


getGridRows : Model -> Element.Element Msg
getGridRows model =
    model.lines
        |> List.map (lineToRowElement model)
        |> Element.column []


getGrid : Model -> Element Msg
getGrid model =
    [ getColumnHeaders model, getGridRows model ]
        |> Element.column [ Element.height Element.fill ]


view : Model -> Html.Html Msg
view model =
    [ queryInput, control, getStatus model, getGrid model ]
        |> Element.column
            [ Element.width Element.fill
            , Element.height Element.fill
            , Element.Background.color (Element.rgb255 250 200 50)
            ]
        |> Element.layout [ Element.Events.onMouseUp MouseUpMsg ]
