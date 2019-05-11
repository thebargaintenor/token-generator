open System.Drawing
open System.Drawing.Imaging

open ImageOperations

let loadbitmap (path: string) = new Bitmap(path)

let createToken (template: Bitmap) (image: Bitmap) (outputPath: string) = 
    let mask = Color.FromArgb(255, 255, 0, 255)
    let maskBounds = findMaskBounds template mask
    match maskBounds with
    | Some bounds ->
        use token = applyMask template mask bounds image
        token.Save(outputPath, ImageFormat.Png)
        Ok outputPath
    | None -> Error "no fuchsia mask found"

[<EntryPoint>]
let main argv =
    match argv with
    | [| templatePath ; imagePath ; outputPath |] -> 
        try
            use template = loadbitmap templatePath
            use image = loadbitmap imagePath
            let result = createToken template image outputPath
            match result with
            | Ok outputLocation -> printfn "Success! Your token was written to %s" outputLocation
            | Error message -> printfn "Error: %s" message
        with
            | ex -> printfn "something failed: %s \n %s" ex.Message ex.StackTrace
    | _ ->
        printfn "Missing arguments"

    printfn "Exiting happily."
    0
