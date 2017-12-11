all: build

DEPENDS_CSPROJ=$(find . -type f -name "*.csproj" | xargs echo)

PROJECT_NAME=SME
DEPENDS_CS=$(find . -type f -name "*.cs" | xargs echo)
DEPENDS_TT=$(find . -type f -name "*.tt" | xargs echo)
DEPENDS_CSPROJ=$(find . -type f -name "*.csproj" | xargs echo)
VERSION=$(shell cat SME/SME.nuspec | grep "<version>" | tr -d "[A-z/<>] ")

build: SME.sln ${DEPENDS_CS} ${DEPENDS_TT} ${DEPENDS_CSPROJ}
	msbuild /p:Configuration=Release SME.sln

nupkg: build
	if [ -d nupkg ]; then rm -rf nupkg; fi
	mkdir nupkg

nupkg/SME.nupkg: SME/SME.nuspec nupkg
	cd SME; nuget pack SME.nuspec
	pwd
	mv SME/SME.$(VERSION).nupkg nupkg/SME.$(VERSION).nupkg

nupkg/SME.Tracer.nupkg: SME.Tracer/SME.Tracer.nuspec nupkg
	cd SME.Tracer; nuget pack SME.Tracer.nuspec
	mv SME.Tracer/SME.Tracer.$(VERSION).nupkg nupkg/SME.Tracer.$(VERSION).nupkg

nupkg/SME.GraphViz.nupkg: SME.GraphViz/SME.GraphViz.nuspec nupkg
	cd SME.GraphViz; nuget pack SME.GraphViz.nuspec
	mv SME.GraphViz/SME.GraphViz.$(VERSION).nupkg nupkg/SME.GraphViz.$(VERSION).nupkg

nupkg/SME.AST.nupkg: SME.AST/SME.AST.nuspec nupkg
	cd SME.AST; nuget pack SME.AST.nuspec
	mv SME.AST/SME.AST.$(VERSION).nupkg nupkg/SME.AST.$(VERSION).nupkg

nupkg/SME.CPP.nupkg: SME.CPP/SME.CPP.nuspec nupkg
	cd SME.CPP; nuget pack SME.CPP.nuspec
	mv SME.CPP/SME.CPP.$(VERSION).nupkg nupkg/SME.CPP.$(VERSION).nupkg

nupkg/SME.VHDL.nupkg: SME.VHDL/SME.VHDL.nuspec nupkg
	cd SME.VHDL; nuget pack SME.VHDL.nuspec
	mv SME.VHDL/SME.VHDL.$(VERSION).nupkg nupkg/SME.VHDL.$(VERSION).nupkg

pack: nupkg nupkg/SME.nupkg nupkg/SME.Tracer.nupkg nupkg/SME.GraphViz.nupkg nupkg/SME.AST.nupkg nupkg/SME.CPP.nupkg nupkg/SME.VHDL.nupkg


deploy:
	cd nupkg; nuget push SME.$(VERSION).nupkg -src https://www.nuget.org
	cd nupkg; nuget push SME.Tracer.$(VERSION).nupkg -src https://www.nuget.org
	cd nupkg; nuget push SME.GraphViz.$(VERSION).nupkg -src https://www.nuget.org
	cd nupkg; nuget push SME.AST.$(VERSION).nupkg -src https://www.nuget.org
	cd nupkg; nuget push SME.CPP.$(VERSION).nupkg -src https://www.nuget.org
	cd nupkg; nuget push SME.VHDL.$(VERSION).nupkg -src https://www.nuget.org


clean:
	msbuild /target:Clean SME.sln
	find . -type d -name obj | xargs rm -r
	find . -type d -name bin | xargs rm -r
	if [ -f nupkg/SME.$(VERSION).nupkg ]; then rm nupkg/SME.$(VERSION).nupkg; fi
	if [ -f nupkg/SME.Tracer.$(VERSION).nupkg ]; then rm nupkg/SME.Tracer.$(VERSION).nupkg; fi
	if [ -f nupkg/SME.GraphViz.$(VERSION).nupkg ]; then rm nupkg/SME.GraphViz.$(VERSION).nupkg; fi
	if [ -f nupkg/SME.AST.$(VERSION).nupkg ]; then rm nupkg/SME.AST.$(VERSION).nupkg; fi
	if [ -f nupkg/SME.VHDL.$(VERSION).nupkg ]; then rm nupkg/SME.VHDL.$(VERSION).nupkg; fi



.PHONY: all build clean
