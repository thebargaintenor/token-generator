module ImageOperations

open System.Drawing
open System.Drawing.Drawing2D

module euclidean =
    let srgb (want: Color) (got: Color) =
        let rbar = (double want.R - double got.R) / 2.
        sqrt (
            (2. + rbar / 256.) * (double want.R - double got.R) ** 2. +
            4. * (double want.G - double got.G) ** 2. +
            (2. + (255. - rbar) / 256.) * (double want.B - double got.B) ** 2.)
    
    let naive (want: Color) (got: Color) =
        sqrt (
            (double want.A - double got.A) ** 2. +
            (double want.R - double got.R) ** 2. +
            (double want.G - double got.G) ** 2. +
            (double want.B - double got.B) ** 2.)

type TransparencyMask = {
    Bounds: Rectangle;
    Filter: bool array array;
}

let colorsCloseEnough (want: Color) (got: Color): bool =
    let tolerance = 50.0
    let delta = euclidean.srgb want got
    delta <= tolerance

let containsColor (color: Color) (tolerance: int) (scanline: Color seq) =
    let matchingPixels = 
        scanline
        |> Seq.filter (colorsCloseEnough color)
        |> Seq.length
    matchingPixels >= tolerance

let horizontalScan (image: Bitmap) (y: int) =
    seq {0 .. image.Width - 1}
    |> Seq.map (fun x -> image.GetPixel(x, y))

let getTransparencyMask (alpha: Color) (image: Bitmap) =
    let rowScan (y: int): bool array =
        [| 0 .. image.Width - 1 |]
        |> Array.map (fun x -> colorsCloseEnough alpha (image.GetPixel(x, y)))
    
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

let findMaskBounds (image: Bitmap) (mask: Color): TransparencyMask option =
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

let resizeToBounds (image: Bitmap) (bounds: Rectangle): Bitmap = 
    let width, height =
        let widthF, heightF = (double image.Width), (double image.Height)
        if image.Width > image.Height then
            int ((double bounds.Height) * widthF / heightF), bounds.Height
        else
            bounds.Width, int ((double bounds.Width) * heightF / widthF)

    let resized = new Bitmap(width, height)
    use g = Graphics.FromImage(resized)
    g.InterpolationMode <- InterpolationMode.Bicubic
    g.DrawImage(image, 0, 0, width, height)
    resized

let applyMask (template: Bitmap) (mask: TransparencyMask) (image: Bitmap): Bitmap = 
    let {Bounds=bounds; Filter=filter} = mask
    let output = new Bitmap(template.Width, template.Height)
    use resizedContent = resizeToBounds image mask.Bounds

    let rx, ry =
        let landscape = resizedContent.Width >= resizedContent.Height
        if landscape then
            bounds.X - (resizedContent.Width - bounds.Width) / 2, bounds.Y
        else
            bounds.X, bounds.Y - (resizedContent.Height - bounds.Height) / 2

    let transformToContent x y = x - rx, y - ry
    let showPixel x y = filter.[y].[x]

    use g = Graphics.FromImage(output)

    // y in this system is relative to resized image
    let drawScanline y =
        horizontalScan template y
        |> Seq.iteri 
            (fun x _ -> 
                if showPixel x y then
                    let cx, cy = transformToContent x y
                    template.SetPixel(x, y, resizedContent.GetPixel(cx, cy)))

    // overlay content with horizontal scans and draw result
    {0 .. template.Height - 1} |> Seq.iter drawScanline
    g.DrawImage(template, 0, 0, template.Width, template.Height)
    output