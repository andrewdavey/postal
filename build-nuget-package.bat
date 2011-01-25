msbuild /p:Configuration=Release src\postal\postal.csproj

pushd .
cd tools\nuget

rmdir /S /Q package\lib

mkdir package\lib
mkdir package\lib\net40

copy ..\..\src\postal\bin\release\postal.dll package\lib\net40

nuget pack package\postal.nuspec

popd