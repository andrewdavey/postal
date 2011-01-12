pushd .
cd tools\nuget

rmdir /S /Q package

mkdir package
mkdir package\lib
mkdir package\lib\net40

copy postal.nuspec package\postal.nuspec
copy ..\..\src\postal\bin\release\postal.dll package\lib\net40

nuget pack package\postal.nuspec

rmdir /S /Q package

popd