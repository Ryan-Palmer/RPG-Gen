open System.Diagnostics
open System
open Types
open System.Text.Json
open System.IO

let verbose = true

let timedAction f x m = async {
    let stopwatch = Stopwatch()
    stopwatch.Start()
    let! result = f x
    stopwatch.Stop()
    if verbose then printfn $"{m} generated in {stopwatch.Elapsed.Seconds} seconds\n\n"
    return result
}

let extractWorldState storyThread = async {
    let factAgent = Agent.getResponseAgent ""
    let threadCopy = Agent.copyThread factAgent storyThread
    let! worldResponse = 
            factAgent.RunAsync<World>("""
                You have been provided with a thread of prose detailing a dungeons and dragons style campaign. 
                Your job is to carefully extract all of the hard facts from the prose, creating the canonical world state that will be used to make all decisions.
                Be extremely thorough as anything you miss will be forgotten.
                Include details of every character in the scene, both player and non-player.
                Include all details of the location, including every detail of the scene and all environmental items that can be interacted with in any way.
                Remember that an item can EITHER be in the environment OR in a character's inventory, NEVER both.
                Use as many flags as you need to capture booean facts about the scene which might be relevant to the story. Give the flags clear descriptions so that they make sense.
            """, threadCopy) |> Async.AwaitTask
    let path = $"I:\Repos\RPGGen\POC\world.json"
    File.WriteAllText(path, worldResponse.Result |> JsonSerializer.Serialize)
}

async {
    let turnStopwatch = Stopwatch()
    turnStopwatch.Start()

    printfn "RPGGen initialising...\n"

    let! storyThread, initialScene = timedAction DungeonMaster.getInitialScene () "Initial scene"

    printfn $"{initialScene}\n\n"
    
    printfn "Extracting World state...\n"
    do! timedAction extractWorldState storyThread "World state"

    printfn "Illustrating...\n"
    
    let! initSceneDescription = timedAction Illustrator.getSceneDescription storyThread "Illustration description"    
    if verbose then printfn $"Illustration description:\n\n{initSceneDescription}\n\n"

    do! timedAction Illustrator.illustrateScene initSceneDescription "Illustration"

    turnStopwatch.Stop()
    if verbose then printfn $"Total init time {turnStopwatch.Elapsed.Seconds} seconds\n\n"

    while true do
        turnStopwatch.Reset()
        turnStopwatch.Start()

        printfn "Enter the players' action:\n"
        let userAction = Console.ReadLine()

        printfn "\n\nGenerating...\n"
        let! dmResponse = timedAction (DungeonMaster.takeTurn storyThread) userAction "Turn"
        
        printfn $"{dmResponse}\n\n"
        
        printfn "Extracting World state...\n"
        do! timedAction extractWorldState storyThread "World state"

        printfn "Illustrating...\n"

        let! sceneDescription = timedAction Illustrator.getSceneDescription storyThread "Illustration description"
        if verbose then printfn $"Illustration description:\n\n{sceneDescription}\n\n"

        do! timedAction Illustrator.illustrateScene sceneDescription "Illustration generated"

        turnStopwatch.Stop()
        if verbose then printfn $"Total turn time {turnStopwatch.Elapsed.Seconds} seconds\n\n"
}
|> Async.RunSynchronously