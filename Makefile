PROJECT := src/VcvPatchBridge
CONFIG  := Release

.PHONY: build test publish-linux publish-win publish-osx publish-all clean

build:
	dotnet build

test:
	dotnet test

publish-linux:
	dotnet publish $(PROJECT) -c $(CONFIG) -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64

publish-win:
	dotnet publish $(PROJECT) -c $(CONFIG) -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64

publish-osx:
	dotnet publish $(PROJECT) -c $(CONFIG) -r osx-x64 --self-contained -p:PublishSingleFile=true -o publish/osx-x64

publish-all: publish-linux publish-win

clean:
	rm -rf publish
	dotnet clean
