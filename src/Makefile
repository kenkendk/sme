VERSION=$(shell cat SME/SME.csproj | grep "<PackageVersion>" | cut -f 2 -d ">" | cut -f 1 -d "<")
PKGS=SME SME.AST SME.Components SME.CPP SME.GraphViz SME.Tracer SME.VHDL
PKG_TARGETS=$(PKGS:%=nupkg/%.$(VERSION).nupkg)
API=$(shell cat ~/.config/nuget_api.txt)

build: SME.sln
	dotnet build -c Release SME.sln

nupkg_folder:
	if [ -d nupkg ]; then rm -rf nupkg; fi
	mkdir nupkg

nupkg/%.$(VERSION).nupkg: %
	dotnet pack -c Release -o nupkg -p:PackageVersion=$(VERSION) $<

pack: build nupkg_folder $(PKG_TARGETS)

test: build
	dotnet test --no-build --logger:"console;verbosity=normal" UnitTest

deploy: pack
	dotnet nuget push "nupkg/*.nupkg" -s https://api.nuget.org/v3/index.json -k $(API)

all: build

clean:
	dotnet clean
	find . -type d -name obj | xargs rm -rf
	find . -type d -name bin | xargs rm -rf
	rm -rf nupkg