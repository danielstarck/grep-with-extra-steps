port module Main exposing (..)

import Browser
import Browser.Events
import Dict
import Dict.Extra as Dict
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


type alias MatchingLineDto =
    { filePath : String
    , lineNumber : Int
    , matchingText : String
    }


type alias ResultChunkDto =
    List MatchingLineDto


type alias FileSearchResult =
    { filePath : String
    , matchingLines : List MatchingLine
    }


type alias MatchingLine =
    { lineNumber : Int
    , matchingText : String
    }


type alias Model =
    { directory : String
    , files : String
    , text : String
    , status : Status
    , fileSearchResults : List FileSearchResult
    , resizing : Maybe Resizing
    , fileColumnWidth : Int
    , lineColumnWidth : Int
    }


type alias PartialResizeState =
    { column : ColumnResizeHandle
    , initialSize : Int
    }


type alias ResizeState =
    { column : ColumnResizeHandle
    , initialSize : Int
    , initialMouseX : Int
    }


type Resizing
    = AwaitMouseMove PartialResizeState
    | InProgress ResizeState


init : () -> ( Model, Cmd Msg )
init _ =
    ( { directory = ""
      , files = ""
      , text = ""
      , status = Idle
      , fileSearchResults = []
      , resizing = Nothing
      , fileColumnWidth = 200
      , lineColumnWidth = 50
      }
    , Cmd.none
    )


type ServerMessage
    = ResultChunk ResultChunkDto
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
    | MouseMoveX Int


toFileSearchResults : ResultChunkDto -> List FileSearchResult
toFileSearchResults =
    let
        toMatchingLine : MatchingLineDto -> MatchingLine
        toMatchingLine matchingLineDto =
            { lineNumber = matchingLineDto.lineNumber
            , matchingText = matchingLineDto.matchingText
            }

        toFileSearchResult : ( String, ResultChunkDto ) -> FileSearchResult
        toFileSearchResult ( filePath, chunk ) =
            { filePath = filePath
            , matchingLines = chunk |> List.map toMatchingLine
            }
    in
    Dict.groupBy .filePath
        >> Dict.toList
        >> List.map toFileSearchResult


mergeFileSearchResults : List FileSearchResult -> List FileSearchResult -> List FileSearchResult
mergeFileSearchResults results1 results2 =
    results1
        ++ results2
        |> Dict.groupBy .filePath
        |> Dict.toList
        |> List.map
            (\( filePath, fileSearchResults ) ->
                { filePath = filePath
                , matchingLines = fileSearchResults |> List.concatMap .matchingLines |> List.sortBy .lineNumber
                }
            )
        |> List.sortBy .filePath


update : Msg -> Model -> ( Model, Cmd Msg )
update msg model =
    case msg of
        ServerMessage message ->
            case message of
                ResultChunk chunk ->
                    case model.status of
                        ExecutingQuery ->
                            ( { model | fileSearchResults = chunk |> toFileSearchResults |> mergeFileSearchResults model.fileSearchResults }, Cmd.none )

                        _ ->
                            ( model, Cmd.none )

                QueryFinished ->
                    ( { model | status = Idle }, Cmd.none )

                Unexpected _ ->
                    ( model, Cmd.none )

        StartQuery ->
            let
                payload =
                    Json.Encode.object
                        [ ( "directory", Json.Encode.string model.directory )
                        , ( "files", Json.Encode.string model.files )
                        , ( "text", Json.Encode.string model.text )
                        ]
            in
            ( { model | status = ExecutingQuery, fileSearchResults = [] }, sendTaggedMessage "StartQuery" <| Just payload )

        CancelQuery ->
            case model.status of
                ExecutingQuery ->
                    ( { model | status = Idle }, sendTaggedMessage "CancelQuery" Nothing )

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
                    Just <|
                        AwaitMouseMove
                            { column = column
                            , initialSize = initialSize
                            }
              }
            , Cmd.none
            )

        GlobalMouseUp ->
            ( { model | resizing = Nothing }, Cmd.none )

        MouseMoveX currentMouseX ->
            case model.resizing of
                Just (AwaitMouseMove partialResizeState) ->
                    -- TODO: somehow disable mouse y movement when resizing
                    let
                        resizing =
                            InProgress { column = partialResizeState.column, initialSize = partialResizeState.initialSize, initialMouseX = currentMouseX }
                    in
                    ( { model | resizing = Just resizing }, Cmd.none )

                Just (InProgress resizeState) ->
                    let
                        deltaMouseX =
                            currentMouseX - resizeState.initialMouseX

                        newWidth =
                            max 10 (resizeState.initialSize + deltaMouseX)
                    in
                    case resizeState.column of
                        File ->
                            ( { model | fileColumnWidth = newWidth }, Cmd.none )

                        Line ->
                            ( { model | lineColumnWidth = newWidth }, Cmd.none )

                Nothing ->
                    -- TODO: can never happen. model things differently?
                    ( model, Cmd.none )

        DirectoryChanged directory ->
            ( { model | directory = directory }, Cmd.none )

        FilesChanged files ->
            ( { model | files = files }, Cmd.none )

        TextChanged text ->
            ( { model | text = text }, Cmd.none )


port sendMessage : Json.Encode.Value -> Cmd msg


port receiveMessage : (Json.Encode.Value -> msg) -> Sub msg


sendTaggedMessage : String -> Maybe Json.Encode.Value -> Cmd msg
sendTaggedMessage tag maybePayload =
    let
        fields =
            ( "tag", Json.Encode.string tag )
                :: (case maybePayload of
                        Just payload ->
                            [ ( "payload", payload ) ]

                        Nothing ->
                            []
                   )

        json =
            Json.Encode.object fields
    in
    sendMessage json


decodeServerMessage : Json.Encode.Value -> ServerMessage
decodeServerMessage json =
    let
        matchingLineDtoDecoder =
            Json.Decode.map3
                MatchingLineDto
                (Json.Decode.field "filePath" Json.Decode.string)
                (Json.Decode.field "lineNumber" Json.Decode.int)
                (Json.Decode.field "matchingText" Json.Decode.string)

        resultChunkDecoder : Json.Decode.Decoder ServerMessage
        resultChunkDecoder =
            Json.Decode.list matchingLineDtoDecoder
                |> Json.Decode.field "chunk"
                |> Json.Decode.andThen (ResultChunk >> Json.Decode.succeed)

        serverMessageDecoder : Json.Decode.Decoder ServerMessage
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
                            Json.Decode.map MouseMoveX
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
    Element.el [ Element.Background.color <| Element.rgb255 50 197 250, Element.padding 2 ] <| Element.text <| statusText


getFileColumnHeader : Model -> Element Msg
getFileColumnHeader model =
    getColumnHeader (Just ( ColumnResizeHandleMouseDown File, model.fileColumnWidth )) "File"


getLineColumnHeader : Model -> Element Msg
getLineColumnHeader model =
    getColumnHeader (Just ( ColumnResizeHandleMouseDown Line, model.lineColumnWidth )) "Line"


textColumnHeader : Element Msg
textColumnHeader =
    getColumnHeader Nothing "Text"



-- TODO: refactor


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



-- TODO: refactor. [columnHeader, columnDivider, columnHeader] instead of [columnWithDivider, column]


getColumnHeaders : Model -> Element Msg
getColumnHeaders model =
    [ getFileColumnHeader model, getLineColumnHeader model, textColumnHeader ]
        |> Element.row [ Element.width Element.fill ]


fileSearchResultToRowElements : Model -> FileSearchResult -> Element Msg
fileSearchResultToRowElements model result =
    getFileHeaderRow model result
        :: getMatchingLineRows model result
        |> Element.column [ Element.width Element.fill ]


getFileHeaderRow : Model -> FileSearchResult -> Element Msg
getFileHeaderRow model result =
    let
        matchingLineCountText =
            (result.matchingLines
                |> List.length
                |> String.fromInt
            )
                ++ " matches in [TODO: result.linesSearched] lines."
    in
    [ getFileCell model.fileColumnWidth result.filePath, getLineCell model.lineColumnWidth Nothing, getTextCell matchingLineCountText ]
        |> Element.row [ Element.width Element.fill, Element.Background.color <| Element.rgb255 86 172 76 ]


getMatchingLineRows : Model -> FileSearchResult -> List (Element Msg)
getMatchingLineRows model result =
    let
        toMatchingLineRow : MatchingLine -> Element Msg
        toMatchingLineRow matchingLine =
            [ getFileCell model.fileColumnWidth result.filePath, getLineCell model.lineColumnWidth <| Just matchingLine.lineNumber, getTextCell matchingLine.matchingText ]
                |> Element.row [ Element.width Element.fill, Element.Background.color <| Element.rgb255 95 200 84 ]
    in
    result.matchingLines
        |> List.map toMatchingLineRow


getFileCell : Int -> String -> Element Msg
getFileCell width filePath =
    getCellWithBorder width filePath


getLineCell : Int -> Maybe Int -> Element Msg
getLineCell width lineNumber =
    lineNumber
        |> Maybe.map String.fromInt
        |> Maybe.withDefault "-"
        |> getCellWithBorder width


getTextCell : String -> Element Msg
getTextCell text =
    getCell Nothing text


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
            [ Element.width width
            , Element.clip
            ]


getGridRows : Model -> Element Msg
getGridRows model =
    model.fileSearchResults
        |> List.map (fileSearchResultToRowElements model)
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
