module ImageOperations

open System.Drawing
open System.Drawing.Drawing2D

let colorsCloseEnough (want: Color) (got: Color): bool =
    let tolerance = 50.0
    let delta = 
        sqrt (
            (double want.A - double got.A) ** 2. +
            (double want.R - double want.R) ** 2. +
            (double want.G - double want.G) ** 2. +
            (double want.B - double want.B) ** 2.)
    delta <= tolerance

let containsColor (color: Color) (tolerance: int) (scanline: Color seq) =
    let matchingPixels = 
        scanline
        |> Seq.filter (colorsCloseEnough color)
        |> Seq.length
    matchingPixels >= tolerance

let verticalScan (image: Bitmap) (x: int) =
    seq {0 .. image.Height - 1}
    |> Seq.map (fun y -> image.GetPixel(x, y))

let horizontalScan (image: Bitmap) (y: int) =
    seq {0 .. image.Width - 1}
    |> Seq.map (fun x -> image.GetPixel(x, y))

let scan (image: Bitmap) (color: Color) (scanningFunction: Bitmap -> int -> Color seq) (range: int seq): int option =
    let matchingScan x = scanningFunction image x |> containsColor color 1
    range
    |> Seq.tryFind matchingScan

let findMaskBounds (image: Bitmap) (mask: Color): Rectangle option =
    let maskScan = scan image mask
    // is there a more idiomatic way for this?
    let scannedBounds = [|
        maskScan verticalScan {0 .. image.Width - 1}                  // left
        maskScan horizontalScan {0 .. image.Height - 1}               // top
        maskScan verticalScan (Seq.rev {0 .. image.Width - 1})        // right
        maskScan horizontalScan (Seq.rev {0 .. image.Height - 1})     // bottom
        |]
    match scannedBounds with
    | [| Some left; Some top; Some right; Some bottom |] -> Some (new Rectangle(left, top, right - left, bottom - top))
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

let applyMask (template: Bitmap) (mask: Color) (maskBounds: Rectangle) (image: Bitmap): Bitmap = 
    let output = new Bitmap(template.Width, template.Height)
    use resizedContent = resizeToBounds image maskBounds

    let rx, ry =
        let landscape = resizedContent.Width >= resizedContent.Height
        if landscape then
            maskBounds.X - (resizedContent.Width - maskBounds.Width) / 2, maskBounds.Y
        else
            maskBounds.X, maskBounds.Y - (resizedContent.Height - maskBounds.Height) / 2

    let transformToContent x y = x - rx, y - ry
    let showPixel x y =
        let inMaskBounds =
            x >= maskBounds.X && x < maskBounds.X + maskBounds.Width &&
            y >= maskBounds.Y && y < maskBounds.Y + maskBounds.Height

        if inMaskBounds then
            colorsCloseEnough mask (template.GetPixel(x, y))
        else
            false

    use g = Graphics.FromImage(output)

    // y in this system is relative to resized image
    let drawScanline y =
        horizontalScan template y
        |> Seq.iteri 
            (fun x c -> 
                if showPixel x y then
                    let cx, cy = transformToContent x y
                    template.SetPixel(x, y, resizedContent.GetPixel(cx, cy)))

    // overlay content with horizontal scans and draw result
    {0 .. template.Height - 1} |> Seq.iter drawScanline
    g.DrawImage(template, 0, 0, template.Width, template.Height)
    output