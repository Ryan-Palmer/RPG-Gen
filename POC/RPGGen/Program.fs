open System.Diagnostics
open System

let turnStopwatch = Stopwatch()
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
    turnStopwatch.Start()

    printfn "RPGGen initialising...\n"

    let! storyThread, initialScene = timedAction DungeonMaster.getInitialScene () "Initial scene"

    printfn $"{initialScene}\n\nIllustrating...\n"
    
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
        
        printfn $"{dmResponse}\n\n\nIllustrating...\n"

        let! sceneDescription = timedAction Illustrator.getSceneDescription storyThread "Illustration description"
        if verbose then printfn $"Illustration description:\n\n{sceneDescription}\n\n"

        do! timedAction Illustrator.illustrateScene sceneDescription "Illustration generated"

        turnStopwatch.Stop()
        if verbose then printfn $"Total turn time {turnStopwatch.Elapsed.Seconds} seconds\n\n"
}
|> Async.RunSynchronously