open System
open OpenAI
open OpenAI.Responses
open System.ClientModel
open ImageGen

let runCliCmd (cmd : string) =
    let proc = new System.Diagnostics.Process()
    proc.StartInfo.FileName <- "cmd.exe"
    proc.StartInfo.Arguments <- $"/C {cmd}"
    proc.StartInfo.RedirectStandardOutput <- true
    proc.StartInfo.UseShellExecute <- false
    proc.StartInfo.CreateNoWindow <- true
    proc.Start() |> ignore
    let output = proc.StandardOutput.ReadToEnd()
    proc.WaitForExit()
    output

let unloadLLM () = 
    runCliCmd "lms unload --all" |> ignore
    Async.Sleep(1000) |> Async.RunSynchronously

let llmServerRoot = "http://localhost:1234/v1"
let storyModel = "gemma-3-27b-it-qat"
let llmServer = Uri llmServerRoot

Console.WriteLine("RPGGen initialising...\n\n")

let clientOptions = OpenAIClientOptions(Endpoint = llmServer)
let credential = ApiKeyCredential("Unused")
let chatClient = OpenAIClient(credential, clientOptions)
let responseAgent = 
    storyModel
    |> chatClient.GetResponsesClient
    |> _.CreateAIAgent(instructions = "", tools = [||])

let mutable story = "Describe a classic scene from Dungeons and Dragons as if you are the dungeon master talking to the players. Don't describe your personal actions, just your words as the dungeon master."

let initialScene = 
    responseAgent.RunAsync story
    |> Async.AwaitTask
    |> Async.RunSynchronously

Console.Write($"{initialScene.Text}\n\nIllustrating...\n\n")

unloadLLM ()
illustrateScene initialScene.Text |> ignore

while true do
    Console.Write("Enter the players' action:\n\n")
    let userAction = Console.ReadLine()
    Console.Write("\n\nGenerating...\n\n\n")
    story <- $"{story}\n\nThe players take the following action: {userAction}\n\nAs the dungeon master, describe what happens next."
    let dmResponse = 
        responseAgent.RunAsync story
        |> Async.AwaitTask
        |> Async.RunSynchronously
    Console.WriteLine $"{dmResponse.Text}Illustrating...\n\n\n"
    unloadLLM ()
    illustrateScene dmResponse.Text |> ignore
    story <- $"{story}\n\n{dmResponse.Text}"


