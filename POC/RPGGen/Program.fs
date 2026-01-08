open System
open OpenAI
open OpenAI.Responses
open System.ClientModel
open System.Diagnostics

let llmServerRoot = "http://localhost:1234/v1"
let storyModel = "gemma-3-27b-it-qat"
let llmServer = Uri llmServerRoot
let chatClient = OpenAIClient(ApiKeyCredential("Unused"), OpenAIClientOptions(Endpoint = llmServer))

let unloadResponseAgent () = 
    Process.runCliCmd "lms unload --all" |> ignore
    Async.Sleep(1000)

let responseAgent = 
    storyModel
    |> chatClient.GetResponsesClient
    |> _.CreateAIAgent(instructions = "", tools = [||])

let stopwatch = Stopwatch()

let verbose = true

let game = async {
    Console.WriteLine("RPGGen initialising...\n")
    stopwatch.Start()
    
    let storyThread = responseAgent.GetNewThread()
    let! initialScene = DungeonMaster.getInitialScene storyThread responseAgent

    stopwatch.Stop()
    let initTurnSeconds = stopwatch.Elapsed.Seconds
    if verbose then Console.WriteLine($"Initial scene generated in {initTurnSeconds} seconds\n\n")
    stopwatch.Reset()
    stopwatch.Start()

    Console.WriteLine($"{initialScene.Text}\n\nIllustrating...\n")
    
    let! illustrationDescription = Illustrator.getIllustrationDescription storyThread responseAgent
    if verbose then Console.WriteLine $"Illustration description:\n\n{illustrationDescription.Text}\n\n"

    stopwatch.Stop()
    let initIllustrationDescriptionSeconds = stopwatch.Elapsed.Seconds
    if verbose then Console.WriteLine($"Illustration description generated in {initIllustrationDescriptionSeconds} seconds\n\n")
    stopwatch.Reset()
    stopwatch.Start()

    do! unloadResponseAgent ()

    Illustrator.illustrateScene illustrationDescription.Text |> ignore

    stopwatch.Stop()
    let initIllustrationSeconds = stopwatch.Elapsed.Seconds
    if verbose then Console.WriteLine($"Illustration generated in {initIllustrationSeconds} seconds\n")

    if verbose then Console.WriteLine($"Total init time {initTurnSeconds + initIllustrationDescriptionSeconds + initIllustrationSeconds} seconds\n\n")

    while true do
        stopwatch.Reset()
        stopwatch.Start()

        Console.WriteLine("Enter the players' action:\n")
        let userAction = Console.ReadLine()

        Console.WriteLine("\n\nGenerating...\n")
        let! dmResponse = DungeonMaster.takeTurn storyThread responseAgent userAction
        
        stopwatch.Stop()
        let turnSeconds = stopwatch.Elapsed.Seconds
        if verbose then Console.WriteLine($"Turn generated in {turnSeconds} seconds\n")
        stopwatch.Reset()
        stopwatch.Start()

        Console.WriteLine $"{dmResponse.Text}\n\n\nIllustrating...\n"

        let! illustrationDescription = Illustrator.getIllustrationDescription storyThread responseAgent
        if verbose then Console.WriteLine $"Illustration description:\n\n{illustrationDescription.Text}\n\n"

        stopwatch.Stop()
        let illustrationDescriptionSeconds = stopwatch.Elapsed.Seconds
        if verbose then Console.WriteLine($"Illustration description generated in {illustrationDescriptionSeconds} seconds\n")
        stopwatch.Reset()
        stopwatch.Start()

        do! unloadResponseAgent ()

        Illustrator.illustrateScene illustrationDescription.Text |> ignore

        stopwatch.Stop()
        let illustrationSeconds = stopwatch.Elapsed.Seconds
        if verbose then Console.WriteLine($"Illustration generated in {illustrationSeconds} seconds\n\n")
        stopwatch.Reset()
        stopwatch.Start()

        if verbose then Console.WriteLine($"Total turn time {turnSeconds + illustrationDescriptionSeconds + illustrationSeconds} seconds\n\n")
}

game
|> Async.RunSynchronously