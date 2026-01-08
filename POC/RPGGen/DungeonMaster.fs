module DungeonMaster

open Microsoft.Agents.AI

let initPrompt = "Describe a classic scene from Dungeons and Dragons as if you are the dungeon master talking to the players. Be super creative, let your imagination run wild. Don't describe your personal actions, just your words as the dungeon master."

let getInitialScene (thread : AgentThread) (agent : ChatClientAgent) =
    agent.RunAsync(initPrompt, thread)
    |> Async.AwaitTask

let takeTurn (thread : AgentThread) (agent : ChatClientAgent) (userAction : string)=
    agent.RunAsync($"The players take the following action: {userAction}\n\nAs the dungeon master, describe what happens next. Don't describe your personal actions, just your words as the dungeon master.", thread)
    |> Async.AwaitTask