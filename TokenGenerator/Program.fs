open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats

open ImageOperations

let loadbitmap (path: string) = Image.Load<Argb32>(path)

let createToken (template: Image<Argb32>) (image: Image<Argb32>) (outputPath: string) = 
    let magenta = Argb32(byte 255, byte 0, byte 255, byte 255)
    findMaskBounds template magenta
    |> function
        | Some mask ->
            use token = applyMask template mask image
            token.SaveAsPng(outputPath)
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
