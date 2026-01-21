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

type Quest (
    Name : string,
    Description : string,
    IsActive : bool) =
    [<JsonRequired>]member val Name = Name with get, set
    [<JsonRequired>]member val Description = Description with get, set
    [<JsonRequired>]member val IsActive = IsActive with get, set

type World (
    CurrentLocation : Location,
    Characters : Character list,
    Flags : Flag list,
    ActiveQuests : Quest list,
    RecentEvents : string list,
    SceneNarrative : string,
    TimeOfDay : string,
    Weather : string) =
    [<JsonRequired>]member val CurrentLocation = CurrentLocation with get, set
    [<JsonRequired>]member val Characters = Characters with get, set
    [<JsonRequired>]member val Flags = Flags with get, set
    [<JsonRequired>]member val ActiveQuests = ActiveQuests with get, set
    [<JsonRequired>]member val RecentEvents = RecentEvents with get, set
    [<JsonRequired>]member val SceneNarrative = SceneNarrative with get, set
    [<JsonRequired>]member val TimeOfDay = TimeOfDay with get, set
    [<JsonRequired>]member val Weather = Weather with get, set


let mutable world = Unchecked.defaultof<World>

let getWorld () =
    printfn " *** Loading world state ***\n"
    world

let getWorldAIFunc = AIFunctionFactory.Create((getWorld : Func<World>), "get_world", "Loads the current canonical world state. This is the source of truth for all facts about the game world. ALWAYS call this first before making any decisions.")

let setWorld newWorld =
    printfn " *** Saving world state ***\n"
    world <- newWorld

let setWorldAIFunc = AIFunctionFactory.Create((setWorld : Action<World>), "set_world", "Saves the updated world state with all canonical facts.")

let extractWorldState storyThread = async {
    let factAgent = 
        Agent.getResponseAgent """
            You are a fact-extracting agent. Your job is simple:
            1. Read the story thread carefully
            2. Extract ALL important facts
            3. Return a complete World object
            
            CRITICAL RULES:
            - An item is EITHER in the environment OR in a character's inventory, NEVER both
            - Include EVERY character mentioned (player and non-player)
            - Include EVERY interactable item or environmental detail
            - Update flags for important story states (doors locked/unlocked, NPCs friendly/hostile, etc.)
            - Keep RecentEvents to the last 3-5 significant events only
            - Update SceneNarrative with a brief (2-3 sentences) summary of the current situation
            - Set TimeOfDay and Weather based on story context
            
            Be thorough. Anything you miss is forgotten forever.
        """ [| |]
    let threadCopy = Agent.copyThread factAgent storyThread
    let! worldResponse = 
        factAgent.RunAsync<World>("""
            Extract the complete world state from this story thread.
            Include all facts about: location, characters, items, quests, flags, recent events, scene narrative, time, and weather.
        """, threadCopy) |> Async.AwaitTask
    setWorld worldResponse.Result
    let path = $"I:\Repos\RPGGen\POC\world.json"
    File.WriteAllText(path, worldResponse.Result |> JsonSerializer.Serialize)
}

let updateWorldState action actionResult = async {
    let factAgent = 
        Agent.getResponseAgent """
            You are a fact-extracting agent.
            
            STEP 1: Call get_world to load the current state
            STEP 2: Apply the action and result to update the state
            STEP 3: Call set_world with the updated state
            
            CRITICAL RULES:
            - An item is EITHER in the environment OR in a character's inventory, NEVER both
            - Include EVERY character mentioned (player and non-player)
            - Include EVERY interactable item or environmental detail
            - Update flags for important story states
            - Add the action result to RecentEvents (keep last 3-5 only)
            - Update SceneNarrative with current situation (2-3 sentences)
            - Update time/weather if they changed
        """ [| getWorldAIFunc; setWorldAIFunc |]
    return! 
        factAgent.RunAsync($"""
            STEP 1: Use get_world tool now
            STEP 2: Player action: {action}
            STEP 3: Result: {actionResult}
            STEP 4: Use set_world tool with updated state
            
            Update all relevant fields. Be thorough - missing facts are lost forever.
        """)
        |> Async.AwaitTask
        |> Async.Ignore
}

let takeAction action = async {
    let rulesAgent = 
        Agent.getResponseAgent """
            You are a rules agent for a fantasy RPG.
            
            STEP 1: Call get_world to see the current state
            STEP 2: Decide if the action succeeds, fails, or partially succeeds
            STEP 3: Describe the consequences clearly
            
            Consider: character abilities, environmental factors, item availability, and current flags.
            Be fair but challenging. Not all actions succeed.
        """ [| getWorldAIFunc |]

    let! actionResponse = 
        rulesAgent.RunAsync($"""
            STEP 1: Use get_world tool now to load current state
            STEP 2: Evaluate this action: {action}
            STEP 3: Describe what happens (success/failure and consequences)
        """) |> Async.AwaitTask

    return actionResponse.Text
}