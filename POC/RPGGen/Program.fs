open System
open OpenAI
open OpenAI.Responses
open System.ClientModel

let llmServerRoot = "http://localhost:1234/v1"
let storyModel = "gemma-3-27b-it-qat"
let llmServer = Uri llmServerRoot
let chatClient = OpenAIClient(ApiKeyCredential("Unused"), OpenAIClientOptions(Endpoint = llmServer))

let unloadResponseAgent () = 
    Process.runCliCmd "lms unload --all" |> ignore
    Async.Sleep(1000) |> Async.RunSynchronously

let responseAgent = 
    storyModel
    |> chatClient.GetResponsesClient
    |> _.CreateAIAgent(instructions = "", tools = [||])

let game = async {
    Console.WriteLine("RPGGen initialising...\n\n")
    
    let storyThread = responseAgent.GetNewThread()
    let! initialScene = DungeonMaster.getInitialScene storyThread responseAgent

    Console.Write($"{initialScene.Text}\n\nIllustrating...\n\n")

    let! illustrationDescription = Illustrator.getIllustrationDescription storyThread responseAgent
    //Console.WriteLine $"Illustration description:\n\n{illustrationDescription.Text}\n\n\n"

    unloadResponseAgent ()

    Illustrator.illustrateScene illustrationDescription.Text |> ignore

    while true do
        Console.Write("Enter the players' action:\n\n")
        let userAction = Console.ReadLine()

        Console.Write("\n\nGenerating...\n\n\n")
        let! dmResponse = DungeonMaster.takeTurn storyThread responseAgent userAction

        Console.WriteLine $"{dmResponse.Text}\n\n\nIllustrating...\n\n\n"

        let! illustrationDescription = Illustrator.getIllustrationDescription storyThread responseAgent
        //Console.WriteLine $"Illustration description:\n\n{illustrationDescription.Text}\n\n\n"

        unloadResponseAgent ()

        Illustrator.illustrateScene illustrationDescription.Text |> ignore
}

game
 |> Async.RunSynchronously
