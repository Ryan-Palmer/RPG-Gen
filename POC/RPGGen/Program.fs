open System.Diagnostics
open System

let stopwatch = Stopwatch()
let verbose = true

let game = async {
    Console.WriteLine("RPGGen initialising...\n")
    stopwatch.Start()
    
    let! storyThread, initialScene = DungeonMaster.getInitialScene ()

    stopwatch.Stop()
    let initTurnSeconds = stopwatch.Elapsed.Seconds
    if verbose then Console.WriteLine($"Initial scene generated in {initTurnSeconds} seconds\n\n")
    stopwatch.Reset()
    stopwatch.Start()

    Console.WriteLine($"{initialScene}\n\nIllustrating...\n")
    
    let! initSceneDescription = Illustrator.getSceneDescription storyThread
    if verbose then Console.WriteLine $"Illustration description:\n\n{initSceneDescription}\n\n"

    stopwatch.Stop()
    let initSceneDescriptionSeconds = stopwatch.Elapsed.Seconds
    if verbose then Console.WriteLine($"Illustration description generated in {initSceneDescriptionSeconds} seconds\n\n")
    stopwatch.Reset()
    stopwatch.Start()

    do! Illustrator.illustrateScene initSceneDescription

    stopwatch.Stop()
    let initIllustrationSeconds = stopwatch.Elapsed.Seconds
    if verbose then Console.WriteLine($"Illustration generated in {initIllustrationSeconds} seconds\n")

    if verbose then Console.WriteLine($"Total init time {initTurnSeconds + initSceneDescriptionSeconds + initIllustrationSeconds} seconds\n\n")

    while true do
        stopwatch.Reset()
        stopwatch.Start()

        Console.WriteLine("Enter the players' action:\n")
        let userAction = Console.ReadLine()

        Console.WriteLine("\n\nGenerating...\n")
        let! dmResponse = DungeonMaster.takeTurn storyThread userAction
        
        stopwatch.Stop()
        let turnSeconds = stopwatch.Elapsed.Seconds
        if verbose then Console.WriteLine($"Turn generated in {turnSeconds} seconds\n")
        stopwatch.Reset()
        stopwatch.Start()

        Console.WriteLine $"{dmResponse}\n\n\nIllustrating...\n"

        let! sceneDescription = Illustrator.getSceneDescription storyThread
        if verbose then Console.WriteLine $"Illustration description:\n\n{sceneDescription}\n\n"

        stopwatch.Stop()
        let sceneDescriptionSeconds = stopwatch.Elapsed.Seconds
        if verbose then Console.WriteLine($"Illustration description generated in {sceneDescriptionSeconds} seconds\n")
        stopwatch.Reset()
        stopwatch.Start()

        do! Illustrator.illustrateScene sceneDescription

        stopwatch.Stop()
        let illustrationSeconds = stopwatch.Elapsed.Seconds
        if verbose then Console.WriteLine($"Illustration generated in {illustrationSeconds} seconds\n\n")
        stopwatch.Reset()
        stopwatch.Start()

        if verbose then Console.WriteLine($"Total turn time {turnSeconds + sceneDescriptionSeconds + illustrationSeconds} seconds\n\n")
}

game
|> Async.RunSynchronously