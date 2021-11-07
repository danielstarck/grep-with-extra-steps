port module Main exposing (..)

import Browser
import Browser.Events
import Element exposing (Element)
import Element.Background
import Element.Border
import Element.Events
import Element.Input
import Html exposing (Html)
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


type alias ResultChunk =
    { filePath : String
    , lineNumber : Int
    , matchingText : String
    }


type alias Model =
    { status : Status
    , resultChunks : List ResultChunk
    , resizing : Maybe ResizeOperation
    , fileColumnWidth : Int
    , lineColumnWidth : Int
    }


type alias ResizeOperation =
    { column : ColumnResizeHandle
    , initialSize : Int
    , initialMouseX : Maybe Int
    }


init : () -> ( Model, Cmd Msg )
init _ =
    ( { status = Idle
      , resultChunks = []
      , resizing = Nothing
      , fileColumnWidth = 200
      , lineColumnWidth = 50
      }
    , Cmd.none
    )


type ServerMessage
    = ResultChunks (List ResultChunk)
    | QueryFinished
    | Unexpected Json.Encode.Value


type ColumnResizeHandle
    = File
    | Line


type Msg
    = ServerMessage ServerMessage
    | StartQuery
    | CancelQuery
    | DirectoryChanged String
    | FilesChanged String
    | TextChanged String
    | ColumnResizeHandleMouseDown ColumnResizeHandle
    | GlobalMouseUp
    | MouseMoveMsg Int


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        ServerMessage message ->
            case message of
                ResultChunks chunks ->
                    case model.status of
                        ExecutingQuery ->
                            ( { model | resultChunks = model.resultChunks ++ chunks }, Cmd.none )

                        _ ->
                            ( model, Cmd.none )

                QueryFinished ->
                    ( { model | status = Idle }, Cmd.none )

                Unexpected _ ->
                    ( model, Cmd.none )

        StartQuery ->
            ( { model | status = ExecutingQuery, resultChunks = [] }, sendMessage "StartQuery" )

        CancelQuery ->
            case model.status of
                ExecutingQuery ->
                    ( { model | status = Idle }, sendMessage "CancelQuery" )

                _ ->
                    ( model, Cmd.none )

        ColumnResizeHandleMouseDown column ->
            ( { model
                | resizing =
                    let
                        initialSize =
                            case column of
                                File ->
                                    model.fileColumnWidth

                                Line ->
                                    model.lineColumnWidth
                    in
                    Just
                        { column = column
                        , initialSize = initialSize
                        , initialMouseX = Nothing
                        }
              }
            , Cmd.none
            )

        GlobalMouseUp ->
            ( { model | resizing = Nothing }, Cmd.none )

        MouseMoveMsg x ->
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
                            case resizing.column of
                                File ->
                                    ( { model | fileColumnWidth = newSize }, Cmd.none )

                                Line ->
                                    ( { model | lineColumnWidth = newSize }, Cmd.none )

                        Nothing ->
                            ( { model | resizing = Just { resizing | initialMouseX = Just x } }, Cmd.none )

                Nothing ->
                    -- TODO: can never happen. model things differently?
                    ( model, Cmd.none )

        _ ->
            ( model, Cmd.none )


port sendMessage : String -> Cmd msg


port receiveMessage : (Json.Encode.Value -> msg) -> Sub msg


decodeServerMessage : Json.Encode.Value -> ServerMessage
decodeServerMessage json =
    let
        chunkDecoder =
            Json.Decode.map3
                ResultChunk
                (Json.Decode.field "filePath" Json.Decode.string)
                (Json.Decode.field "lineNumber" Json.Decode.int)
                (Json.Decode.field "matchingText" Json.Decode.string)

        resultChunksDecoder : Json.Decode.Decoder ServerMessage
        resultChunksDecoder =
            Json.Decode.list chunkDecoder
                |> Json.Decode.field "chunks"
                |> Json.Decode.andThen (ResultChunks >> Json.Decode.succeed)

        serverMessageDecoder : Json.Decode.Decoder ServerMessage
        serverMessageDecoder =
            Json.Decode.string
                |> Json.Decode.field "tag"
                |> Json.Decode.andThen
                    (\tag ->
                        case tag of
                            "ResultChunks" ->
                                resultChunksDecoder

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
                            Json.Decode.map MouseMoveMsg
                                (Json.Decode.field "pageX" Json.Decode.int)
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


queryInput : Element Msg
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


getStatus : Model -> Element msg
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
    getColumnHeader (Just ( ColumnResizeHandleMouseDown File, model.fileColumnWidth )) "File"


getLineColumnHeader : Model -> Element Msg
getLineColumnHeader model =
    getColumnHeader (Just ( ColumnResizeHandleMouseDown Line, model.lineColumnWidth )) "Line"


textColumnHeader : Element Msg
textColumnHeader =
    getColumnHeader Nothing "Text"


getColumnHeader : Maybe ( Msg, Int ) -> String -> Element Msg
getColumnHeader maybeMouseDownMsgAndSize title =
    let
        baseAttributes =
            [ Element.Background.color <| Element.rgb255 120 120 253
            , Element.clip
            ]
    in
    case maybeMouseDownMsgAndSize of
        Just ( msg, size ) ->
            Element.row []
                [ Element.text title
                    |> Element.el ((Element.width <| Element.px size) :: baseAttributes)
                , getColumnBorderResizeHandle msg
                ]

        Nothing ->
            Element.text title
                |> Element.el (Element.width Element.fill :: baseAttributes)


getColumnBorderResizeHandle : Msg -> Element Msg
getColumnBorderResizeHandle mouseDownMsg =
    getColumnBorder <| Just mouseDownMsg


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
    Element.el
        attributes
        Element.none


columnBorder : Element Msg
columnBorder =
    getColumnBorder Nothing


getColumnHeaders : Model -> Element Msg
getColumnHeaders model =
    [ getFileColumnHeader model, getLineColumnHeader model, textColumnHeader ]
        |> Element.row [ Element.width Element.fill ]


resultChunkToRowElement : Model -> ResultChunk -> Element Msg
resultChunkToRowElement model chunk =
    [ chunk |> getFileCell model, chunk |> getLineCell model, chunk |> getTextCell ]
        |> Element.row [ Element.width Element.fill ]


getFileCell : Model -> ResultChunk -> Element Msg
getFileCell model chunk =
    getCellWithBorder model.fileColumnWidth chunk.filePath


getLineCell : Model -> ResultChunk -> Element Msg
getLineCell model chunk =
    getCellWithBorder model.lineColumnWidth <| String.fromInt chunk.lineNumber


getTextCell : ResultChunk -> Element Msg
getTextCell chunk =
    getCell Nothing chunk.matchingText


getCellWithBorder : Int -> String -> Element Msg
getCellWithBorder width string =
    Element.row [] [ getCell (Just width) string, columnBorder ]


getCell : Maybe Int -> String -> Element msg
getCell maybeWidthPx string =
    let
        width =
            case maybeWidthPx of
                Just px ->
                    Element.px px

                Nothing ->
                    Element.fill
    in
    Element.text string
        |> Element.el
            [ Element.Background.color <| Element.rgb255 120 253 120
            , Element.width width
            , Element.clip
            ]


getGridRows : Model -> Element Msg
getGridRows model =
    model.resultChunks
        |> List.map (resultChunkToRowElement model)
        |> Element.column [ Element.width Element.fill ]


getGrid : Model -> Element Msg
getGrid model =
    [ getColumnHeaders model, getGridRows model ]
        |> Element.column [ Element.width Element.fill, Element.height Element.fill ]


view : Model -> Html Msg
view model =
    [ queryInput, control, getStatus model, getGrid model ]
        |> Element.column
            [ Element.width Element.fill
            , Element.height Element.fill
            , Element.Background.color (Element.rgb255 250 200 50)
            ]
        |> Element.layout [ Element.Events.onMouseUp GlobalMouseUp ]
