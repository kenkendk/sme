all: build

DEPENDS_CS=$(find . -type f -name *.cs | xargs echo)

build: SME.sln ${DEPENDS_CS}
	xbuild /p:Configuration=Release SME.sln

nupkg: build
	if [ -d nupkg ]; then rm -rf nupkg; fi
	mkdir nupkg

nupkg/SME.nupkg: SME/SME.nuspec nupkg
	cd SME; nuget pack SME.nuspec
	mv SME/SME.*.nupkg nupkg/SME.nupkg

nupkg/SME.GraphViz.nupkg: SME.GraphViz/SME.GraphViz.nuspec nupkg
	cd SME.GraphViz; nuget pack SME.GraphViz.nuspec
	mv SME.GraphViz/SME.GraphViz.*.nupkg nupkg/SME.GraphViz.nupkg
	cd ..

nupkg/SME.AST.nupkg: SME.AST/SME.AST.nuspec nupkg
	cd SME.AST; nuget pack SME.AST.nuspec
	mv SME.AST/SME.AST.*.nupkg nupkg/SME.AST.nupkg

nupkg/SME.VHDL.nupkg: SME.VHDL/SME.VHDL.nuspec nupkg
	cd SME.VHDL; nuget pack SME.VHDL.nuspec
	mv SME.VHDL/SME.VHDL.*.nupkg nupkg/SME.VHDL.nupkg

pack: nupkg nupkg/SME.nupkg nupkg/SME.GraphViz.nupkg nupkg/SME.AST.nupkg nupkg/SME.VHDL.nupkg


deploy:
	cd nupkg; nuget push SME.nuspec
	cd nupkg; nuget push SME.GraphViz.nuspec
	cd nupkg; nuget push SME.AST.nuspec
	cd nupkg; nuget push SME.VHDL.nuspec


clean:
	xbuild /p:Configuration=Clean SME.sln
	find . -type d -name obj | xargs rm
	find . -type d -name bin | xargs rm


.PHONY: all build clean
