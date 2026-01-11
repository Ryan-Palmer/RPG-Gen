module Types

open System.Text.Json.Serialization

[<JsonConverter(typeof<JsonStringEnumConverter>)>]
type CharacterType =
    | PC = 0
    | NPC = 1

type Item (
    Name : string,
    Description : string)=
    [<JsonRequired>]member val Name = Name with get, set
    [<JsonRequired>]member val Description = Description with get, set

type Character (
    CharacterType : CharacterType,
    Name : string,
    Description : string,
    Status : string,
    Inventory: Item list) =
    [<JsonRequired>]member val CharacterType = CharacterType with get, set
    [<JsonRequired>]member val Name = Name with get, set
    [<JsonRequired>]member val Description = Description with get, set
    [<JsonRequired>]member val Status = Status with get, set
    [<JsonRequired>]member val Inventory = Inventory with get, set


type Location (
    Name : string,
    Items : Item list,
    Description : string) =
    [<JsonRequired>]member val Name = Name with get, set
    [<JsonRequired>]member val Items = Items with get, set
    [<JsonRequired>]member val Description = Description with get, set

type Flag (
    Description : string,
    Status : bool) =
    [<JsonRequired>]member val Description = Description with get, set
    [<JsonRequired>]member val Status = Status with get, set

type World (
    CurrentLocation : Location,
    Characters : Character list,
    Flags : Flag list) =
    [<JsonRequired>]member val CurrentLocation = CurrentLocation with get, set
    [<JsonRequired>]member val Characters = Characters with get, set
    [<JsonRequired>]member val Flags = Flags with get, set