module ImageOperations

open SixLabors.ImageSharp
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing

module euclidean =
    let srgb (want: Argb32) (got: Argb32) =
        let rbar = (double want.R - double got.R) / 2.
        sqrt (
            (2. + rbar / 256.) * (double want.R - double got.R) ** 2. +
            4. * (double want.G - double got.G) ** 2. +
            (2. + (255. - rbar) / 256.) * (double want.B - double got.B) ** 2.)
    
    let naive (want: Argb32) (got: Argb32) =
        sqrt (
            (double want.A - double got.A) ** 2. +
            (double want.R - double got.R) ** 2. +
            (double want.G - double got.G) ** 2. +
            (double want.B - double got.B) ** 2.)

type TransparencyMask = {
    Bounds: Rectangle;
    Filter: bool array array;
}

let colorsCloseEnough (want: Argb32) (got: Argb32): bool =
    let tolerance = 50.0
    let delta = euclidean.srgb want got
    delta <= tolerance

let containsColor (color: Argb32) (tolerance: int) (scanline: Argb32 seq) =
    let matchingPixels = 
        scanline
        |> Seq.filter (colorsCloseEnough color)
        |> Seq.length
    matchingPixels >= tolerance

let horizontalScan (image: Image<Argb32>) (y: int) =
    seq {0 .. image.Width - 1}
    |> Seq.map (fun x -> image.Item(x, y))

let getTransparencyMask (alpha: Argb32) (image: Image<Argb32>) =
    let rowScan (y: int): bool array =
        [| 0 .. image.Width - 1 |]
        |> Array.map (fun x -> colorsCloseEnough alpha (image.Item(x, y)))
    
    [| 0 .. image.Height - 1 |]
    |> Array.map (rowScan)

let dumpMaskToConsole (mask: bool array array) =
    for row in mask do
        row 
        |> Array.map(fun a -> if a then '*' else ' ') 
        |> System.String
        |> printfn "%s"

let columnFrom2DArray (xys: bool array array) (x: int) =
    xys |> Array.map (fun xs -> xs.[x])

let rowFrom2DArray (xys: bool array array) (y: int) = xys.[y]

let findMaskBounds (image: Image<Argb32>) (mask: Argb32): TransparencyMask option =
    let transparencyMask = getTransparencyMask mask image

    try
        let left = 
            {0 .. image.Width - 1}
            |> Seq.map (columnFrom2DArray transparencyMask) 
            |> Seq.findIndex (Array.reduce (||))
        let top = 
            {0 .. image.Height - 1}
            |> Seq.map (rowFrom2DArray transparencyMask)
            |> Seq.findIndex (Array.reduce (||))
        let right = 
            {0 .. image.Width - 1}
            |> Seq.map (columnFrom2DArray transparencyMask)
            |> Seq.findIndexBack (Array.reduce (||))       // right
        let bottom =
            {0 .. image.Height - 1}
            |> Seq.map (rowFrom2DArray transparencyMask)
            |> Seq.findIndexBack (Array.reduce (||))
        let output = {
            Bounds=Rectangle(left, top, right - left + 1, bottom - top + 1); // right and bottom bound are not exclusive
            Filter=transparencyMask;
        }
        Some(output)
    with
        | _ -> None

let private resizeToFill (bounds: Rectangle) (image: Image<Argb32>): Image<Argb32> =
    let transformation (context: IImageProcessingContext) =
        let resizeOptions =
            ResizeOptions
                (Mode = ResizeMode.Crop, Size = Size(bounds.Width, bounds.Height), Sampler = KnownResamplers.Bicubic)

        context.Resize(resizeOptions) |> ignore

    image.Clone(transformation)

let applyMask (template: Image<Argb32>) (mask: TransparencyMask) (image: Image<Argb32>): Image<Argb32> = 
    let {Bounds=bounds; Filter=filter} = mask
    use resizedContent = resizeToFill mask.Bounds image

    let transformToContent x y = x - bounds.X, y - bounds.Y
    let showPixel x y = filter.[y].[x]

    let output = new Image<Argb32>(template.Width, template.Height)

    let drawScanline y =
        horizontalScan template y
        |> Seq.iteri 
            (fun x _ -> 
                output.Item(x, y) <-
                    if showPixel x y then
                        let cx, cy = transformToContent x y
                        resizedContent.Item(cx, cy)
                    else
                        template.Item(x, y))

    {0 .. template.Height - 1} |> Seq.iter drawScanline
    output