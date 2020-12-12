open System.Drawing
open System.Drawing.Imaging

open ImageOperations

let loadbitmap (path: string) = new Bitmap(path)

let createToken (template: Bitmap) (image: Bitmap) (outputPath: string) = 
    let magenta = Color.FromArgb(255, 255, 0, 255)
    findMaskBounds template magenta
    |> function
        | Some mask ->
            use token = applyMask template mask image
            token.Save(outputPath, ImageFormat.Png)
            Ok outputPath
        | None -> Error "no magenta mask found"

[<EntryPoint>]
let main argv =
    match argv with
    | [| templatePath ; imagePath ; outputPath |] -> 
        try
            use template = loadbitmap templatePath
            use image = loadbitmap imagePath
            createToken template image outputPath
            |> function
                | Ok outputLocation -> printfn "Success! Your token was written to %s" outputLocation
                | Error message -> printfn "Error: %s" message
        with
            | ex -> printfn "something failed: %s \n %s" ex.Message ex.StackTrace
    | _ ->
        printfn "Missing arguments"

    printfn "Exiting happily."
    0
