# Token Generator

A ~highly~ mostly opinionated D&D token generator.  Transparency is allowed, and the output is a PNG.  You can tell it whatever you want, but it'll be a PNG underneath.

By default, it will assume your masking color is magenta (#FFOOFF), but that is configurable with `--mask`.  Valid RGB hex values are accepted, but if you use `#`, remember to quote your value to avoid confusing your shell.  

## Usage

```
USAGE: TokenGenerator.exe [--help] --template <template> --output <output> [--mask <Mask>] <image>

IMAGE:

    <image>               Path of image to apply to token template

OPTIONS:

    --template, -t <template>
                          Path of token template
    --output, -o <output> Output path for finished token
    --mask, -m <mask>     Masking color as RGB hex value (default FF00FF)
    --help                display this list of options.
```

### Building

This doesn't include anything prebuilt, so you'll need the .NET Core 3.1 SDK.

For all your Linux, MacOS, and WSL needs:

```
git clone git@github.com:thebargaintenor/token-generator.git
cd token-generator/TokenGenerator
dotnet build
dotnet run -t templatePath -o outputPath imagePath
```

You are welcome to use your IDE of choice if you'd prefer.

## Eventual Features

Processing in batches is on the to-do list, and I'll get there someday.