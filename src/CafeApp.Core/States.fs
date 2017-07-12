module States

open Domain
open Events
open System

type State =
  | ClosedTab of Guid option
  | OpenedTab of Tab
  | PlacedOrder of Order
  | OrderInProgress of InProgressOrder
  | ServedOrder of Order


let apply state event =
  match state,event with
  | ClosedTab _, TabOpened tab ->
      OpenedTab tab

  | _ -> state