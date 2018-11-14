# Token Generator

A highly opinionated D&D token generator that assumes your template uses #FF00FF for masking.  Transparency is allowed, and the output is a PNG.  You can tell it whatever you want, but it'll be a PNG underneath.

## Usage

This doesn't include anything prebuilt, so you'll need the .NET Core 2 SDK.

### Got Linux or MacOS?

```
git clone git@github.com:thebargaintenor/token-generator
cd token-generator
dotnet build
dotnet run templatePath contentPath outputPath
```

Or you could clone the repo and use MonoDevelop/VS for Mac.  The Nuget dependencies should factor in somewhere.

### Windows?

Visual Studio is fine too.