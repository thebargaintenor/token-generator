open System.Drawing
open System.Drawing.Imaging

open ImageOperations

type Result<'a> =
| Success of 'a
| Failure of string

let loadbitmap (path: string) = new Bitmap(path)

let createToken (template: Bitmap) (image: Bitmap) (outputPath: string): string Result = 
    printfn "something happened"
    let mask = Color.FromArgb(255, 255, 0, 255)
    let maskBounds = findMaskBounds template mask
    match maskBounds with
    | Some bounds ->
        use token = applyMask template mask bounds image
        token.Save(outputPath, ImageFormat.Png)
        Success outputPath
    | None -> Failure "no fuchsia mask found"

[<EntryPoint>]
let main argv =
    match argv with
    | [| templatePath ; imagePath ; outputPath |] -> 
        printfn "template: %s, image: %s" templatePath imagePath
        try
            use template = loadbitmap templatePath
            use image = loadbitmap imagePath
            let result = createToken template image outputPath
            match result with
            | Success outputLocation -> printfn "Success! Your token was written to %s" outputLocation
            | Failure message -> printfn "Error: %s" message
        with
            | _ -> printfn "failed to load image"
    | _ ->
        printfn "Missing arguments"

    printfn "Hello World from F#!"
    0 // return an integer exit code
