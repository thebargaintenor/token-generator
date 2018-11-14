module ImageOperations

open System.Drawing
open System.Drawing.Drawing2D

let containsColor (color: Color) (tolerance: int) (scanline: Color seq) =
    scanline
    |> Seq.filter color.Equals
    |> Seq.length
    |> (>=) tolerance

let verticalScan (image: Bitmap) (x: int) =
    seq {0 .. image.Height}
    |> Seq.map (fun y -> image.GetPixel(x, y))

let horizontalScan (image: Bitmap) (y: int) =
    seq {0 .. image.Width}
    |> Seq.map (fun x -> image.GetPixel(x, y))

let scan (image: Bitmap) (color: Color) (scanningFunction: Bitmap -> int -> Color seq) (range: int seq) =
    let matchingScan x = scanningFunction image x |> containsColor color 1
    range
    |> Seq.tryFindIndex matchingScan

let findMaskBounds (image: Bitmap) (mask: Color): Rectangle option =
    let maskScan = scan image mask
    // is there a more idiomatic way for this?
    let scannedBounds = [|
        maskScan horizontalScan {0 .. image.Width}              // left
        maskScan verticalScan {0 .. image.Height}               // top
        maskScan horizontalScan (Seq.rev {0 .. image.Width})    // right
        maskScan verticalScan (Seq.rev {0 .. image.Height})     // bottom
        |]
    match scannedBounds with
    | [| Some left; Some top; Some right; Some bottom |] -> Some (new Rectangle(left, top, right - left, bottom - top))
    | _ -> None

let resizeToBounds (image: Bitmap) (bounds: Rectangle): Bitmap = 
    let width, height =
        let widthF, heightF = (double image.Width), (double image.Height)
        if image.Width > image.Height then
            int (widthF * heightF / widthF), bounds.Height
        else
            bounds.Height, int (heightF * widthF / heightF)

    let resized = new Bitmap(width, height)
    use g = Graphics.FromImage(resized)
    g.InterpolationMode <- InterpolationMode.Bicubic
    g.DrawImage(image, 0, 0, bounds.Width, bounds.Height)
    resized

let applyMask (template: Bitmap) (mask: Color) (maskBounds: Rectangle) (image: Bitmap): Bitmap = 
    let output = new Bitmap(template.Width, template.Height)
    let transform x y =
        x + maskBounds.X, y + maskBounds.Y
    let showPixel x y =
        let tx, ty = transform x y
        mask.Equals(template.GetPixel(tx, ty))

    use resizedContent = resizeToBounds image maskBounds
    use g = Graphics.FromImage(output)

    let drawScanline y =
        let scanline = horizontalScan resizedContent y
        scanline
        |> Seq.iteri 
            (fun x c -> 
                if showPixel x y then 
                    let tx, ty = transform x y
                    template.SetPixel(tx, ty, c))

    // overlay content with horizontal scans and draw result
    {0 .. resizedContent.Height} |> Seq.iter drawScanline
    g.DrawImage(template, 0, 0, template.Width, template.Height)
    output