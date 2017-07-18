module QueriesApi
open Queries
open Suave
open Suave.Filters
open Suave.Operators
open JsonFormatter
open EventStore
open CommandHandlers

let readModels getReadModels wp (context : HttpContext) =
  async {
    let! models = getReadModels()
    return! wp models context
  }

let getState eventStore tabId (context : HttpContext) =
  async {
    match System.Guid.TryParse(tabId) with
    | true, tabId ->
      let! state = eventStore.GetState tabId
      return! toStateJson state context
    | _ ->
      let err = {Message = "Invalid Tab Id" }
      return! toErrorJson err context
  }

let queriesApi queries eventStore =
  GET >=>
  choose [
     path "/tables" >=>
      readModels queries.Table.GetTables toTablesJSON
     path "/todos/chef" >=>
      readModels queries.ToDo.GetChefToDos toChefToDosJSON
     path "/todos/waiter" >=>
      readModels queries.ToDo.GetWaiterToDos toWaiterToDosJSON
     path "/todos/cashier" >=>
      readModels queries.ToDo.GetCashierToDos toCashierToDosJSON
     path "/foods" >=>
      readModels queries.Food.GetFoods toFoodsJSON
     path "/drinks" >=>
      readModels queries.Drink.GetDrinks toDrinksJSON
     pathScan "/state/%s" (getState eventStore)
  ]