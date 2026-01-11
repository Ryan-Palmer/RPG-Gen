open System.Diagnostics
open System

let verbose = true

let timedAction f x m = async {
    let stopwatch = Stopwatch()
    stopwatch.Start()
    let! result = f x
    stopwatch.Stop()
    if verbose then printfn $"{m} generated in {stopwatch.Elapsed.Seconds} seconds\n\n"
    return result
}

async {
    let turnStopwatch = Stopwatch()
    turnStopwatch.Start()

    printfn "RPGGen initialising...\n"

    let! storyThread, initialScene = timedAction DungeonMaster.getInitialScene () "Initial scene"

    printfn $"{initialScene}\n\n"
    
    printfn "Extracting World state...\n"
    do! timedAction World.extractWorldState storyThread "World state"

    printfn "Illustrating...\n"
    
    let! initSceneDescription = timedAction Illustrator.getSceneDescription () "Illustration description"    
    if verbose then printfn $"Illustration description:\n\n{initSceneDescription}\n\n"

    do! timedAction Illustrator.illustrateScene initSceneDescription "Illustration"

    turnStopwatch.Stop()
    if verbose then printfn $"Total init time {turnStopwatch.Elapsed.Seconds} seconds\n\n"

    while true do
        turnStopwatch.Reset()
        turnStopwatch.Start()

        printfn "Enter the players' action:\n"
        let userAction = Console.ReadLine()

        printfn "Applying action...\n"
        let! actionResult = timedAction World.takeAction userAction "Action result"
        if verbose then printfn $"Action result: {actionResult}"

        printfn "\nUpdating World state...\n"
        do! timedAction (World.updateWorldState userAction) actionResult "Update"

        printfn "\n\nGenerating...\n"
        let! dmResponse = timedAction (DungeonMaster.takeTurn storyThread userAction) actionResult "Turn"
        
        printfn $"{dmResponse}\n\n"

        printfn "Extracting World state...\n"
        do! timedAction World.extractWorldState storyThread "World state"

        printfn "Illustrating...\n"

        let! sceneDescription = timedAction Illustrator.getSceneDescription () "Illustration description"
        if verbose then printfn $"Illustration description:\n\n{sceneDescription}\n\n"

        do! timedAction Illustrator.illustrateScene sceneDescription "Illustration"

        turnStopwatch.Stop()
        if verbose then printfn $"Total turn time {turnStopwatch.Elapsed.Seconds} seconds\n\n"
}
|> Async.RunSynchronously

// Compact thread with SummarizingChatReducer