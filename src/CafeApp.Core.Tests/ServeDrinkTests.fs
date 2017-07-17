module ServeDrinkTests
open Domain
open States
open Commands
open Events
open CafeAppTestsDSL
open NUnit.Framework
open TestData
open Errors

[<Test>]
let ``Can Serve Drink`` () =
  let order = {order with Drinks = [coke;lemonade]}
  let expected = {
      PlacedOrder = order
      ServedDrinks = [coke]
      PreparedFoods = []
      ServedFoods = []}
  Given (PlacedOrder order)
  |> When (ServeDrink (coke, order.Tab.Id))
  |> ThenStateShouldBe (OrderInProgress expected)
  |> WithEvents [DrinkServed (coke, order.Tab.Id)]

[<Test>]
let ``Can not serve non ordered drink`` () =
  let order = {order with Drinks = [coke]}
  Given (PlacedOrder order)
  |> When (ServeDrink (lemonade, order.Tab.Id))
  |> ShouldFailWith (CanNotServeNonOrderedDrink lemonade)

[<Test>]
let ``Can not serve drink for already served order`` () =
  Given (ServedOrder order)
  |> When (ServeDrink (coke, order.Tab.Id))
  |> ShouldFailWith OrderAlreadyServed

[<Test>]
let ``Can not serve drink for non placed order`` () =
  Given (OpenedTab tab)
  |> When (ServeDrink (coke, tab.Id))
  |> ShouldFailWith CanNotServeForNonPlacedOrder

[<Test>]
let ``Can not serve with closed tab`` () =
  Given (ClosedTab None)
  |> When (ServeDrink (coke, tab.Id))
  |> ShouldFailWith (CanNotServeWithClosedTab)

[<Test>]
let ``Can not serve an already served drink`` () =
  let order = {order with Drinks = [coke;lemonade]}
  let ipo = {
      PlacedOrder = order
      ServedDrinks = [coke]
      PreparedFoods = []
      ServedFoods = []}
  Given (OrderInProgress ipo)
  |> When (ServeDrink (coke, order.Tab.Id))
  |> ShouldFailWith (CanNotServeAlreadyServedDrink coke)

[<Test>]
let ``Can complete the order by serving drink`` () =
  let order = {order with Drinks = [coke;lemonade]}
  let orderInProgress = {
    PlacedOrder = order
    ServedDrinks = [coke]
    PreparedFoods = []
    ServedFoods = []
  }
  let amount = drinkPrice coke + drinkPrice lemonade
  let payment = { Tab = tab; Amount = amount}
  Given (OrderInProgress orderInProgress)
  |> When (ServeDrink (lemonade, order.Tab.Id))
  |> ThenStateShouldBe (ServedOrder order)
  |> WithEvents [
      DrinkServed (lemonade, order.Tab.Id)
      OrderServed (order, payment)
    ]

[<Test>]
let ``Remain in order in progress while serving drink`` () =
  let order = {order with Drinks = [coke;lemonade;appleJuice]}
  let orderInProgress = {
    PlacedOrder = order
    ServedDrinks = [coke]
    PreparedFoods = []
    ServedFoods = []
  }
  let expected =
    {orderInProgress with
        ServedDrinks = lemonade :: orderInProgress.ServedDrinks}

  Given (OrderInProgress orderInProgress)
  |> When (ServeDrink (lemonade, order.Tab.Id))
  |> ThenStateShouldBe (OrderInProgress expected)
  |> WithEvents [DrinkServed (lemonade, order.Tab.Id)]

[<Test>]
let ``Can serve drinks for order containing only one drinks`` () =
  let order = {order with Drinks = [coke]}
  let payment = {Tab = order.Tab; Amount = drinkPrice coke}

  Given (PlacedOrder order)
  |> When (ServeDrink (coke, order.Tab.Id))
  |> ThenStateShouldBe (ServedOrder order)
  |> WithEvents [
      DrinkServed (coke, order.Tab.Id)
      OrderServed (order, payment)
    ]

[<Test>]
let ``Can not close a placed order with food to serve`` () =
  let order = {order with Drinks = [coke]; Foods = [salad] }
  let expectedOrderInProgress = {
    PlacedOrder = order
    ServedDrinks = [coke]
    PreparedFoods = []
    ServedFoods = []
  }
  Given (PlacedOrder order)
  |> When (ServeDrink (coke, order.Tab.Id))
  |> ThenStateShouldBe (OrderInProgress expectedOrderInProgress)
  |> WithEvents [DrinkServed (coke, order.Tab.Id)]

[<Test>]
let ``Can not close a in progress order with food to prepare`` () =
  let order = {order with Drinks = [coke;lemonade]; Foods = [salad] }
  let orderInProgress = {
    PlacedOrder = order
    ServedDrinks = [coke]
    PreparedFoods = []
    ServedFoods = []
  }
  let expected =
    {orderInProgress with
        ServedDrinks = lemonade :: orderInProgress.ServedDrinks}

  Given (OrderInProgress orderInProgress)
  |> When (ServeDrink (lemonade, order.Tab.Id))
  |> ThenStateShouldBe (OrderInProgress expected)
  |> WithEvents [DrinkServed (lemonade, order.Tab.Id)]

[<Test>]
let ``Can not serve non ordered drinks during order in progress `` () =
  let order = {order with Drinks = [coke;lemonade]}
  let orderInProgress = {
    PlacedOrder = order
    ServedDrinks = [coke]
    PreparedFoods = []
    ServedFoods = []
  }

  Given (OrderInProgress orderInProgress)
  |> When (ServeDrink (appleJuice,order.Tab.Id))
  |> ShouldFailWith (CanNotServeNonOrderedDrink appleJuice)

[<Test>]
let ``Can not serve an already served drinks`` () =
  let order = {order with Drinks = [coke;lemonade]}
  let orderInProgress = {
    PlacedOrder = order
    ServedDrinks = [coke]
    PreparedFoods = []
    ServedFoods = []
  }

  Given (OrderInProgress orderInProgress)
  |> When (ServeDrink (coke,order.Tab.Id))
  |> ShouldFailWith (CanNotServeAlreadyServedDrink coke)
