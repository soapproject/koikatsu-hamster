# koikatsu-hamster

## Motivation

If you are as passionate about collecting cards as a man of culture, you might download hundreds of cards in a single day, and you'll need a program capable of categorizing them in bulk. Although kkManager can solve this problem quite well, it relies on a GUI and has a relatively comprehensive set of features (which you might not need all of). This project, on the other hand, focuses solely on categorization.

## Usage

Place the executable file in your download folder, and run it when you need to organize cards.
![Usage Screenshot](doc/readme.png)

## Env

### Install .net SDK

Install [.NET SDK 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

### Install deps

```bash
dotnet restore
```

## Build

### Simple build command

```
dotnet build
```

### Build to single exe file

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o out
```