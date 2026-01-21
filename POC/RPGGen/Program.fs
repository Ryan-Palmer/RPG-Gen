open System.Diagnostics
open System
open System.Text.Json

let verbose = true

let timedAction f x m = async {
    let stopwatch = Stopwatch()
    stopwatch.Start()
    let! result = f x
    stopwatch.Stop()
    if verbose then ConsoleOutput.printVerboseInfo $"{m} generated in {stopwatch.Elapsed.Seconds} seconds\n"
    return result
}

let printWorldState () =
    let options = JsonSerializerOptions()
    options.WriteIndented <- true
    let worldJson = JsonSerializer.Serialize(World.world, options)
    ConsoleOutput.printWorldState $"\n=== WORLD STATE ===\n{worldJson}\n===================\n"

let getPlayerCharacters () =
    ConsoleOutput.printUserPrompt "Welcome to RPGGen\n"
    ConsoleOutput.printUserPrompt "Let's design your characters!\n"
    
    let rec getCharacterList acc =
        ConsoleOutput.printUserPrompt "Enter character name (or press Enter to finish):"
        let name = Console.ReadLine()
        if String.IsNullOrWhiteSpace(name) then
            acc
        else
            ConsoleOutput.printUserPrompt $"\nEnter description for {name}:"
            let description = Console.ReadLine()
            let character = World.Character(World.CharacterType.PC, name, description, "Healthy", [])
            ConsoleOutput.printSuccess $"\nAdded {name} to the party!\n"
            getCharacterList (character :: acc)
    
    let characters = getCharacterList []
    if List.isEmpty characters then
        ConsoleOutput.printSystemMessage "No characters created. Creating a default character...\n"
        [World.Character(World.CharacterType.PC, "Adventurer", "A brave hero ready for adventure", "Healthy", [])]
    else
        List.rev characters

async {
    let turnStopwatch = Stopwatch()
    turnStopwatch.Start()

    let playerCharacters = getPlayerCharacters ()
    
    ConsoleOutput.printSystemMessage "Initialising scene...\n"
    let! storyThread, initialScene = timedAction DungeonMaster.getInitialScene playerCharacters "Initial scene"
    ConsoleOutput.printDmDialogue $"{initialScene}\n"
    
    ConsoleOutput.printSystemMessage "Initialising World state...\n"
    do! timedAction World.initWorldState storyThread "World state"
    if verbose then printWorldState ()

    ConsoleOutput.printSystemMessage "Illustrating...\n"
    let! initSceneDescription = timedAction Illustrator.getSceneDescription () "Illustration description"    
    if verbose then ConsoleOutput.printVerboseInfo $"Illustration description:\n{initSceneDescription}\n"
    do! timedAction Illustrator.illustrateScene initSceneDescription "Illustration"

    turnStopwatch.Stop()
    if verbose then ConsoleOutput.printVerboseInfo $"Total init time {turnStopwatch.Elapsed.Seconds} seconds\n"

    while true do
        turnStopwatch.Reset()
        turnStopwatch.Start()

        ConsoleOutput.printUserPrompt "Enter the players' action:\n"
        let userAction = Console.ReadLine()

        ConsoleOutput.printSystemMessage "\nGetting action result...\n"
        let! actionResult = timedAction World.takeAction userAction "Action result"
        if verbose then ConsoleOutput.printActionResult $"Action result: {actionResult}\n"

        ConsoleOutput.printSystemMessage "Updating World state...\n"
        do! timedAction (World.applyAction userAction) actionResult "World update"
        if verbose then printWorldState ()

        ConsoleOutput.printSystemMessage "Generating narrative...\n"
        let! dmResponse = timedAction (DungeonMaster.takeTurn userAction) actionResult "Turn"
        ConsoleOutput.printDmDialogue $"\n=== DM RESPONSE ===\n{dmResponse}\n===================\n"

        ConsoleOutput.printSystemMessage "Evolving World state from narrative...\n"
        do! timedAction World.applyNarrative dmResponse "World evolution"
        if verbose then printWorldState ()

        ConsoleOutput.printSystemMessage "Illustrating...\n"
        let! sceneDescription = timedAction Illustrator.getSceneDescription () "Illustration description"
        if verbose then ConsoleOutput.printVerboseInfo $"Illustration description:\n\n{sceneDescription}\n"
        do! timedAction Illustrator.illustrateScene sceneDescription "Illustration"

        turnStopwatch.Stop()
        if verbose then ConsoleOutput.printVerboseInfo $"Total turn time {turnStopwatch.Elapsed.Seconds} seconds\n"
}
|> Async.RunSynchronously

// Compact thread with SummarizingChatReducer