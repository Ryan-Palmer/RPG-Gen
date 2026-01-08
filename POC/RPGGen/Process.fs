module Process

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
