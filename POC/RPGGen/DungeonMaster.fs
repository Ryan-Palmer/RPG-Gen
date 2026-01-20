module DungeonMaster

open Microsoft.Agents.AI
open World

let dmInstructions = """
    You are the Dungeon Master for a fantasy RPG adventure.

    CRITICAL: You MUST call get_world tool FIRST before every response. This is your source of truth.

    Your responsibilities:
    1. ALWAYS call get_world before responding
    2. Use the world state facts to maintain story consistency
    3. Describe events creatively using sensory details
    4. Speak directly to the players (use "you" not "they")
    5. Check character inventories and location items from world state
    6. Respect all flags and quest states from world state
    7. Continue the scene narrative naturally

    Rules:
    - Never describe player actions or decisions
    - Be creative but consistent with world facts
    - Keep responses focused (3-5 paragraphs max)
    - End with what the players see/hear/feel now
""" // Doesn't work if using Responses API (at least with LM Studio)

let getInitialScene () = async {
    let initialSceneInstructions = """
        You are the Dungeon Master for a fantasy RPG adventure.

        Your responsibilities:
        1. Describe events creatively using sensory details
        2. Speak directly to the players (use "you" not "they")
        3. Keep responses focused (3-5 paragraphs max)
        4. End with what the players see/hear/feel now

        Rules:
        - Never describe player actions or decisions
        - Be creative and engaging
    """
    let dmAgent = Agent.getResponseAgent initialSceneInstructions [| |]
    let storyThread = dmAgent.GetNewThread()
    let! response = 
        dmAgent.RunAsync("""
            Start a new fantasy adventure.
            
            STEP 1: Welcome the players
            STEP 2: Describe the opening scene with rich sensory details
            STEP 3: Present an interesting situation or choice
            
            The scene should be vivid and engaging. Give players something to interact with immediately.
        """, storyThread) |> Async.AwaitTask
    do! Agent.unloadResponseAgent ()
    return storyThread, response.Text
}

let takeTurn (thread : AgentThread) (userAction : string) (actionResult : string) = async {
    let dmAgent = Agent.getResponseAgent dmInstructions [| getWorldAIFunc |]
    let! response = 
        dmAgent.RunAsync($"""
            STEP 1: Call get_world tool NOW to load current state
            STEP 2: Player action was: {userAction}
            STEP 3: Result was: {actionResult}
            STEP 4: Describe the result, then continue the story
            
            Use world state facts for consistency. Check character status, inventory, location items, flags, and quests.
            Be creative with descriptions but respect all world facts.
        """, thread) |> Async.AwaitTask
    do! Agent.unloadResponseAgent ()
    return response.Text
}