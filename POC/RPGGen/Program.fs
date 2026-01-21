open System.Diagnostics
open System
open System.Text.Json

let verbose = true

let timedAction f x m = async {
    let stopwatch = Stopwatch()
    stopwatch.Start()
    let! result = f x
    stopwatch.Stop()
    if verbose then printfn $"{m} generated in {stopwatch.Elapsed.Seconds} seconds\n"
    return result
}

let printWorldState () =
    let options = JsonSerializerOptions()
    options.WriteIndented <- true
    let worldJson = JsonSerializer.Serialize(World.world, options)
    printfn $"\n=== WORLD STATE ===\n{worldJson}\n===================\n"

let getPlayerCharacters () =
    printfn "Welcome to RPGGen\n"
    printfn "Let's design your characters!\n"
    
    let rec getCharacterList acc =
        printfn "Enter character name (or press Enter to finish):"
        let name = Console.ReadLine()
        if String.IsNullOrWhiteSpace(name) then
            acc
        else
            printfn $"\nEnter description for {name}:"
            let description = Console.ReadLine()
            let character = World.Character(World.CharacterType.PC, name, description, "Healthy", [])
            printfn $"\nAdded {name} to the party!\n"
            getCharacterList (character :: acc)
    
    let characters = getCharacterList []
    if List.isEmpty characters then
        printfn "No characters created. Creating a default character...\n"
        [World.Character(World.CharacterType.PC, "Adventurer", "A brave hero ready for adventure", "Healthy", [])]
    else
        List.rev characters

async {
    let turnStopwatch = Stopwatch()
    turnStopwatch.Start()

    let playerCharacters = getPlayerCharacters ()
    
    printfn "Initialising scene...\n"
    let! storyThread, initialScene = timedAction DungeonMaster.getInitialScene playerCharacters "Initial scene"
    printfn $"{initialScene}\n"
    
    printfn "Initialising World state...\n"    
    do! timedAction World.initWorldState storyThread "World state"
    if verbose then printWorldState ()

    printfn "Illustrating...\n"
    let! initSceneDescription = timedAction Illustrator.getSceneDescription () "Illustration description"    
    if verbose then printfn $"Illustration description:\n{initSceneDescription}\n"
    do! timedAction Illustrator.illustrateScene initSceneDescription "Illustration"

    turnStopwatch.Stop()
    if verbose then printfn $"Total init time {turnStopwatch.Elapsed.Seconds} seconds\n"

    while true do
        turnStopwatch.Reset()
        turnStopwatch.Start()

        printfn "Enter the players' action:\n"
        let userAction = Console.ReadLine()

        printfn "\nGetting action result...\n"
        let! actionResult = timedAction World.takeAction userAction "Action result"
        if verbose then printfn $"Action result: {actionResult}\n"

        printfn "Updating World state...\n"
        do! timedAction (World.applyAction userAction) actionResult "World update"
        if verbose then printWorldState ()

        printfn "Generating narrative...\n"
        let! dmResponse = timedAction (DungeonMaster.takeTurn storyThread userAction) actionResult "Turn"
        printfn $"\n=== DM RESPONSE ===\n{dmResponse}\n===================\n"

        printfn "Evolving World state from narrative...\n"
        do! timedAction World.applyNarrative dmResponse "World evolution"
        if verbose then printWorldState ()

        printfn "Illustrating...\n"
        let! sceneDescription = timedAction Illustrator.getSceneDescription () "Illustration description"
        if verbose then printfn $"Illustration description:\n\n{sceneDescription}\n"
        do! timedAction Illustrator.illustrateScene sceneDescription "Illustration"

        turnStopwatch.Stop()
        if verbose then printfn $"Total turn time {turnStopwatch.Elapsed.Seconds} seconds\n"
}
|> Async.RunSynchronously

// Compact thread with SummarizingChatReducer