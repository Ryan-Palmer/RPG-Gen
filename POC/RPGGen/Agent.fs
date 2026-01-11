module Agent

open System
open OpenAI
open OpenAI.Responses
open OpenAI.Chat
open System.ClientModel
open Microsoft.Agents.AI

let llmServerRoot = "http://localhost:1234/v1"
let storyModel = "google/gemma-3-27b"//"openai/gpt-oss-120b"
let llmServer = Uri llmServerRoot
let chatClient = OpenAIClient(ApiKeyCredential("Unused"), OpenAIClientOptions(Endpoint = llmServer))

let copyThread (agent : ChatClientAgent) (thread : AgentThread) = 
    let t =  thread.Serialize()
    t |> agent.DeserializeThread

let unloadResponseAgent () = 
    Process.runCliCmd "lms unload --all" |> ignore
    Async.Sleep(1000)

#nowarn 57
let getResponseAgent instructions = 
    storyModel
    //|> chatClient.GetResponsesClient
    |> chatClient.GetChatClient
    |> _.CreateAIAgent(instructions = instructions, tools = [||])