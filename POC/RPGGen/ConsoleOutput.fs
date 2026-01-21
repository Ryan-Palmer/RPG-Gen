module ConsoleOutput

open System

let private printColored color (text: string) =
    Console.ForegroundColor <- color
    Console.WriteLine(text)
    Console.ResetColor()

let printUserPrompt text = printColored ConsoleColor.Cyan text

let printDmDialogue text = printColored ConsoleColor.DarkCyan text

let printWorldState text = printColored ConsoleColor.Yellow text

let printActionResult text = printColored ConsoleColor.Magenta text

let printSystemMessage text = printColored ConsoleColor.Gray text

let printVerboseInfo text = printColored ConsoleColor.DarkGray text

let printSuccess text = printColored ConsoleColor.DarkGreen text

let printWorldStateOperation text = printColored ConsoleColor.DarkYellow text
