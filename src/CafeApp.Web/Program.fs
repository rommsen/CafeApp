module Program

open Suave
open Suave.Web
open Suave.Successful
open Suave.RequestErrors
open Suave.Operators
open Suave.Filters
open CommandApi
open InMemory
open System.Text
open Chessie.ErrorHandling
open Events
open Projections
open JsonFormatter
open QueriesApi
open Suave.WebSocket
open Suave.Sockets
open Suave.Sockets.Control
open Suave.Sockets.SocketOp
open System.Reflection
open System.IO

let eventsStream = new Control.Event<Event list>()
let project event =
  projectReadModel inMemoryActions event
  |> Async.RunSynchronously |> ignore
let projectEvents events =
  events
  |> List.iter project

let socketHandler (ws : WebSocket) cx = socket {
  while true do
    let! events =
      Control.Async.AwaitEvent(eventsStream.Publish)
      |> Suave.Sockets.SocketOp.ofAsync
    for event in events do
      let eventData =
        event
        |> eventJObj
        |> string
        |> System.Text.Encoding.UTF8.GetBytes
        |> ByteSegment
      do! ws.send Text eventData true
}

let commandApiHandler eventStore (context : HttpContext) = async {
  let payload =
    Encoding.UTF8.GetString context.request.rawForm
  let! response =
    handleCommandRequest
      inMemoryQueries eventStore payload
  match response with
  | Ok ((state,events), _) ->
    do! eventStore.SaveEvents state events
    eventsStream.Trigger(events)
    return! toStateJson state context
  | Bad (err) ->
    return! toErrorJson err.Head context
}

let commandApi eventStore =
  path "/command"
    >=> POST
    >=> commandApiHandler eventStore

let clientDir =
  let exePath = Assembly.GetEntryAssembly().Location
  let exeDir = (new FileInfo(exePath)).Directory
  Path.Combine(exeDir.FullName, "public")

[<EntryPoint>]
let main argv =
  let app =
    let eventStore = inMemoryEventStore ()
    choose [
      path "/websocket" >=> handShake socketHandler
      commandApi eventStore
      queriesApi inMemoryQueries eventStore
      GET >=> choose [
        path "/" >=> Files.browseFileHome "index.html"
        Files.browseHome ]
    ]

  eventsStream.Publish.Add(projectEvents)

  let cfg = {defaultConfig with
              homeFolder = Some(clientDir)
              bindings = [HttpBinding.createSimple HTTP "0.0.0.0" 8083]
            }
  startWebServer cfg app
  0
