module DungeonMaster

open Microsoft.Agents.AI
open World

let dmInstructions = """
    You are a dungeon master talking to the players.
    Be super creative, let your imagination run wild.
    Don't describe your personal actions.
    IMPORTANT: ALWAYS first check the world facts using your get_world tool.
""" // Doesn't work if using Responses API (at least with LM Studio)

let getInitialScene () = async {
    let dmAgent = Agent.getResponseAgent dmInstructions [| getWorldAIFunc |]
    let storyThread = dmAgent.GetNewThread()
    let! response = dmAgent.RunAsync("It's the start of the adventure. Welcome the players and describe the opening scene.", storyThread) |> Async.AwaitTask
    do! Agent.unloadResponseAgent ()
    return storyThread, response.Text
}

let takeTurn (thread : AgentThread) (userAction : string) (actionResult : string) = async {
    let dmAgent = Agent.getResponseAgent dmInstructions [| getWorldAIFunc |]
    let! response = 
        dmAgent.RunAsync($"""
            The players took the following action: {userAction}\n\nThis resulted in the following change:{actionResult}.
            First describe the change, then continue the story.
        """, thread) |> Async.AwaitTask
    do! Agent.unloadResponseAgent ()
    return response.Text
}