module DungeonMaster

open Microsoft.Agents.AI

let dmInstructions = "" // Doesn't work if using Responses API (at least with LM Studio)

let getInitialScene () = async {
    let dmAgent = Agent.getResponseAgent dmInstructions
    let storyThread = dmAgent.GetNewThread()
    let! response = dmAgent.RunAsync("You are a dungeon master talking to the players. Be super creative, let your imagination run wild. It's the start of the adventure. Welcome the players and describe the opening scene. Don't describe your personal actions.", storyThread) |> Async.AwaitTask
    do! Agent.unloadResponseAgent ()
    return storyThread, response.Text
}

let takeTurn (thread : AgentThread) (userAction : string) = async {
    let dmAgent = Agent.getResponseAgent dmInstructions
    let! response = dmAgent.RunAsync($"The players take the following action: {userAction}\n\nWhat happens next?", thread) |> Async.AwaitTask
    do! Agent.unloadResponseAgent ()
    return response.Text
}