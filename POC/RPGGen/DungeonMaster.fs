module DungeonMaster

open Microsoft.Agents.AI
open World

let dmInstructions = """
    You are the Dungeon Master for a fantasy RPG adventure.

    CRITICAL: You MUST call get_world tool FIRST before every response. The world state is your ONLY source of history and truth.

    Your responsibilities:
    1. ALWAYS call get_world before responding to see the full history and current state
    2. Use RecentEvents from world state to understand what just happened
    3. Use all world state facts (characters, items, flags, quests, locations) to maintain consistency
    4. Describe events creatively using sensory details based on the current action and result
    5. Speak directly to the players (use "you" not "they")
    6. Check character inventories, status, and location items from world state
    7. Respect all flags and quest states from world state
    8. Continue the scene narrative naturally based on world state context

    Rules:
    - The world state contains ALL history through RecentEvents and other fields
    - Never describe player actions or decisions
    - Be creative but consistent with world facts
    - Keep responses focused (3-5 paragraphs max)
    - End with what the players see/hear/feel now
""" // Doesn't work if using Responses API (at least with LM Studio)

let getInitialScene (characters: Character list) = async {
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
    
    let characterDescriptions = 
        characters
        |> List.map (fun c -> $"- {c.Name}: {c.Description}")
        |> String.concat "\n"
    
    let! response = 
        dmAgent.RunAsync($"""
            Start a new fantasy adventure with these Player characters:
            
            {characterDescriptions}
            
            STEP 1: Welcome the players and acknowledge their characters
            STEP 2: Describe the opening scene with rich sensory details
            STEP 3: Present an interesting situation or choice
            
            The scene should be vivid and engaging. Give players something to interact with immediately.
        """, storyThread) |> Async.AwaitTask
    do! Agent.unloadResponseAgent ()
    return storyThread, response.Text
}

let takeTurn (userAction : string) (actionResult : string) = async {
    let dmAgent = Agent.getResponseAgent dmInstructions [| getWorldAIFunc |]
    let thread = dmAgent.GetNewThread()
    let! response = 
        dmAgent.RunAsync($"""
            STEP 1: Call get_world tool NOW to load current state and history
            STEP 2: Review RecentEvents and current world state to understand context
            STEP 3: Player action was: {userAction}
            STEP 4: Result was: {actionResult}
            STEP 5: Describe the result vividly, then continue the story
            
            Use world state facts for consistency. Check character status, inventory, location items, flags, and quests.
            The world state contains all the history you need through RecentEvents and other fields.
            Be creative with descriptions but respect all world facts.
        """, thread) |> Async.AwaitTask
    do! Agent.unloadResponseAgent ()
    return response.Text
}