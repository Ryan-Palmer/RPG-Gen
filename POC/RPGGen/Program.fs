open System
open OpenAI
open OpenAI.Responses
open System.ClientModel
open ImageGen

let llmServerRoot = "http://localhost:1234/v1"
let storyModel = "gemma-3-27b-it-qat"
let llmServer = Uri llmServerRoot

Console.WriteLine("RPGGen started.")

let clientOptions = OpenAIClientOptions(Endpoint = llmServer)
let credential = ApiKeyCredential("Unused")
let chatClient = OpenAIClient(credential, clientOptions)
let responseAgent = 
    storyModel
    |> chatClient.GetResponsesClient
    |> _.CreateAIAgent(instructions = "", tools = [||])

let sceneDescription = 
    responseAgent.RunAsync "Describe a classic scene from Dungeons and Dragons as if you are the dungeon master talking to the players. Don't describe your personal actions, just your words as the dungeon master."
    |> Async.AwaitTask
    |> Async.RunSynchronously

Console.WriteLine sceneDescription.Text

Async.Sleep(60000) |> Async.RunSynchronously // Wait for story model to unload

illustrateScene sceneDescription.Text


