module CommandHandlers

open States
open Events
open System
open Domain
open Commands

let execute state command =
  match command with
  | OpenTab tab -> [TabOpened tab]

  | _ -> failwith "Todo"

let evolve state command =
  let events = execute state command
  let newState = List.fold apply state events
  (newState, events)