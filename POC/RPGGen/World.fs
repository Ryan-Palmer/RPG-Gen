module World

open System.Text.Json.Serialization
open Microsoft.Extensions.AI
open System
open System.Text.Json
open System.IO

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


let mutable world = Unchecked.defaultof<World>

let getWorld () =
    world

let getWorldAIFunc = AIFunctionFactory.Create((getWorld : Func<World>), "get_world")

let setWorld newWorld = 
    world <- newWorld

let setWorldAIFunc = AIFunctionFactory.Create((setWorld : Action<World>), "set_world")

let extractWorldState storyThread = async {
    let factAgent = 
        Agent.getResponseAgent """
            You are a fastidious fact-extracting agent.
        """ [| |]
    let threadCopy = Agent.copyThread factAgent storyThread
    let! worldResponse = 
        factAgent.RunAsync<World>("""
            You have been provided with a thread of prose detailing a dungeons and dragons style campaign.
            Your job is to carefully extract all of the hard facts from the prose before returning the canonical world state.
            This state will be used to make all decisions for the next turn.
            Be extremely thorough as anything you miss will be forgotten.
            Include details of every character in the scene, both player and non-player.
            Include all details of the location, including every detail of the scene and all environmental items that can be interacted with in any way.
            Remember that an item can EITHER be in the environment OR in a character's inventory, NEVER both.
            Create and update as many flags as you need to capture only BOOLEAN facts about the scene which might be relevant to the story.
            Give the flags clear descriptions so that their true / false state makes sense.
        """, threadCopy) |> Async.AwaitTask
    setWorld worldResponse.Result
    let path = $"I:\Repos\RPGGen\POC\world.json"
    File.WriteAllText(path, worldResponse.Result |> JsonSerializer.Serialize)
}

let updateWorldState action actionResult = async {
    let factAgent = 
        Agent.getResponseAgent """
            You are a fastidious fact-extracting agent.
            You have a get_world tool to load the canonical world state from the previous turn. Do this first.
            Use your set_world tool to update the world state given the action and consequences.
        """ [| getWorldAIFunc; setWorldAIFunc |]
    let! _ = 
        factAgent.RunAsync($"""
            The user has taken the following action: {action}\n
            This had the following consequences: {actionResult}.
            Get the existing world state using tools, then set the updated state using tools.
            Your job is to carefully establish all of the hard facts.
            This state will be used to make all decisions for the next turn.
            Be extremely thorough as anything you miss will be forgotten.
            Include details of every character in the scene, both player and non-player.
            Include all details of the location, including every detail of the scene and all environmental items that can be interacted with in any way.
            Remember that an item can EITHER be in the environment OR in a character's inventory, NEVER both.
            Create and update as many flags as you need to capture only BOOLEAN facts about the scene which might be relevant to the story.
            Give the flags clear descriptions so that their true / false state makes sense.
        """) |> Async.AwaitTask
    let path = $"I:\Repos\RPGGen\POC\world.json"
    File.WriteAllText(path, world |> JsonSerializer.Serialize)
    return ()
}

let takeAction action = async {
    let factAgent = 
        Agent.getResponseAgent """
            You are rule-applying agent who will take a fantasy world state and an action and describe what the consequences are.
            Do they succeed or fail?
            How does the world change in response to their action?
        """ [| getWorldAIFunc |]

    let! actionResponse = 
        factAgent.RunAsync($"""
            The user takes the following action : {action}
        """) |> Async.AwaitTask

    return actionResponse.Text
}