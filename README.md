# NupkgVersionExtractor

![Nuget](https://img.shields.io/nuget/v/NupkgVersionExtractor?style=social)

Extracting a `.nupkg` file's version should be as easy as `command pathToFile`.

This is a pretty simple project to extract the version contained in the path of a `.nupkg` file.(Meaning that no .csproj is needed!)

### dotnet-tools

```
dotnet tool install --global NupkgVersionExtractor --version 1.0.0.2
nupkg-version-extractor {pathToNupkg}
```

### Execute locally

You can download the release's file if you want to execute on a windows 10 - 64 bit system.

Otherwise, use [`dotnet publish`](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-publish) to achieve the same result for your platform.

```
git clone https://github.com/amoraitis/NupkgVersionExtractor.git

dotnet publish NupkgVersionExtractor/NupkgVersionExtractor -r {target} -c Release /p:PublishSingleFile=true -o ./NupkgVersionExtractor

.\NupkgVersionExtractor\NupkgVersionExtractor.exe {pathToNupkg}
```

[Docs](https://docs.microsoft.com/en-us/dotnet/core/deploying/) related to the `target` parameter for `dotnet publish`.
