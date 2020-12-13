open Argu
open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats

open ImageOperations

type CliArgs =
    | [<AltCommandLine("-t"); Mandatory; Unique>] Template of template: string
    | [<AltCommandLine("-o"); Mandatory; Unique>] Output of output: string
    | [<AltCommandLine("-m"); Unique>] Mask of mask: string
    | [<MainCommand; ExactlyOnce; Last>] Image of image: string

    interface IArgParserTemplate with
        member this.Usage =
            match this with
            | Template _ -> "Path of token template"
            | Output _ -> "Output path for finished token"
            | Mask _ -> "Masking color as RGB hex value (default FF00FF)"
            | Image _ -> "Path of image to apply to token template"

module Constraints =
    let parseColor (rgbhex: string): Argb32 =
        let mutable parsed = Color.Magenta;
        if Color.TryParseHex(rgbhex, ref parsed) then
            parsed.ToPixel<Argb32>()
        else
            failwith "Invalid color value provided"

let loadbitmap (path: string) = Image.Load<Argb32>(path)

let createToken (maskingColor: Argb32) (template: Image<Argb32>) (image: Image<Argb32>) (outputPath: string) = 
    findMaskBounds template maskingColor
    |> function
        | Some mask ->
            use token = applyMask template mask image
            token.SaveAsPng(outputPath)
            Ok outputPath
        | None -> Error "no magenta mask found"

[<EntryPoint>]
let main argv =
    let errorHandler = 
        ProcessExiter(
            colorizer =
                function
                | ErrorCode.HelpText -> None
                | _ -> Some System.ConsoleColor.Red)
    
    let parser = ArgumentParser.Create<CliArgs>(programName="TokenGenerator.exe", errorHandler=errorHandler)
    let args = parser.ParseCommandLine argv

    let templatePath = args.GetResult Template
    let imagePath = args.GetResult Image
    let outputPath = args.GetResult Output

    let mask =
        if args.Contains Mask then
            args.PostProcessResult(<@ Mask @>, Constraints.parseColor)
        else
            Argb32(255.0f, 0.0f, 255.0f, 255.0f)

    try
        use template = loadbitmap templatePath
        use image = loadbitmap imagePath
        createToken mask template image outputPath
        |> function
            | Ok outputLocation -> printfn "Success! Your token was written to %s" outputLocation
            | Error message -> printfn "Error: %s" message
    with
        | ex -> printfn "something failed: %s \n %s" ex.Message ex.StackTrace

    printfn "Exiting happily."
    0
